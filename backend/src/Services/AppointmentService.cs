using System.Linq.Expressions;
using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Enums;
using LogopedicBackend.Models;
using LogopedicBackend.Services.Results.Appointments;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Common.Paging;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace LogopedicBackend.Services;

public class AppointmentService(
    LogopedicContext context,
    ITherapistService therapistService,
    IPatientService patientService) : IAppointmentService
{
    public async Task<AppointmentDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var appointment = await context.Appointments
            .AsNoTracking()
            .Where(a => a.Id == id && a.TherapistId == therapistId)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                StartTime = a.StartTime,
                DurationInMinutes = a.DurationInMinutes,
                Type = a.Type,
                Status = a.Status
            })
            .SingleOrDefaultAsync(ct);

        return appointment;
    }

    public async Task<OneOf<PagedResultDto<AppointmentDto>, InvalidDateRangeError, InvalidPageSizeError>> QueryAsync(
        AppointmentQueryParameters query, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var from = (query.From ?? now.Date).ToUniversalTime();
        var to = (query.To ?? now.AddMonths(1)).ToUniversalTime();

        if (from > to)
        {
            return new InvalidDateRangeError(from, to);
        }

        var pageNumber = query.PageNumber;

        if (query.PageSize is > AppointmentQueryParameters.MaxPageSize or < AppointmentQueryParameters.MinPageSize)
        {
            return new InvalidPageSizeError(
                query.PageSize,
                AppointmentQueryParameters.MinPageSize,
                AppointmentQueryParameters.MaxPageSize);
        }

        var pageSize = query.PageSize;

        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var baseQuery = context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapistId && a.StartTime >= from && a.StartTime < to);

        if (query.PatientId is not null)
        {
            baseQuery = baseQuery.Where(a => a.PatientId == query.PatientId.Value);
        }

        if (query.Status?.Any() == true)
        {
            baseQuery = baseQuery.Where(a => query.Status.Contains(a.Status));
        }

        if (query.Type?.Any() == true)
        {
            baseQuery = baseQuery.Where(a => query.Type.Contains(a.Type));
        }

        baseQuery = ApplySorting(baseQuery, query.Sort);

        var totalCount = await baseQuery.CountAsync(ct);
        var skip = (pageNumber - 1) * pageSize;

        var items = await baseQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                StartTime = a.StartTime,
                DurationInMinutes = a.DurationInMinutes,
                Type = a.Type,
                Status = a.Status
            })
            .ToListAsync(ct);

        return new PagedResultDto<AppointmentDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<OneOf<AppointmentCreated, PatientNotFound, TimeConflict, DurationZeroOrLess>> CreateAsync(
        CreateAppointmentDto dto,
        CancellationToken ct = default)
    {
        if (dto.DurationInMinutes <= 0)
        {
            return new DurationZeroOrLess(dto.DurationInMinutes);
        }

        var therapist = await therapistService.GetCurrentTherapistOrThrowAsync(ct);

        var patient = await patientService.GetByIdAsync(dto.PatientId, ct);

        if (patient is null)
        {
            return new PatientNotFound(dto.PatientId);
        }

        // Check for overlapping appointments
        var endTime = dto.StartTime.AddMinutes(dto.DurationInMinutes);
        var conflictingAppointments = await context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapist.Id &&
                        a.StartTime < endTime &&
                        a.StartTime.AddMinutes(a.DurationInMinutes) > dto.StartTime)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = patient.Id,
                StartTime = a.StartTime,
                DurationInMinutes = a.DurationInMinutes,
                Type = a.Type,
                Status = a.Status
            })
            .ToListAsync(ct);

        if (conflictingAppointments.Count != 0)
        {
            return new TimeConflict(dto.StartTime, endTime, conflictingAppointments);
        }

        var appointment = new Appointment
        {
            StartTime = dto.StartTime,
            DurationInMinutes = dto.DurationInMinutes,
            Type = dto.Type,
            Status = AppointmentStatus.Scheduled,
            TherapistId = therapist.Id,
            Therapist = therapist,
            PatientId = dto.PatientId,
            Patient = patient
        };

        context.Appointments.Add(appointment);
        await context.SaveChangesAsync(ct);

        var result = new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = patient.Id,
            StartTime = appointment.StartTime,
            DurationInMinutes = appointment.DurationInMinutes,
            Type = appointment.Type,
            Status = appointment.Status
        };

        return new AppointmentCreated(result);
    }

    public async Task<OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess>> PatchAsync(
        int id,
        PatchAppointmentDto dto,
        CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        // If no fields to update, return early
        if (dto.StartTime is null &&
            dto.DurationInMinutes is null &&
            dto.Type is null &&
            dto.Status is null &&
            dto.PatientId is null)
        {
            return new AppointmentUpdated();
        }

        if (dto.DurationInMinutes is not null)
        {
            if (dto.DurationInMinutes.Value <= 0)
            {
                return new DurationZeroOrLess(dto.DurationInMinutes.Value);
            }
        }

        if (dto.PatientId is not null)
        {
            var patientExists = await patientService.ExistsForTherapistAsync(dto.PatientId.Value, ct);
            if (!patientExists)
            {
                return new PatientNotFound(dto.PatientId.Value);
            }
        }

        var rows = await context.Appointments
            .Where(a => a.Id == id && a.TherapistId == therapistId)
            .ExecuteUpdateAsync(setter => setter
                    .SetProperty(a => a.StartTime, a => dto.StartTime ?? a.StartTime)
                    .SetProperty(a => a.DurationInMinutes, a => dto.DurationInMinutes ?? a.DurationInMinutes)
                    .SetProperty(a => a.Type, a => dto.Type ?? a.Type)
                    .SetProperty(a => a.Status, a => dto.Status ?? a.Status)
                    .SetProperty(a => a.PatientId, a => dto.PatientId ?? a.PatientId),
                ct);

        if (rows == 0)
        {
            return new AppointmentNotFound(id);
        }

        return new AppointmentUpdated();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var rows = await context.Appointments
            .Where(a => a.Id == id && a.TherapistId == therapistId)
            .ExecuteDeleteAsync(ct);

        return rows > 0;
    }

    private static IQueryable<Appointment> ApplySorting(IQueryable<Appointment> baseQuery, string sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "startTime:asc";
        }

        var clauses = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var map = new Dictionary<string, Expression<Func<Appointment, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["starttime"] = a => a.StartTime,
            ["id"] = a => a.Id,
            ["duration"] = a => a.DurationInMinutes,
            ["type"] = a => a.Type,
            ["status"] = a => a.Status
        };

        IOrderedQueryable<Appointment>? ordered = null;

        var hasIdSort = false;

        foreach (var clause in clauses)
        {
            var parts = clause.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var field = parts[0];
            var direction = parts.Length > 1 ? parts[1] : "asc";

            // Skip fields which don't correspond to allowed sorting fields
            if (!map.TryGetValue(field, out var selector))
            {
                continue;
            }

            if (!hasIdSort && string.Equals(field, "id", StringComparison.OrdinalIgnoreCase))
            {
                hasIdSort = true;
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

        if (ordered is null)
        {
            return baseQuery.OrderBy(a => a.StartTime).ThenBy(a => a.Id);
        }

        if (!hasIdSort)
        {
            ordered = ordered.ThenBy(a => a.Id);
        }

        return ordered;
    }
}