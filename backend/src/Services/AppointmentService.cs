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
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        AppointmentDto? appointment = await context.Appointments
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
            return new InvalidPageSizeError(query.PageSize,
                AppointmentQueryParameters.MinPageSize,
                AppointmentQueryParameters.MaxPageSize);
        }

        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        IQueryable<Appointment> baseQuery = context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapistId && a.StartTime >= query.From && a.StartTime < query.To);

        baseQuery = ApplyFiltering(baseQuery, query);
        baseQuery = ApplySorting(baseQuery, query);

        return await baseQuery.ToPagedResultAsync(query.PageNumber,
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
        CreateAppointmentDto dto, CancellationToken ct = default)
    {
        if (dto.DurationInMinutes <= 0)
        {
            return new DurationZeroOrLess(dto.DurationInMinutes);
        }

        Therapist therapist = await therapistService.GetCurrentTherapistOrThrowAsync(ct);

        Patient? patient = await patientService.GetByIdAsync(dto.PatientId, ct);

        if (patient is null)
        {
            return new PatientNotFound(dto.PatientId);
        }

        // Check for overlapping appointments
        DateTimeOffset endTime = dto.StartTime.AddMinutes(dto.DurationInMinutes);
        List<AppointmentDto> conflictingAppointments = await context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapist.Id && a.StartTime < endTime &&
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

        Appointment appointment = new()
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

        AppointmentDto result = new()
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
        PatchAsync(int id, PatchAppointmentDto dto, CancellationToken ct = default)
    {
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        Appointment? appointment = await context.Appointments
            .Include(appointment => appointment.Patient)
            .SingleOrDefaultAsync(a => a.Id == id && a.TherapistId == therapistId, ct);

        if (appointment is null)
        {
            return new AppointmentNotFound(id);
        }

        // If no fields to update, return early
        if (dto.StartTime is null && dto.DurationInMinutes is null && dto.Type is null && dto.Status is null &&
            dto.PatientId is null)
        {
            return new AppointmentUpdated();
        }

        DateTimeOffset newStartTime = dto.StartTime ?? appointment.StartTime;
        int newDurationInMinutes = dto.DurationInMinutes ?? appointment.DurationInMinutes;
        AppointmentType newType = dto.Type ?? appointment.Type;
        AppointmentStatus newStatus = dto.Status ?? appointment.Status;
        int newPatientId = dto.PatientId ?? appointment.PatientId;

        if (newDurationInMinutes <= 0)
        {
            return new DurationZeroOrLess(newDurationInMinutes);
        }

        bool patientExists = await patientService.ExistsForTherapistAsync(newPatientId, ct);
        if (!patientExists)
        {
            return new PatientNotFound(newPatientId);
        }

        DateTimeOffset endTime = newStartTime.AddMinutes(newDurationInMinutes);
        List<AppointmentDto> conflictingAppointments = await context.Appointments
            .AsNoTracking()
            .Where(a => a.Id != id && // Exclude currently updated appointment or it will always conflict
                        a.TherapistId == therapistId && a.StartTime < endTime &&
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

        using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
        ILogger logger = factory.CreateLogger<AppointmentService>();
        logger.LogInformation("{id}", appointment.PatientId.ToString());
        logger.LogInformation("{id}", appointment.Patient.Id.ToString());

        return new AppointmentUpdated();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        int therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);
        Appointment? appointment =
            context.Appointments.SingleOrDefault(a => a.Id == id && a.TherapistId == therapistId);

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
        string sort = query.Sort;

        if (string.IsNullOrWhiteSpace(sort))
        {
            sort = "startTime:asc";
        }

        string[] clauses = sort.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Dictionary<string, Expression<Func<Appointment, object?>>> map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = a => a.Id,
            ["starttime"] = a => a.StartTime,
            ["duration"] = a => a.DurationInMinutes,
            ["type"] = a => a.Type,
            ["status"] = a => a.Status
        };

        IOrderedQueryable<Appointment>? ordered = null;

        bool hasIdSort = false;

        foreach (string clause in clauses)
        {
            string[] parts = clause.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string field = parts[0];
            string direction = parts.Length > 1 ? parts[1] : "asc";

            // Skip fields which don't correspond to allowed sorting fields
            if (!map.TryGetValue(field, out Expression<Func<Appointment, object?>>? selector))
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
            return baseQuery.OrderBy(a => a.StartTime)
                .ThenBy(a => a.Id);
        }

        if (!hasIdSort)
        {
            ordered = ordered.ThenBy(a => a.Id);
        }

        return ordered;
    }
}
