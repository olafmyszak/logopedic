using LogopedicBackend.Data;
using LogopedicBackend.Exceptions;
using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Services;

public class TherapistService(LogopedicContext context, ICurrentUserService currentUser) : ITherapistService
{
    private Therapist? _cachedTherapist;

    public async Task<Therapist?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        if (_cachedTherapist is not null && _cachedTherapist.UserId == userId)
        {
            return _cachedTherapist;
        }

        _cachedTherapist = await context.Therapists.SingleOrDefaultAsync(t => t.UserId == userId, ct);

        return _cachedTherapist;
    }

    public async Task<int?> GetTherapistIdByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var therapist = await GetByUserIdAsync(userId, ct);
        return therapist?.Id;
    }

    public async Task<int> GetCurrentTherapistIdOrThrowAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var id = await GetTherapistIdByUserIdAsync(userId, ct);
        if (!id.HasValue) throw new ForbiddenException("User is not associated with a therapist record.");
        return id.Value;
    }

    public async Task<Therapist> GetCurrentTherapistOrThrowAsync(CancellationToken ct = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAccessException();
        var therapist = await GetByUserIdAsync(userId, ct);
        if (therapist is null) throw new ForbiddenException("User is not associated with a therapist record.");
        return therapist;
    }
}