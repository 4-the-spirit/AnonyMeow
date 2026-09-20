using System.Security.Cryptography;
using System.Text;
using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services.SpamDetection;

// Flags an author repeating byte-identical content across submissions within the lookback
// window — a common copy-paste spam pattern (the same promo text posted to many threads).
public class DuplicateContentHeuristic(AppDbContext dbContext, IOptions<SpamDetectionOptions> options) : ISpamHeuristic
{
    public SpamFlagReason Reason => SpamFlagReason.DuplicateContent;

    public async Task<bool> IsMatchAsync(SpamDetectionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(context.Text))
        {
            return false;
        }

        var since = DateTimeOffset.UtcNow - TimeSpan.FromHours(options.Value.DuplicateContentLookbackHours);
        var hash = Hash(context.Text);

        // Mirrors the exact text composition each *Service uses when it evaluates PII/spam for
        // the same content type, so a prior submission hashes identically to how it was
        // evaluated when it was created.
        var priorPostTexts = await dbContext.Posts
            .Where(p => p.AuthorId == context.AuthorId && p.CreatedAtUtc >= since)
            .Select(p => p.BodyMarkdown == null ? p.Title : p.Title + "\n" + p.BodyMarkdown)
            .ToListAsync(cancellationToken);
        var priorCommentTexts = await dbContext.Comments
            .Where(c => c.AuthorId == context.AuthorId && c.CreatedAtUtc >= since)
            .Select(c => c.BodyMarkdown)
            .ToListAsync(cancellationToken);
        var priorMessageTexts = await dbContext.Messages
            .Where(m => m.SenderId == context.AuthorId && m.CreatedAtUtc >= since)
            .Select(m => m.Body)
            .ToListAsync(cancellationToken);

        var priorTexts = priorPostTexts.Concat(priorCommentTexts).Concat(priorMessageTexts);

        // The submission being evaluated is already persisted by the time this heuristic runs,
        // so it always matches itself once — more than one match means a genuine repeat.
        return priorTexts.Count(t => Hash(t) == hash) > 1;
    }

    private static string Hash(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim())));
}
