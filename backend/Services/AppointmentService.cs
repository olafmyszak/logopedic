using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using LogopedicBackend.Services.Results.Appointments;
using LogopedicBackend.Services.Results.Common.NotFound;
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
                StartTime = a.StartTime,
                DurationInMinutes = a.DurationInMinutes,
                Type = a.Type,
                Status = a.Status
            })
            .SingleOrDefaultAsync(ct);

        return appointment;
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var appointments = await context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapistId)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                StartTime = a.StartTime,
                DurationInMinutes = a.DurationInMinutes,
                Type = a.Type,
                Status = a.Status
            })
            .ToListAsync(ct);

        return appointments;
    }

    public async Task<OneOf<AppointmentCreated, PatientNotFound, TimeConflict>> CreateAsync(
        CreateAppointmentDto dto,
        CancellationToken ct = default)
    {
        var therapist = await therapistService.GetCurrentTherapistOrThrowAsync(ct);

        var patient = await patientService.GetByIdAsync(dto.PatientId, ct);

        if (patient is null)
        {
            return new PatientNotFound();
        }

        // Check for overlapping appointments
        var endTime = dto.StartTime.AddMinutes(dto.DurationInMinutes);
        var conflict = await context.Appointments
            .AsNoTracking()
            .Where(a => a.TherapistId == therapist.Id)
            .AnyAsync(a => a.StartTime < endTime && a.StartTime.AddMinutes(a.DurationInMinutes) > dto.StartTime, ct);

        if (conflict)
        {
            return new TimeConflict();
        }

        var appointment = new Appointment
        {
            StartTime = dto.StartTime,
            DurationInMinutes = dto.DurationInMinutes,
            Type = dto.Type,
            Status = dto.Status,
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
            StartTime = appointment.StartTime,
            DurationInMinutes = appointment.DurationInMinutes,
            Type = appointment.Type,
            Status = appointment.Status
        };

        return new AppointmentCreated(result);
    }

    public async Task<OneOf<AppointmentUpdated, AppointmentNotFound, PatientNotFound>> UpdateAsync(
        int id,
        UpdateAppointmentDto dto,
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

        if (dto.PatientId is not null)
        {
            var patientExists = await patientService.ExistsForTherapistAsync(dto.PatientId.Value, ct);
            if (!patientExists)
            {
                return new PatientNotFound();
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
            return new AppointmentNotFound();
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
}