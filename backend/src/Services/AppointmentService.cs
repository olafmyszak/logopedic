using System.Linq.Expressions;
using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Enums;
using LogopedicBackend.Extensions;
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
        if (query.From > query.To)
        {
            return new InvalidDateRangeError(query.From, query.To);
        }

        if (query.PageSize is > AppointmentQueryParameters.MaxPageSize or < AppointmentQueryParameters.MinPageSize)
        {
            return new InvalidPageSizeError(
                query.PageSize,
                AppointmentQueryParameters.MinPageSize,
                AppointmentQueryParameters.MaxPageSize);
        }

        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var baseQuery = context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapistId && a.StartTime >= query.From && a.StartTime < query.To);

        baseQuery = ApplyFiltering(baseQuery, query);
        baseQuery = ApplySorting(baseQuery, query);

        return await baseQuery.ToPagedResultAsync(
            query.PageNumber,
            query.PageSize,
            a => new AppointmentDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                StartTime = a.StartTime,
                DurationInMinutes = a.DurationInMinutes,
                Type = a.Type,
                Status = a.Status
            },
            ct);
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
                PatientId = a.PatientId,
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

    public async Task<OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound, DurationZeroOrLess, TimeConflict>>
        PatchAsync(
            int id,
            PatchAppointmentDto dto,
            CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var appointment =
            await context.Appointments.SingleOrDefaultAsync(a => a.Id == id && a.TherapistId == therapistId, ct);

        if (appointment is null)
        {
            return new AppointmentNotFound(id);
        }

        // If no fields to update, return early
        if (dto.StartTime is null &&
            dto.DurationInMinutes is null &&
            dto.Type is null &&
            dto.Status is null &&
            dto.PatientId is null)
        {
            return new AppointmentUpdated();
        }

        var newStartTime = dto.StartTime ?? appointment.StartTime;
        var newDurationInMinutes = dto.DurationInMinutes ?? appointment.DurationInMinutes;
        var newType = dto.Type ?? appointment.Type;
        var newStatus = dto.Status ?? appointment.Status;
        var newPatientId = dto.PatientId ?? appointment.PatientId;

        if (newDurationInMinutes <= 0)
        {
            return new DurationZeroOrLess(newDurationInMinutes);
        }

        var patientExists = await patientService.ExistsForTherapistAsync(newPatientId, ct);
        if (!patientExists)
        {
            return new PatientNotFound(newPatientId);
        }

        var endTime = newStartTime.AddMinutes(newDurationInMinutes);
        var conflictingAppointments = await context.Appointments
            .AsNoTracking()
            .Where(a => a.Id != id && // Exclude currently updated appointment or it will always conflict
                        a.TherapistId == therapistId &&
                        a.StartTime < endTime &&
                        a.StartTime.AddMinutes(newDurationInMinutes) > newStartTime)
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

        if (conflictingAppointments.Count != 0)
        {
            return new TimeConflict(newStartTime, endTime, conflictingAppointments);
        }

        appointment.StartTime = newStartTime;
        appointment.DurationInMinutes = newDurationInMinutes;
        appointment.Type = newType;
        appointment.Status = newStatus;
        appointment.PatientId = newPatientId;

        await context.SaveChangesAsync(ct);

        return new AppointmentUpdated();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);
        var appointment = context.Appointments.SingleOrDefault(a => a.Id == id && a.TherapistId == therapistId);

        if (appointment is null)
        {
            return false;
        }

        context.Appointments.Remove(appointment);
        await context.SaveChangesAsync(ct);

        return true;
    }

    private static IQueryable<Appointment> ApplyFiltering(IQueryable<Appointment> baseQuery,
        AppointmentQueryParameters query)
    {
        if (query.PatientId is not null)
        {
            baseQuery = baseQuery.Where(a => a.PatientId == query.PatientId.Value);
        }

        if (query.Status.Length > 0)
        {
            baseQuery = baseQuery.Where(a => query.Status.Contains(a.Status));
        }

        if (query.Type.Length > 0)
        {
            baseQuery = baseQuery.Where(a => query.Type.Contains(a.Type));
        }

        return baseQuery;
    }

    private static IQueryable<Appointment> ApplySorting(IQueryable<Appointment> baseQuery,
        AppointmentQueryParameters query)
    {
        var sort = query.Sort;

        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "startTime:asc";
        }

        var clauses = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var map = new Dictionary<string, Expression<Func<Appointment, object?>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = a => a.Id,
            ["starttime"] = a => a.StartTime,
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