using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface ICurrentUserAccessor
{
    Task<AppUser> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    // Same as GetCurrentUserAsync but returns null instead of throwing when the caller is
    // anonymous — for AllowAnonymous read endpoints that personalize their response (e.g.
    // "did I react to this") when a viewer is present, but still work without one.
    Task<AppUser?> GetCurrentUserOrNullAsync(CancellationToken cancellationToken = default);
}
