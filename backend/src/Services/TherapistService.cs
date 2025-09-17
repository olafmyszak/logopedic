using LogopedicBackend.Data;
using LogopedicBackend.Exceptions;
using LogopedicBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LogopedicBackend.Services;

public class TherapistService(LogopedicContext context, ICurrentUserService currentUser) : ITherapistService
{
    public async Task<int> GetCurrentTherapistIdOrThrowAsync(CancellationToken ct = default)
    {
        string userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        Therapist? therapist = await context.Therapists.SingleOrDefaultAsync(t => t.UserId == userId, ct);

        if (therapist is null)
        {
            throw new ForbiddenException("User is not associated with a therapist record.");
        }

        return therapist.Id;
    }

    public async Task<Therapist> GetCurrentTherapistOrThrowAsync(CancellationToken ct = default)
    {
        string userId = currentUser.UserId ?? throw new UnauthorizedAccessException();

        Therapist? therapist = await context.Therapists.SingleOrDefaultAsync(t => t.UserId == userId, ct);

        if (therapist is null)
        {
            throw new ForbiddenException("User is not associated with a therapist record.");
        }

        return therapist;
    }
}
