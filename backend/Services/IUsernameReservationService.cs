using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface IUsernameReservationService
{
    bool IsValidFormat(string username);

    Task<bool> IsAvailableAsync(string username, CancellationToken cancellationToken = default);

    // Sets and persists the username on the given user; throws UsernameConflictException on collision.
    Task ReserveAsync(AppUser user, string username, CancellationToken cancellationToken = default);
}
