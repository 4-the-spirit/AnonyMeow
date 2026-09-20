using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

// Shared PostResponse assembly for endpoints that list posts spanning multiple communities
// (Feed, Saved Posts) — mirrors the per-item assembly PostEndpoints.ListPostsAsync does inline
// for single-community listings, factored out here since community name has to be resolved
// per-item instead of once for the whole page.
internal static class PostResponseAssembly
{
    public static async Task<List<PostResponse>> BuildAsync(
        IReadOnlyList<Post> posts,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        Guid viewerId,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { Username = u.Username ?? string.Empty, u.DisplayName, u.AvatarSeed }, cancellationToken);

        var communityIds = posts.Select(p => p.CommunityId).Distinct().ToList();
        var communityNames = await dbContext.Communities
            .Where(c => communityIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var flairIds = posts.Where(p => p.FlairId is not null).Select(p => p.FlairId!.Value);
        var flairMap = await flairService.GetResponseMapAsync(flairIds, cancellationToken);

        var postIds = posts.Select(p => p.Id).ToList();
        var imagesByPost = await dbContext.PostImages
            .Where(i => postIds.Contains(i.PostId))
            .OrderBy(i => i.Position)
            .GroupBy(i => i.PostId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<string>)g.Select(i => i.Url).ToList(), cancellationToken);

        var scores = await votingService.GetScoresAsync(VoteTargetType.Post, postIds, cancellationToken);
        var commentCounts = await commentService.GetCommentCountsAsync(postIds, cancellationToken);
        var reactionsByPost = await reactionService.GetSummariesAsync(ReactionTargetType.Post, postIds, viewerId, cancellationToken);
        var viewerVotes = await votingService.GetViewerVotesAsync(VoteTargetType.Post, postIds, viewerId, cancellationToken);

        var responses = new List<PostResponse>(posts.Count);
        foreach (var post in posts)
        {
            var score = scores.GetValueOrDefault(post.Id);
            var commentCount = commentCounts.GetValueOrDefault(post.Id);
            var reactions = reactionsByPost.GetValueOrDefault(post.Id, []);
            var viewerVote = viewerVotes.TryGetValue(post.Id, out var vote) ? vote : (sbyte?)null;
            var flair = post.FlairId is not null ? flairMap.GetValueOrDefault(post.FlairId.Value) : null;
            var author = authors.GetValueOrDefault(post.AuthorId);
            var imageUrls = imagesByPost.GetValueOrDefault(post.Id, []);
            responses.Add(PostResponse.FromEntity(
                post, communityNames.GetValueOrDefault(post.CommunityId, string.Empty),
                author?.Username ?? string.Empty, imageUrls, score, commentCount,
                flair: flair, reactions: reactions, authorAvatarSeed: author?.AvatarSeed,
                authorDisplayName: author?.DisplayName, viewerVote: viewerVote));
        }

        return responses;
    }
}
