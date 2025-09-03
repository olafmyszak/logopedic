using LogopedicBackend.Data;
using LogopedicBackend.Dtos;
using LogopedicBackend.Models;
using LogopedicBackend.Services.Results.Common.NotFound;
using LogopedicBackend.Services.Results.Patients;
using Microsoft.EntityFrameworkCore;
using OneOf;

namespace LogopedicBackend.Services;

public class PatientService(LogopedicContext context, ITherapistService therapistService) : IPatientService
{
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

        return await context.Patients
            .SingleOrDefaultAsync(p => p.Id == patientId && p.TherapistId == therapistId, ct);
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
            }).SingleOrDefaultAsync(ct);
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
            }).ToListAsync(ct);

        return patients;
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
            .ExecuteUpdateAsync(setter => setter
                    .SetProperty(p => p.FullName, p => dto.FullName ?? p.FullName)
                    .SetProperty(p => p.DateOfBirth, p => dto.DateOfBirth ?? p.DateOfBirth)
                    .SetProperty(p => p.ContactInfo, p => dto.ContactInfo ?? p.ContactInfo)
                    .SetProperty(p => p.Notes, p => dto.Notes ?? p.Notes),
                ct);

        if (rows == 0)
        {
            return new PatientNotFound(id);
        }

        return new PatientUpdated();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        var rows = await context.Patients
            .Where(a => a.Id == id && a.TherapistId == therapistId)
            .ExecuteDeleteAsync(ct);

        return rows > 0;
    }
}