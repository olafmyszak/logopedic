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
            ["fullname"] = p => p.FullName, ["id"] = p => p.Id, ["contactinfo"] = p => p.ContactInfo
        };

    public async Task<bool> ExistsForTherapistAsync(int patientId, CancellationToken ct)
    {
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && p.TherapistId == therapistId, ct);
    }

    public async Task<Patient?> GetByIdAsync(int patientId, CancellationToken ct = default)
    {
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients.SingleOrDefaultAsync(p => p.Id == patientId && p.TherapistId == therapistId, ct);
    }

    public async Task<PatientDto?> GetPatientDtoByIdAsync(int patientId, CancellationToken ct = default)
    {
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

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
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        List<PatientDto> patients = await context.Patients
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
        int pageNumber = query.PageNumber;

        const int minPageSize = 1;
        const int maxPageSize = 200;

        if (query.PageSize is > maxPageSize or < minPageSize)
        {
            return new InvalidPageSizeError(query.PageSize, minPageSize, maxPageSize);
        }

        int pageSize = query.PageSize;

        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        IQueryable<Patient> baseQuery = context.Patients
            .AsNoTracking()
            .Where(p => p.TherapistId == therapistId);

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

        int totalCount = await baseQuery.CountAsync(ct);
        int skip = (pageNumber - 1) * pageSize;

        List<PatientDto> items = await baseQuery.Skip(skip)
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
            Items = items, TotalCount = totalCount, PageNumber = pageNumber, PageSize = pageSize
        };
    }

    public async Task<OneOf<PatientCreated, InvalidDateOfBirthError>> CreateAsync(CreatePatientDto dto,
        CancellationToken ct = default)
    {
        // Disallow DoBs in the future or more than 120 years in the past
        DateOnly dateNow = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dto.DateOfBirth < dateNow.AddYears(-120) || dto.DateOfBirth > dateNow)
        {
            return new InvalidDateOfBirthError(dto.DateOfBirth, dateNow.AddYears(-120), dateNow);
        }

        Therapist therapist = await therapistService.GetCurrentTherapistOrThrowAsync(ct);

        Patient patient = new()
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

        PatientDto result = new()
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
        Patient? patient = await GetByIdAsync(id, ct);
        if (patient is null)
        {
            return new PatientNotFound(id);
        }

        if (dto.FullName is null && dto.DateOfBirth is null && dto.ContactInfo is null && dto.Notes is null)
        {
            return new PatientUpdated();
        }

        string newFullName = dto.FullName ?? patient.FullName;
        DateOnly newDateOfBirth = dto.DateOfBirth ?? patient.DateOfBirth;
        string newContactInfo = dto.ContactInfo ?? patient.ContactInfo;
        string? newNotes = dto.Notes ?? patient.Notes;

        patient.FullName = newFullName;
        patient.DateOfBirth = newDateOfBirth;
        patient.ContactInfo = newContactInfo;
        patient.Notes = newNotes;

        await context.SaveChangesAsync(ct);

        return new PatientUpdated();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        int rows = await context.Patients
            .Where(a => a.Id == id && a.TherapistId == therapistId)
            .ExecuteDeleteAsync(ct);

        return rows > 0;
    }

    private static IQueryable<Patient> ApplySorting(IQueryable<Patient> baseQuery, string sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "fullName:asc";
        }

        string[] clauses = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedQueryable<Patient>? ordered = null;

        foreach (string clause in clauses)
        {
            string[] parts = clause.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string field = parts[0];
            string direction = parts.Length > 1 ? parts[1] : "asc";

            // Skip fields which don't correspond to allowed sorting fields
            if (!SortMap.TryGetValue(field, out Expression<Func<Patient, object?>>? selector))
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

        return ordered ?? baseQuery.OrderBy(p => p.FullName)
            .ThenBy(p => p.Id);
    }
}
