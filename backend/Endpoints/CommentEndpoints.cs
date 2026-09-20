using AnonyMeow.Common.Middleware;
using AnonyMeow.Common.Options;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Services;
using AnonyMeow.Services.CommentValidation;
using AnonyMeow.Services.Ranking;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AnonyMeow.Endpoints;

public static class CommentEndpoints
{
    private const int DefaultPageSize = 20;
    private const int DefaultReplyPageSize = 3;
    private const int MaxReplyPageSize = 100;

    public static IEndpointRouteBuilder MapCommentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/posts/{postId:guid}/comments", CreateCommentAsync).WithName("CreateComment").RequireRateLimiting("CreateComment");
        app.MapGet("/api/posts/{postId:guid}/comments", ListTopLevelCommentsAsync).WithName("ListPostComments").AllowAnonymous();
        app.MapGet("/api/comments/{id:guid}", GetCommentAsync).WithName("GetComment").AllowAnonymous();
        app.MapGet("/api/comments/{id:guid}/replies", ListRepliesAsync).WithName("ListCommentReplies").AllowAnonymous();
        app.MapPatch("/api/comments/{id:guid}", UpdateCommentAsync).WithName("UpdateComment");
        app.MapDelete("/api/comments/{id:guid}", DeleteCommentAsync).WithName("DeleteComment");
        app.MapPut("/api/comments/{id:guid}/vote", CastVoteAsync).WithName("CastCommentVote").RequireRateLimiting("Vote");
        app.MapDelete("/api/comments/{id:guid}/vote", RemoveVoteAsync).WithName("RemoveCommentVote");
        app.MapPut("/api/comments/{id:guid}/reactions/{emoji}", AddCommentReactionAsync).WithName("AddCommentReaction");
        app.MapDelete("/api/comments/{id:guid}/reactions/{emoji}", RemoveCommentReactionAsync).WithName("RemoveCommentReaction");

