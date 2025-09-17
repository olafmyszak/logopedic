using LogopedicBackend.Models;

namespace LogopedicBackend.Services;

public interface ITherapistService
{
    Task<int> GetCurrentTherapistIdOrThrowAsync(CancellationToken ct = default);
    Task<Therapist> GetCurrentTherapistOrThrowAsync(CancellationToken ct = default);
}
