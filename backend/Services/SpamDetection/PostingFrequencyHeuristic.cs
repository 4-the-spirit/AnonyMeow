using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Services.SpamDetection;

// Flags accounts still inside their "new account" window that are posting/commenting/messaging
// faster than the configured threshold — a common bot/spam-account signal, distinct from the
// flat per-endpoint rate limiter (which caps everyone equally regardless of account age).
public class PostingFrequencyHeuristic(AppDbContext dbContext, IOptions<SpamDetectionOptions> options) : ISpamHeuristic
{
    public SpamFlagReason Reason => SpamFlagReason.RateLimitExceeded;

    public async Task<bool> IsMatchAsync(SpamDetectionContext context, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var author = await dbContext.Users.FindAsync([context.AuthorId], cancellationToken);
        if (author is null)
        {
            return false;
        }

        var accountAge = DateTimeOffset.UtcNow - author.CreatedAtUtc;
        if (accountAge > TimeSpan.FromMinutes(settings.NewAccountWindowMinutes))
        {
            return false;
        }

        var since = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(settings.PostingFrequencyWindowMinutes);
        var recentPosts = await dbContext.Posts.CountAsync(
            p => p.AuthorId == context.AuthorId && p.CreatedAtUtc >= since, cancellationToken);
        var recentComments = await dbContext.Comments.CountAsync(
            c => c.AuthorId == context.AuthorId && c.CreatedAtUtc >= since, cancellationToken);
        var recentMessages = await dbContext.Messages.CountAsync(
            m => m.SenderId == context.AuthorId && m.CreatedAtUtc >= since, cancellationToken);

        // The submission being evaluated is already persisted by the time this heuristic runs,
        // so the count above already includes it.
        return recentPosts + recentComments + recentMessages > settings.MaxSubmissionsForNewAccounts;
    }
}
