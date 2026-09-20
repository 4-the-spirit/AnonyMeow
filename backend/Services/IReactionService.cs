using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Reactions;

namespace AnonyMeow.Services;

public interface IReactionService
{
    // Idempotent: reacting twice with the same (target, user, emoji) is a no-op, not a duplicate.
    Task AddAsync(
        ReactionTargetType targetType, Guid targetId, Guid userId, string emoji, CancellationToken cancellationToken = default);

    Task RemoveAsync(
        ReactionTargetType targetType, Guid targetId, Guid userId, string emoji, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReactionSummaryResponse>> GetSummaryAsync(
        ReactionTargetType targetType, Guid targetId, Guid viewerId, CancellationToken cancellationToken = default);

    // Batch form of GetSummaryAsync for list endpoints — one query for the whole page instead of
    // one per item. Ids absent from the result have no reactions.
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<ReactionSummaryResponse>>> GetSummariesAsync(
        ReactionTargetType targetType, IReadOnlyCollection<Guid> targetIds, Guid viewerId, CancellationToken cancellationToken = default);
}
