using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class CurrentUserAccessor(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext dbContext) : ICurrentUserAccessor
{
    // Presence is heartbeat-based, not a live socket signal: any authenticated request bumps
    // LastSeenAt, throttled so a chatty client doesn't turn this into a write-per-request. The
    // frontend treats "seen within this window" as online (see conversations feature).
    private static readonly TimeSpan LastSeenUpdateThreshold = TimeSpan.FromMinutes(1);

    private AppUser? _cachedUser;

    public async Task<AppUser> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserOrNullAsync(cancellationToken);
        if (user is null)
        {
            throw new UnauthenticatedException();
        }

        return user;
    }

    public async Task<AppUser?> GetCurrentUserOrNullAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedUser is not null)
        {
            return _cachedUser;
        }

        var oid = httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value;
        if (string.IsNullOrEmpty(oid))
        {
            return null;
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(u => u.B2CObjectId == oid, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (user is null)
        {
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                B2CObjectId = oid,
                CreatedAtUtc = now,
                LastSeenAt = now
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else if (user.LastSeenAt is null || now - user.LastSeenAt > LastSeenUpdateThreshold)
        {
            user.LastSeenAt = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _cachedUser = user;
        return user;
    }
}
