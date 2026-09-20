using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

public interface IVotingService
{
    // Upserts the vote and applies the resulting karma delta to the target's author in the same
    // SaveChangesAsync call. Throws SelfVoteException if voterId is the target's own author.
    Task CastVoteAsync(
        VoteTargetType targetType, Guid targetId, Guid voterId, sbyte value, CancellationToken cancellationToken = default);

    // No-op if the voter has no existing vote on this target.
    Task RemoveVoteAsync(
        VoteTargetType targetType, Guid targetId, Guid voterId, CancellationToken cancellationToken = default);

    Task<int> GetScoreAsync(VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken = default);

    // Null when the viewer has no vote on this target (including anonymous viewers, since
    // Guid.Empty never matches a real VoterId).
    Task<sbyte?> GetViewerVoteAsync(
        VoteTargetType targetType, Guid targetId, Guid viewerId, CancellationToken cancellationToken = default);

    // Batch form of GetScoreAsync for list endpoints — one query for the whole page instead of
    // one per item. Targets with no votes are absent from the result (treat as score 0).
    Task<IReadOnlyDictionary<Guid, int>> GetScoresAsync(
        VoteTargetType targetType, IReadOnlyCollection<Guid> targetIds, CancellationToken cancellationToken = default);

    // Batch form of GetViewerVoteAsync for list endpoints. Targets absent from the result mean
    // the viewer has no vote on them.
    Task<IReadOnlyDictionary<Guid, sbyte>> GetViewerVotesAsync(
        VoteTargetType targetType, IReadOnlyCollection<Guid> targetIds, Guid viewerId, CancellationToken cancellationToken = default);
}
