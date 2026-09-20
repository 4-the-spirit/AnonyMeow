using AnonyMeow.Common.Middleware;
using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services;
using AnonyMeow.Services.PostValidation;
using AnonyMeow.Services.Ranking;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Endpoints;

public static class PostEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/communities/{name}/posts", CreatePostAsync).WithName("CreatePost").RequireRateLimiting("CreatePost");
        app.MapGet("/api/communities/{name}/posts", ListPostsAsync).WithName("ListCommunityPosts").AllowAnonymous();
        app.MapGet("/api/posts/{id:guid}", GetPostAsync).WithName("GetPost").AllowAnonymous();
        app.MapPatch("/api/posts/{id:guid}", UpdatePostAsync).WithName("UpdatePost");
        app.MapDelete("/api/posts/{id:guid}", DeletePostAsync).WithName("DeletePost");
        app.MapPost("/api/posts/{id:guid}/poll-votes", CastPollVoteAsync).WithName("CastPollVote");
        app.MapPut("/api/posts/{id:guid}/vote", CastVoteAsync).WithName("CastPostVote").RequireRateLimiting("Vote");
        app.MapDelete("/api/posts/{id:guid}/vote", RemoveVoteAsync).WithName("RemovePostVote");
        app.MapPost("/api/uploads/images/sas", CreateImageUploadSasAsync).WithName("CreateImageUploadSas").RequireRateLimiting("ImageUpload");
        app.MapPatch("/api/posts/{id:guid}/flair", UpdatePostFlairAsync).WithName("UpdatePostFlair");
        app.MapPut("/api/posts/{id:guid}/reactions/{emoji}", AddPostReactionAsync).WithName("AddPostReaction");
        app.MapDelete("/api/posts/{id:guid}/reactions/{emoji}", RemovePostReactionAsync).WithName("RemovePostReaction");

        return app;
    }

    private static async Task<Results<Created<PostResponse>, JsonHttpResult<HttpValidationProblemDetails>, NotFound>> CreatePostAsync(
        string name,
        CreatePostRequest request,
        ICommunityService communityService,
        IPostRequestValidator validator,
        IPostService postService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        var errors = validator.Validate(request);
        if (errors.Count > 0)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.CreateAsync(community.Id, currentUser.Id, request, cancellationToken);

        var pollOptions = await postService.GetPollOptionsAsync(post.Id, cancellationToken);

        return TypedResults.Created(
            $"/api/posts/{post.Id}",
            PostResponse.FromEntity(
                post, community.Name, currentUser.Username ?? string.Empty, request.ImageUrls ?? [],
                score: 0, commentCount: 0, pollOptions.Count > 0 ? pollOptions : null,
                authorAvatarSeed: currentUser.AvatarSeed, authorDisplayName: currentUser.DisplayName));
    }

    private static async Task<Results<Ok<PagedResponse<PostResponse>>, NotFound>> ListPostsAsync(
        string name,
        ICommunityService communityService,
        IPostService postService,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        string? sort = null,
        int page = 1)
    {
        var community = await communityService.GetEntityByNameAsync(name, cancellationToken);
        if (community is null)
        {
            return TypedResults.NotFound();
        }

        page = Math.Max(page, 1);
        var (items, totalCount) = await postService.ListByCommunityAsync(
            community.Id, page, DefaultPageSize, SortOrderParser.Parse(sort), cancellationToken);

        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var authorIds = items.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await dbContext.Users
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { Username = u.Username ?? string.Empty, u.DisplayName, u.AvatarSeed }, cancellationToken);
        var flairIds = items.Where(p => p.FlairId is not null).Select(p => p.FlairId!.Value);
        var flairMap = await flairService.GetResponseMapAsync(flairIds, cancellationToken);
        var postIds = items.Select(p => p.Id).ToList();
        var imagesByPost = await dbContext.PostImages
            .Where(i => postIds.Contains(i.PostId))
            .OrderBy(i => i.Position)
            .GroupBy(i => i.PostId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<string>)g.Select(i => i.Url).ToList(), cancellationToken);

        var viewerId = currentUser?.Id ?? Guid.Empty;
        var scores = await votingService.GetScoresAsync(VoteTargetType.Post, postIds, cancellationToken);
        var commentCounts = await commentService.GetCommentCountsAsync(postIds, cancellationToken);
        var reactionsByPost = await reactionService.GetSummariesAsync(ReactionTargetType.Post, postIds, viewerId, cancellationToken);
        var viewerVotes = await votingService.GetViewerVotesAsync(VoteTargetType.Post, postIds, viewerId, cancellationToken);

        var responses = new List<PostResponse>(items.Count);
        foreach (var post in items)
        {
            var score = scores.GetValueOrDefault(post.Id);
            var commentCount = commentCounts.GetValueOrDefault(post.Id);
            var reactions = reactionsByPost.GetValueOrDefault(post.Id, []);
            var viewerVote = viewerVotes.TryGetValue(post.Id, out var vote) ? vote : (sbyte?)null;
            var flair = post.FlairId is not null ? flairMap.GetValueOrDefault(post.FlairId.Value) : null;
            var author = authors.GetValueOrDefault(post.AuthorId);
            var imageUrls = imagesByPost.GetValueOrDefault(post.Id, []);
            responses.Add(PostResponse.FromEntity(
                post, community.Name, author?.Username ?? string.Empty, imageUrls, score, commentCount,
                flair: flair, reactions: reactions, authorAvatarSeed: author?.AvatarSeed,
                authorDisplayName: author?.DisplayName, viewerVote: viewerVote));
        }

        return TypedResults.Ok(new PagedResponse<PostResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Results<Ok<PostResponse>, NotFound>> GetPostAsync(
        Guid id,
        IPostService postService,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser?.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        var community = await dbContext.Communities.FindAsync([post.CommunityId], cancellationToken);
        var author = await dbContext.Users.FindAsync([post.AuthorId], cancellationToken);
        var pollOptions = await postService.GetPollOptionsAsync(post.Id, cancellationToken);
        var imageUrls = await postService.GetImageUrlsAsync(post.Id, cancellationToken);
        var score = await votingService.GetScoreAsync(VoteTargetType.Post, post.Id, cancellationToken);
        var commentCount = await commentService.GetCommentCountAsync(post.Id, cancellationToken);
        var flair = await flairService.GetResponseForPostAsync(post, cancellationToken);
        var reactions = await reactionService.GetSummaryAsync(ReactionTargetType.Post, post.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
        var viewerVote = await votingService.GetViewerVoteAsync(VoteTargetType.Post, post.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);

        return TypedResults.Ok(PostResponse.FromEntity(
            post, community?.Name ?? string.Empty, author?.Username ?? string.Empty, imageUrls, score, commentCount,
            pollOptions.Count > 0 ? pollOptions : null, flair, reactions, author?.AvatarSeed,
            authorDisplayName: author?.DisplayName, viewerVote: viewerVote));
    }

    private static async Task<Results<Ok<PostResponse>, NotFound, ForbidHttpResult, JsonHttpResult<HttpValidationProblemDetails>>> UpdatePostAsync(
        Guid id,
        UpdatePostRequest request,
        IPostService postService,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        PostValidationHelpers.ValidateTitleIfProvided(request.Title, errors);
        PostValidationHelpers.ValidateBodyLength(request.BodyMarkdown, errors);
        if (errors.Count > 0)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        if (post.AuthorId != currentUser.Id)
        {
            return TypedResults.Forbid();
        }

        await postService.UpdateAsync(post, request.Title, request.BodyMarkdown, request.Url, cancellationToken);

        var community = await dbContext.Communities.FindAsync([post.CommunityId], cancellationToken);
        var pollOptions = await postService.GetPollOptionsAsync(post.Id, cancellationToken);
        var imageUrls = await postService.GetImageUrlsAsync(post.Id, cancellationToken);
        var score = await votingService.GetScoreAsync(VoteTargetType.Post, post.Id, cancellationToken);
        var commentCount = await commentService.GetCommentCountAsync(post.Id, cancellationToken);
        var flair = await flairService.GetResponseForPostAsync(post, cancellationToken);
        var reactions = await reactionService.GetSummaryAsync(ReactionTargetType.Post, post.Id, currentUser.Id, cancellationToken);
        var viewerVote = await votingService.GetViewerVoteAsync(VoteTargetType.Post, post.Id, currentUser.Id, cancellationToken);

        return TypedResults.Ok(PostResponse.FromEntity(
            post, community?.Name ?? string.Empty, currentUser.Username ?? string.Empty, imageUrls, score, commentCount,
            pollOptions.Count > 0 ? pollOptions : null, flair: flair, reactions: reactions, authorAvatarSeed: currentUser.AvatarSeed,
            authorDisplayName: currentUser.DisplayName, viewerVote: viewerVote));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeletePostAsync(
        Guid id,
        IPostService postService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        if (post.AuthorId != currentUser.Id)
        {
            return TypedResults.Forbid();
        }

        await postService.SoftDeleteAsync(post, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IReadOnlyList<PollOptionResponse>>, NotFound>> CastPollVoteAsync(
        Guid id,
        CastPollVoteRequest request,
        IPostService postService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        var options = await postService.CastPollVoteAsync(post.Id, currentUser.Id, request.PollOptionId, cancellationToken);
        return TypedResults.Ok(options);
    }

    private static async Task<Results<NoContent, NotFound>> CastVoteAsync(
        Guid id,
        CastVoteRequest request,
        IPostService postService,
        IVotingService votingService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        await votingService.CastVoteAsync(VoteTargetType.Post, id, currentUser.Id, request.Value, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveVoteAsync(
        Guid id,
        IPostService postService,
        IVotingService votingService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        await votingService.RemoveVoteAsync(VoteTargetType.Post, id, currentUser.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ImageUploadSasResponse>> CreateImageUploadSasAsync(
        IImageUploadService imageUploadService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var (uploadUrl, blobUrl) = await imageUploadService.CreateUploadSasAsync(currentUser.Id, cancellationToken);
        return TypedResults.Ok(new ImageUploadSasResponse(uploadUrl, blobUrl));
    }

    private static async Task<Results<Ok<PostResponse>, NotFound, ForbidHttpResult, JsonHttpResult<HttpValidationProblemDetails>>> UpdatePostFlairAsync(
        Guid id,
        UpdatePostFlairRequest request,
        IPostService postService,
        IFlairService flairService,
        IVotingService votingService,
        ICommentService commentService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request.FlairId == Guid.Empty)
        {
            return ValidationProblemFactory.Create("flairId", "A tag is required.");
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        if (post.AuthorId != currentUser.Id)
        {
            return TypedResults.Forbid();
        }

        await flairService.AssignToPostAsync(post, request.FlairId, cancellationToken);

        var community = await dbContext.Communities.FindAsync([post.CommunityId], cancellationToken);
        var pollOptions = await postService.GetPollOptionsAsync(post.Id, cancellationToken);
        var imageUrls = await postService.GetImageUrlsAsync(post.Id, cancellationToken);
        var score = await votingService.GetScoreAsync(VoteTargetType.Post, post.Id, cancellationToken);
        var commentCount = await commentService.GetCommentCountAsync(post.Id, cancellationToken);
        var flair = await flairService.GetResponseForPostAsync(post, cancellationToken);
        var reactions = await reactionService.GetSummaryAsync(ReactionTargetType.Post, post.Id, currentUser.Id, cancellationToken);
        var viewerVote = await votingService.GetViewerVoteAsync(VoteTargetType.Post, post.Id, currentUser.Id, cancellationToken);

        return TypedResults.Ok(PostResponse.FromEntity(
            post, community?.Name ?? string.Empty, currentUser.Username ?? string.Empty, imageUrls, score, commentCount,
            pollOptions.Count > 0 ? pollOptions : null, flair: flair, reactions: reactions, authorAvatarSeed: currentUser.AvatarSeed,
            authorDisplayName: currentUser.DisplayName, viewerVote: viewerVote));
    }

    private static async Task<Results<NoContent, NotFound, JsonHttpResult<HttpValidationProblemDetails>>> AddPostReactionAsync(
        Guid id,
        string emoji,
        IPostService postService,
        IReactionService reactionService,
        IOptions<ReactionOptions> reactionOptions,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        if (!reactionOptions.Value.AllowedEmojis.Contains(emoji))
        {
            return ValidationProblemFactory.Create("emoji", "This emoji is not in the allowed reaction set.");
        }

        await reactionService.AddAsync(ReactionTargetType.Post, id, currentUser.Id, emoji, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemovePostReactionAsync(
        Guid id,
        string emoji,
        IPostService postService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        await reactionService.RemoveAsync(ReactionTargetType.Post, id, currentUser.Id, emoji, cancellationToken);
        return TypedResults.NoContent();
    }
}
