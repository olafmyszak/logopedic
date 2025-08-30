using LogopedicBackend.Models;

namespace LogopedicBackend.Services;

public interface ITherapistService
{
    Task<Therapist?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int?> GetTherapistIdByUserIdAsync(string userId, CancellationToken ct = default);
    Task<int> GetCurrentTherapistIdOrThrowAsync(CancellationToken ct = default);
    Task<Therapist> GetCurrentTherapistOrThrowAsync(CancellationToken ct = default);
}