        return app;
    }

    private static async Task<Results<Created<CommentResponse>, NotFound, JsonHttpResult<HttpValidationProblemDetails>>> CreateCommentAsync(
        Guid postId,
        CreateCommentRequest request,
        ICommentService commentService,
        ICommentRequestValidator validator,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request.BodyMarkdown);
        if (errors.Count > 0)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var comment = await commentService.CreateAsync(
            postId, currentUser.Id, request.BodyMarkdown, request.ParentCommentId, cancellationToken);

        return TypedResults.Created(
            $"/api/comments/{comment.Id}",
            new CommentResponse(
                comment.Id, currentUser.Username ?? string.Empty, currentUser.DisplayName, comment.BodyMarkdown, 0, 0, comment.CreatedAtUtc, null,
                AuthorAvatarSeed: currentUser.AvatarSeed));
    }

    private static async Task<Ok<PagedResponse<CommentResponse>>> ListTopLevelCommentsAsync(
        Guid postId,
        ICommentService commentService,
        IVotingService votingService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        string? sort = null,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var (items, totalCount) = await commentService.ListTopLevelAsync(
            postId, SortOrderParser.Parse(sort), page, DefaultPageSize, cancellationToken);

        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var responses = await CommentResponseAssembly.BuildAsync(
            items, commentService, votingService, reactionService, currentUser?.Id ?? Guid.Empty, dbContext, cancellationToken);

        return TypedResults.Ok(new PagedResponse<CommentResponse>(responses, page, DefaultPageSize, totalCount));
    }

    /// <summary>
    /// Looks up any existing comment by id (not just the caller's own), always populating
    /// PostId/AncestorCommentIds — needed so a notification (e.g. a Reply/Mention pointing at
    /// someone else's comment) can deep-link to the right post and auto-expand its thread, the
    /// same way the profile "Comments" tab already does for the viewer's own comments.
    /// </summary>
    private static async Task<Results<Ok<CommentResponse>, NotFound>> GetCommentAsync(
        Guid id,
        ICommentService commentService,
        IVotingService votingService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null || comment.IsRemoved)
        {
            return TypedResults.NotFound();
        }

        var author = await dbContext.Users.FindAsync([comment.AuthorId], cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var score = await votingService.GetScoreAsync(VoteTargetType.Comment, comment.Id, cancellationToken);
        var replyCount = await commentService.GetReplyCountAsync(comment.Id, cancellationToken);
        var reactions = await reactionService.GetSummaryAsync(
            ReactionTargetType.Comment, comment.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
        var viewerVote = await votingService.GetViewerVoteAsync(
            VoteTargetType.Comment, comment.Id, currentUser?.Id ?? Guid.Empty, cancellationToken);
        var ancestorChain = await commentService.GetAncestorChainAsync(comment.Id, cancellationToken);

        return TypedResults.Ok(new CommentResponse(
            comment.Id, author?.Username ?? string.Empty, author?.DisplayName, comment.BodyMarkdown, score, replyCount,
            comment.CreatedAtUtc, comment.EditedAtUtc, reactions, comment.PostId, ancestorChain,
            author?.AvatarSeed, ViewerVote: viewerVote));
    }

    private static async Task<Ok<PagedResponse<CommentResponse>>> ListRepliesAsync(
        Guid id,
        ICommentService commentService,
        IVotingService votingService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        string? sort = null,
        int page = 1,
        int pageSize = DefaultReplyPageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxReplyPageSize);
        var (items, totalCount) = await commentService.ListRepliesAsync(
            id, SortOrderParser.Parse(sort), page, pageSize, cancellationToken);

        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var responses = await CommentResponseAssembly.BuildAsync(
            items, commentService, votingService, reactionService, currentUser?.Id ?? Guid.Empty, dbContext, cancellationToken);

        return TypedResults.Ok(new PagedResponse<CommentResponse>(responses, page, pageSize, totalCount));
    }

    private static async Task<Results<Ok<CommentResponse>, NotFound, ForbidHttpResult, JsonHttpResult<HttpValidationProblemDetails>>> UpdateCommentAsync(
        Guid id,
        UpdateCommentRequest request,
        ICommentService commentService,
        ICommentRequestValidator validator,
        IVotingService votingService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = validator.Validate(request.BodyMarkdown);
        if (errors.Count > 0)
        {
            return ValidationProblemFactory.Create(errors);
        }

        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (comment.AuthorId != currentUser.Id)
        {
            return TypedResults.Forbid();
        }

        await commentService.UpdateAsync(comment, request.BodyMarkdown, cancellationToken);

        var score = await votingService.GetScoreAsync(VoteTargetType.Comment, comment.Id, cancellationToken);
        var replyCount = await commentService.GetReplyCountAsync(comment.Id, cancellationToken);
        var reactions = await reactionService.GetSummaryAsync(
            ReactionTargetType.Comment, comment.Id, currentUser.Id, cancellationToken);
        var viewerVote = await votingService.GetViewerVoteAsync(
            VoteTargetType.Comment, comment.Id, currentUser.Id, cancellationToken);

        return TypedResults.Ok(new CommentResponse(
            comment.Id, currentUser.Username ?? string.Empty, currentUser.DisplayName, comment.BodyMarkdown, score, replyCount,
            comment.CreatedAtUtc, comment.EditedAtUtc, reactions,
            AuthorAvatarSeed: currentUser.AvatarSeed, ViewerVote: viewerVote));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteCommentAsync(
        Guid id,
        ICommentService commentService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (comment.AuthorId != currentUser.Id)
        {
            return TypedResults.Forbid();
        }

        await commentService.SoftDeleteAsync(comment, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> CastVoteAsync(
        Guid id,
        CastVoteRequest request,
        ICommentService commentService,
        IVotingService votingService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await votingService.CastVoteAsync(VoteTargetType.Comment, id, currentUser.Id, request.Value, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveVoteAsync(
        Guid id,
        ICommentService commentService,
        IVotingService votingService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await votingService.RemoveVoteAsync(VoteTargetType.Comment, id, currentUser.Id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound, JsonHttpResult<HttpValidationProblemDetails>>> AddCommentReactionAsync(
        Guid id,
        string emoji,
        ICommentService commentService,
        IReactionService reactionService,
        IOptions<ReactionOptions> reactionOptions,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        if (!reactionOptions.Value.AllowedEmojis.Contains(emoji))
        {
            return ValidationProblemFactory.Create("emoji", "This emoji is not in the allowed reaction set.");
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await reactionService.AddAsync(ReactionTargetType.Comment, id, currentUser.Id, emoji, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveCommentReactionAsync(
        Guid id,
        string emoji,
        ICommentService commentService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await reactionService.RemoveAsync(ReactionTargetType.Comment, id, currentUser.Id, emoji, cancellationToken);
        return TypedResults.NoContent();
    }
}
