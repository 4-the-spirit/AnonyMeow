using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class VotingService(AppDbContext dbContext) : IVotingService
{
    public async Task CastVoteAsync(
        VoteTargetType targetType, Guid targetId, Guid voterId, sbyte value, CancellationToken cancellationToken = default)
    {
        if (value is not (1 or -1))
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Vote value must be +1 or -1.");
        }

        var authorId = await GetTargetAuthorIdAsync(targetType, targetId, cancellationToken);
        if (authorId == voterId)
        {
            throw new SelfVoteException();
        }

        var existingVote = await dbContext.Votes.SingleOrDefaultAsync(
            v => v.TargetType == targetType && v.TargetId == targetId && v.VoterId == voterId, cancellationToken);
        var previousValue = existingVote?.Value ?? 0;

        if (existingVote is null)
        {
            dbContext.Votes.Add(new Vote
            {
                Id = Guid.NewGuid(), TargetType = targetType, TargetId = targetId, VoterId = voterId, Value = value
            });
        }
        else
        {
            existingVote.Value = value;
        }

        await ApplyKarmaDeltaAsync(authorId, value - previousValue, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveVoteAsync(
        VoteTargetType targetType, Guid targetId, Guid voterId, CancellationToken cancellationToken = default)
    {
        var existingVote = await dbContext.Votes.SingleOrDefaultAsync(
            v => v.TargetType == targetType && v.TargetId == targetId && v.VoterId == voterId, cancellationToken);
        if (existingVote is null)
        {
            return;
        }

        var authorId = await GetTargetAuthorIdAsync(targetType, targetId, cancellationToken);
        dbContext.Votes.Remove(existingVote);

        await ApplyKarmaDeltaAsync(authorId, (sbyte)-existingVote.Value, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> GetScoreAsync(VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken = default) =>
        dbContext.Votes
            .Where(v => v.TargetType == targetType && v.TargetId == targetId)
            .SumAsync(v => (int)v.Value, cancellationToken);

    public async Task<sbyte?> GetViewerVoteAsync(
        VoteTargetType targetType, Guid targetId, Guid viewerId, CancellationToken cancellationToken = default)
    {
        var vote = await dbContext.Votes.SingleOrDefaultAsync(
            v => v.TargetType == targetType && v.TargetId == targetId && v.VoterId == viewerId, cancellationToken);
        return vote?.Value;
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetScoresAsync(
        VoteTargetType targetType, IReadOnlyCollection<Guid> targetIds, CancellationToken cancellationToken = default) =>
        await dbContext.Votes
            .Where(v => v.TargetType == targetType && targetIds.Contains(v.TargetId))
            .GroupBy(v => v.TargetId)
            .Select(g => new { TargetId = g.Key, Score = g.Sum(v => (int)v.Value) })
            .ToDictionaryAsync(x => x.TargetId, x => x.Score, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, sbyte>> GetViewerVotesAsync(
        VoteTargetType targetType, IReadOnlyCollection<Guid> targetIds, Guid viewerId, CancellationToken cancellationToken = default) =>
        await dbContext.Votes
            .Where(v => v.TargetType == targetType && targetIds.Contains(v.TargetId) && v.VoterId == viewerId)
            .ToDictionaryAsync(v => v.TargetId, v => v.Value, cancellationToken);

    private async Task<Guid> GetTargetAuthorIdAsync(
        VoteTargetType targetType, Guid targetId, CancellationToken cancellationToken)
    {
        Guid? authorId = targetType switch
        {
            VoteTargetType.Post => (await dbContext.Posts.FindAsync([targetId], cancellationToken))?.AuthorId,
            VoteTargetType.Comment => (await dbContext.Comments.FindAsync([targetId], cancellationToken))?.AuthorId,
            _ => throw new ArgumentOutOfRangeException(nameof(targetType))
        };

        return authorId ?? throw new VoteTargetNotFoundException();
    }

    private async Task ApplyKarmaDeltaAsync(Guid authorId, int karmaDelta, CancellationToken cancellationToken)
    {
        if (karmaDelta == 0)
        {
            return;
        }

        var author = await dbContext.Users.FindAsync([authorId], cancellationToken);
        if (author is not null)
        {
            author.Karma += karmaDelta;
        }
    }
}
