using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

// Shared CommentResponse assembly for endpoints that list comments without a single already-known
// PostId context (Search) — mirrors PostResponseAssembly's reasoning. CommentEndpoints' own
// listings (top-level/replies, which already know the post) reuse this too, to avoid duplicating
// the batch author-lookup + per-item score/reply-count/reaction assembly logic.
internal static class CommentResponseAssembly
{
    public static async Task<List<CommentResponse>> BuildAsync(
        IReadOnlyList<Comment> comments,
        ICommentService commentService,
        IVotingService votingService,
        IReactionService reactionService,
        Guid viewerId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var authorIds = comments.Select(c => c.AuthorId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { Username = u.Username ?? string.Empty, u.DisplayName, u.AvatarSeed }, cancellationToken);

        var postIds = comments.Select(c => c.PostId).Distinct().ToList();
        var postCommunityIds = await dbContext.Posts
            .Where(p => postIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.CommunityId, cancellationToken);
        var communityIds = postCommunityIds.Values.Distinct().ToList();
        var communityNames = await dbContext.Communities
            .Where(c => communityIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var commentIds = comments.Select(c => c.Id).ToList();
        var scores = await votingService.GetScoresAsync(VoteTargetType.Comment, commentIds, cancellationToken);
        var replyCounts = await commentService.GetReplyCountsAsync(commentIds, cancellationToken);
        var reactionsByComment = await reactionService.GetSummariesAsync(ReactionTargetType.Comment, commentIds, viewerId, cancellationToken);
        var viewerVotes = await votingService.GetViewerVotesAsync(VoteTargetType.Comment, commentIds, viewerId, cancellationToken);
        var ancestorChains = await commentService.GetAncestorChainsAsync(commentIds, cancellationToken);

        var responses = new List<CommentResponse>(comments.Count);
        foreach (var comment in comments)
        {
            var score = scores.GetValueOrDefault(comment.Id);
            var replyCount = replyCounts.GetValueOrDefault(comment.Id);
            var reactions = reactionsByComment.GetValueOrDefault(comment.Id, []);
            var viewerVote = viewerVotes.TryGetValue(comment.Id, out var vote) ? vote : (sbyte?)null;
            var ancestorChain = ancestorChains.GetValueOrDefault(comment.Id, []);
            var author = authors.GetValueOrDefault(comment.AuthorId);
            var communityName = postCommunityIds.TryGetValue(comment.PostId, out var communityId)
                ? communityNames.GetValueOrDefault(communityId, string.Empty)
                : string.Empty;
            responses.Add(new CommentResponse(
                comment.Id, author?.Username ?? string.Empty, author?.DisplayName, comment.BodyMarkdown,
                score, replyCount, comment.CreatedAtUtc, comment.EditedAtUtc, reactions,
                comment.PostId, ancestorChain, author?.AvatarSeed, communityName, viewerVote));
        }

        return responses;
    }
}
