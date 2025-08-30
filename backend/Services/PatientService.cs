using LogopedicBackend.Data;
using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Services;

public class PatientService(LogopedicContext context, ITherapistService therapistService) : IPatientService
{
    public async Task<bool> ExistsForTherapistAsync(int patientId, CancellationToken ct)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients
            .AsNoTracking()
            .AnyAsync(p => p.Id == patientId && p.TherapistId == therapistId, cancellationToken: ct);
    }

    public async Task<Patient?> GetById(int patientId, CancellationToken ct = default)
    {
        var therapistId = await therapistService.GetCurrentTherapistIdOrThrowAsync(ct);

        return await context.Patients
            .SingleOrDefaultAsync(p => p.Id == patientId && p.TherapistId == therapistId, cancellationToken: ct);
    }
}