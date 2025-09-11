using System.Linq.Expressions;
using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Common.Paging;
using LogopedicBackend.Services.Results.Patients;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace LogopedicBackend.Services;

public class PatientService(LogopedicContext context, ITherapistService therapistService) : IPatientService
{
    private static readonly Dictionary<string, Expression<Func<Patient, object?>>> SortMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["fullname"] = p => p.FullName,
            ["id"] = p => p.Id,
            ["contactinfo"] = p => p.ContactInfo
        };

    public async Task<bool> ExistsForTherapistAsync(int patientId, CancellationToken ct)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && p.TherapistId == therapistId, ct);
    }

    public async Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients.SingleOrDefaultAsync(p => p.Id == patientId && p.TherapistId == therapistId, ct);
    }

    public async Task<PatientDto?> GetPatientDtoByIdAsync(int patientId, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients
            .Where(p => p.Id == patientId && p.TherapistId == therapistId)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                ContactInfo = p.ContactInfo,
                Notes = p.Notes
            })
            .SingleOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<PatientDto>> GetAllAsync(CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var patients = await context.Patients
            .AsNoTracking()
            .Where(p => p.TherapistId == therapistId)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                ContactInfo = p.ContactInfo,
                Notes = p.Notes
            })
            .ToListAsync(ct);

        return patients;
    }

    public async Task<OneOf<PagedResultDto<PatientDto>, InvalidPageSizeError>> QueryAsync(PatientQueryParameters query,
        CancellationToken ct = default)
    {
        var pageNumber = query.PageNumber;

        const int minPageSize = 1;
        const int maxPageSize = 200;

        if (query.PageSize is > maxPageSize or < minPageSize)
        {
            return new InvalidPageSizeError(query.PageSize, minPageSize, maxPageSize);
        }

        var pageSize = query.PageSize;

        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var baseQuery = context.Patients.AsNoTracking().Where(p => p.TherapistId == therapistId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            baseQuery = baseQuery.Where(p => EF.Functions.ILike(p.SearchText, $"%{query.Search}%"))
                .Where(p => EF.Functions.TrigramsSimilarity(p.SearchText, query.Search) > 0.2)
                .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.SearchText, query.Search));
        }
        else
        {
            baseQuery = ApplySorting(baseQuery, query.Sort);
        }

        var totalCount = await baseQuery.CountAsync(ct);
        var skip = (pageNumber - 1) * pageSize;

        var items = await baseQuery.Skip(skip)
            .Take(pageSize)
            .Select(p => new PatientDto
            {
                Id = p.Id,
                FullName = p.FullName,
                DateOfBirth = p.DateOfBirth,
                ContactInfo = p.ContactInfo,
                Notes = p.Notes
            })
            .ToListAsync(ct);

        return new PagedResultDto<PatientDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PatientCreated> CreateAsync(CreatePatientDto dto, CancellationToken ct = default)
    {
        var therapist = await therapistService.GetCurrentTherapistOrThrowAsync(ct);

        var patient = new Patient
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            ContactInfo = dto.ContactInfo,
            Notes = dto.Notes,
            TherapistId = therapist.Id,
            Therapist = therapist
        };

        context.Patients.Add(patient);
        await context.SaveChangesAsync(ct);

        var result = new PatientDto
        {
            Id = patient.Id,
            FullName = patient.FullName,
            DateOfBirth = patient.DateOfBirth,
            ContactInfo = patient.ContactInfo,
            Notes = patient.Notes
        };

        return new PatientCreated(result);
    }

    public async Task<OneOf<PatientUpdated, PatientNotFound>> UpdateAsync(int id, UpdatePatientDto dto,
        CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        if (dto.FullName is null && dto.DateOfBirth is null && dto.ContactInfo is null && dto.Notes is null)
        {
            return new PatientUpdated();
        }

        var rows = await context.Patients
            .Where(p => p.Id == id && p.TherapistId == therapistId)
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(p => p.FullName, p => dto.FullName ?? p.FullName)
                    .SetProperty(p => p.DateOfBirth, p => dto.DateOfBirth ?? p.DateOfBirth)
                    .SetProperty(p => p.ContactInfo, p => dto.ContactInfo ?? p.ContactInfo)
                    .SetProperty(p => p.Notes, p => dto.Notes ?? p.Notes), ct);

        if (rows == 0)
        {
            return new PatientNotFound(id);
        }

        return new PatientUpdated();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var rows = await context.Patients.Where(a => a.Id == id && a.TherapistId == therapistId).ExecuteDeleteAsync(ct);

        return rows > 0;
    }

    private static IQueryable<Patient> ApplySorting(IQueryable<Patient> baseQuery, string sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "fullName:asc";
        }

        var clauses = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedQueryable<Patient>? ordered = null;

        foreach (var clause in clauses)
        {
            var parts = clause.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var field = parts[0];
            var direction = parts.Length > 1 ? parts[1] : "asc";

            // Skip fields which don't correspond to allowed sorting fields
            if (!SortMap.TryGetValue(field, out var selector))
            {
                continue;
            }

            if (ordered is null)
            {
                ordered = direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? baseQuery.OrderByDescending(selector)
                    : baseQuery.OrderBy(selector);
            }
            else
            {
                ordered = direction.Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? ordered.ThenByDescending(selector)
                    : ordered.ThenBy(selector);
            }
        }

        return ordered ?? baseQuery.OrderBy(p => p.FullName).ThenBy(p => p.Id);
    }
}