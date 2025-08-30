using LogopedicBackend.Models;

namespace LogopedicBackend.Services;

public interface IPatientService
{
    Task<bool> ExistsForTherapistAsync(int patientId, CancellationToken ct = default);
    Task<Patient?> GetById(int patientId, CancellationToken ct = default);
}