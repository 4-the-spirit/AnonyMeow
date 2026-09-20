using System.Text.RegularExpressions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public partial class MentionParsingService(AppDbContext dbContext) : IMentionParsingService
{
    // Matches UsernameReservationService's exact format (^[a-z0-9_]{3,20}$), case-insensitively —
    // usernames are stored lowercase but people may type an @mention in any case.
    [GeneratedRegex("@([a-zA-Z0-9_]{3,20})")]
    private static partial Regex MentionPattern();

    public async Task<IReadOnlyList<AppUser>> ExtractMentionedUsersAsync(
        string bodyMarkdown, CancellationToken cancellationToken = default)
    {
        var candidates = MentionPattern().Matches(bodyMarkdown)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (candidates.Count == 0)
        {
            return [];
        }

        return await dbContext.Users
            .Where(u => u.Username != null && candidates.Contains(u.Username.ToLower()))
            .ToListAsync(cancellationToken);
    }
}
