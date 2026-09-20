using AnonyMeow.Common.Middleware;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Users;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Endpoints;

public static class UserEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api");

        group.MapPost("/auth/complete-profile", CompleteProfileAsync)
            .RequireAuthorization("AuthenticatedOnly")
            .WithName("CompleteProfile");

        group.MapGet("/users/me", GetMeAsync)
            .WithName("GetMe");

        group.MapPatch("/users/me", UpdateMeAsync)
            .WithName("UpdateMe");

        group.MapGet("/users/check-username", CheckUsernameAsync)
            .RequireAuthorization("AuthenticatedOnly")
            .WithName("CheckUsername");

        group.MapGet("/users/{username}", GetByUsernameAsync)
            .WithName("GetUserByUsername")
            .AllowAnonymous();

        group.MapGet("/users/{username}/posts", GetUserPostsAsync)
            .WithName("GetUserPosts")
            .AllowAnonymous();

        group.MapGet("/users/{username}/comments", GetUserCommentsAsync)
            .WithName("GetUserComments")
            .AllowAnonymous();

        group.MapGet("/users/{username}/communities", GetUserCommunitiesAsync)
            .WithName("GetUserCommunities")
            .AllowAnonymous();

        return app;
    }

    private static async Task<Results<Ok<UserResponse>, JsonHttpResult<HttpValidationProblemDetails>>> CompleteProfileAsync(
        CompleteProfileRequest request,
        ICurrentUserAccessor currentUserAccessor,
        IUsernameReservationService usernameReservationService,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Username) || !usernameReservationService.IsValidFormat(request.Username))
        {
            errors["username"] =
                ["Username must be 3-20 characters: lowercase letters, numbers, and underscores only."];
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["displayName"] = ["Display name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.AvatarSeed))
        {
            errors["avatarSeed"] = ["Avatar seed is required."];
        }

        if (errors.Count > 0)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var user = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await usernameReservationService.ReserveAsync(user, request.Username, cancellationToken);

        user.DisplayName = request.DisplayName;
        user.AvatarSeed = request.AvatarSeed;
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(UserResponse.FromEntity(user));
    }

    private static async Task<Ok<UserResponse>> GetMeAsync(
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var user = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        return TypedResults.Ok(UserResponse.FromEntity(user));
    }

    private static async Task<Ok<UserResponse>> UpdateMeAsync(
        UpdateProfileRequest request,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var user = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);

        if (request.DisplayName is not null)
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.AvatarSeed is not null)
        {
            user.AvatarSeed = request.AvatarSeed;
        }

        if (request.FriendListVisibility is not null)
        {
            user.FriendListVisibility = request.FriendListVisibility.Value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(UserResponse.FromEntity(user));
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetByUsernameAsync(
        string username,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var normalized = username.ToLowerInvariant();
        var user = await dbContext.Users.SingleOrDefaultAsync(
            u => u.Username != null && u.Username.ToLower() == normalized,
            cancellationToken);

        return user is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(UserResponse.FromEntity(user));
    }

    private static async Task<Ok<UsernameAvailabilityResponse>> CheckUsernameAsync(
        string value,
        IUsernameReservationService usernameReservationService,
        CancellationToken cancellationToken)
    {
        var isAvailable = await usernameReservationService.IsAvailableAsync(value, cancellationToken);
        return TypedResults.Ok(new UsernameAvailabilityResponse(isAvailable));
    }

    private static async Task<Ok<PagedResponse<PostResponse>>> GetUserPostsAsync(
        string username,
        IPostService postService,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var (items, totalCount) = await postService.ListByAuthorUsernameAsync(
            username, currentUser?.Id, page, DefaultPageSize, cancellationToken);

        var normalizedUsername = username.ToLowerInvariant();
        var author = await dbContext.Users
            .Where(u => u.Username != null && u.Username.ToLower() == normalizedUsername)
            .Select(u => new { u.AvatarSeed, u.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);
        var authorAvatarSeed = author?.AvatarSeed;
        var authorDisplayName = author?.DisplayName;

        var communityIds = items.Select(p => p.CommunityId).Distinct().ToList();
        var communityNames = await dbContext.Communities
            .Where(c => communityIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var flairIds = items.Where(p => p.FlairId is not null).Select(p => p.FlairId!.Value);
        var flairMap = await flairService.GetResponseMapAsync(flairIds, cancellationToken);
        var postIds = items.Select(p => p.Id).ToList();
        var imagesByPost = await dbContext.PostImages
            .Where(i => postIds.Contains(i.PostId))
            .OrderBy(i => i.Position)
            .GroupBy(i => i.PostId)
            .ToDictionaryAsync(g => g.Key, g => (IReadOnlyList<string>)g.Select(i => i.Url).ToList(), cancellationToken);

        var responses = new List<PostResponse>(items.Count);
        foreach (var post in items)
        {
            var score = await votingService.GetScoreAsync(VoteTargetType.Post, post.Id, cancellationToken);
            var commentCount = await commentService.GetCommentCountAsync(post.Id, cancellationToken);
            var reactions = await reactionService.GetSummaryAsync(
                ReactionTargetType.Post, post.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
            var viewerVote = await votingService.GetViewerVoteAsync(
                VoteTargetType.Post, post.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
            var flair = post.FlairId is not null ? flairMap.GetValueOrDefault(post.FlairId.Value) : null;
            var imageUrls = imagesByPost.GetValueOrDefault(post.Id, []);
            responses.Add(PostResponse.FromEntity(
                post, communityNames.GetValueOrDefault(post.CommunityId, string.Empty), username, imageUrls, score, commentCount,
                flair: flair, reactions: reactions, authorAvatarSeed: authorAvatarSeed, authorDisplayName: authorDisplayName,
                viewerVote: viewerVote));
        }

        return TypedResults.Ok(new PagedResponse<PostResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Ok<PagedResponse<CommentResponse>>> GetUserCommentsAsync(
        string username,
        ICommentService commentService,
        IVotingService votingService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var (items, totalCount) = await commentService.ListByAuthorUsernameAsync(
            username, currentUser?.Id, page, DefaultPageSize, cancellationToken);

        var normalizedUsername = username.ToLowerInvariant();
        var author = await dbContext.Users
            .Where(u => u.Username != null && u.Username.ToLower() == normalizedUsername)
            .Select(u => new { u.AvatarSeed, u.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);
        var authorAvatarSeed = author?.AvatarSeed;
        var authorDisplayName = author?.DisplayName;

        var responses = new List<CommentResponse>(items.Count);
        foreach (var comment in items)
        {
            var score = await votingService.GetScoreAsync(VoteTargetType.Comment, comment.Id, cancellationToken);
            var replyCount = await commentService.GetReplyCountAsync(comment.Id, cancellationToken);
            var reactions = await reactionService.GetSummaryAsync(
                ReactionTargetType.Comment, comment.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
            var viewerVote = await votingService.GetViewerVoteAsync(
                VoteTargetType.Comment, comment.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
            var ancestorChain = await commentService.GetAncestorChainAsync(comment.Id, cancellationToken);
            responses.Add(new CommentResponse(
                comment.Id, username, authorDisplayName, comment.BodyMarkdown, score, replyCount, comment.CreatedAtUtc, comment.EditedAtUtc,
                reactions, comment.PostId, ancestorChain, authorAvatarSeed, ViewerVote: viewerVote));
        }

        return TypedResults.Ok(new PagedResponse<CommentResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Ok<PagedResponse<CommunityMembershipResponse>>> GetUserCommunitiesAsync(
        string username,
        ICommunityService communityService,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var (items, totalCount) = await communityService.ListJoinedByUsernameAsync(
            username, page, DefaultPageSize, cancellationToken);

        return TypedResults.Ok(new PagedResponse<CommunityMembershipResponse>(items, page, DefaultPageSize, totalCount));
    }
}
