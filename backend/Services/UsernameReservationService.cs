using System.Text.RegularExpressions;
using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public partial class UsernameReservationService(AppDbContext dbContext) : IUsernameReservationService
{
    [GeneratedRegex("^[a-z0-9_]{3,20}$")]
    private static partial Regex UsernamePattern();

    public bool IsValidFormat(string username) =>
        !string.IsNullOrEmpty(username) && UsernamePattern().IsMatch(username);

    public async Task<bool> IsAvailableAsync(string username, CancellationToken cancellationToken = default)
    {
        if (!IsValidFormat(username))
        {
            return false;
        }

        var normalized = username.ToLowerInvariant();
        return !await dbContext.Users.AnyAsync(
            u => u.Username != null && u.Username.ToLower() == normalized,
            cancellationToken);
    }

    public async Task ReserveAsync(AppUser user, string username, CancellationToken cancellationToken = default)
    {
        if (!await IsAvailableAsync(username, cancellationToken))
        {
            throw new UsernameConflictException(username);
        }

        user.Username = username;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
