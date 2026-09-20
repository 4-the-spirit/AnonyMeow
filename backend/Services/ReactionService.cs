using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Reactions;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class ReactionService(AppDbContext dbContext) : IReactionService
{
    // A user may hold at most one active reaction per target, so adding a new emoji replaces
    // any existing different-emoji reaction from the same user instead of adding alongside it.
    public async Task AddAsync(
        ReactionTargetType targetType, Guid targetId, Guid userId, string emoji, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Reactions.SingleOrDefaultAsync(
            r => r.TargetType == targetType && r.TargetId == targetId && r.AppUserId == userId,
            cancellationToken);
        if (existing is not null)
        {
            if (existing.Emoji == emoji)
            {
                return;
            }

            dbContext.Reactions.Remove(existing);
        }

        dbContext.Reactions.Add(new Reaction
        {
            Id = Guid.NewGuid(),
            TargetType = targetType,
            TargetId = targetId,
            AppUserId = userId,
            Emoji = emoji,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(
        ReactionTargetType targetType, Guid targetId, Guid userId, string emoji, CancellationToken cancellationToken = default)
    {
        var reaction = await dbContext.Reactions.SingleOrDefaultAsync(
            r => r.TargetType == targetType && r.TargetId == targetId && r.AppUserId == userId && r.Emoji == emoji,
            cancellationToken);
        if (reaction is null)
        {
            return;
        }

        dbContext.Reactions.Remove(reaction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReactionSummaryResponse>> GetSummaryAsync(
        ReactionTargetType targetType, Guid targetId, Guid viewerId, CancellationToken cancellationToken = default)
    {
        var reactions = await dbContext.Reactions
            .Where(r => r.TargetType == targetType && r.TargetId == targetId)
            .ToListAsync(cancellationToken);

        return reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new ReactionSummaryResponse(g.Key, g.Count(), g.Any(r => r.AppUserId == viewerId)))
            .OrderBy(r => r.Emoji)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ReactionSummaryResponse>>> GetSummariesAsync(
        ReactionTargetType targetType, IReadOnlyCollection<Guid> targetIds, Guid viewerId, CancellationToken cancellationToken = default)
    {
        var reactions = await dbContext.Reactions
            .Where(r => r.TargetType == targetType && targetIds.Contains(r.TargetId))
            .ToListAsync(cancellationToken);

        return reactions
            .GroupBy(r => r.TargetId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ReactionSummaryResponse>)g
                    .GroupBy(r => r.Emoji)
                    .Select(eg => new ReactionSummaryResponse(eg.Key, eg.Count(), eg.Any(r => r.AppUserId == viewerId)))
                    .OrderBy(r => r.Emoji)
                    .ToList());
    }
}
