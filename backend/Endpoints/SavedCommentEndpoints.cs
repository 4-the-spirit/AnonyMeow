using AnonyMeow.Data;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AnonyMeow.Endpoints;

public static class SavedCommentEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapSavedCommentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/comments/{id:guid}/save", SaveCommentAsync).WithName("SaveComment");
        app.MapDelete("/api/comments/{id:guid}/save", UnsaveCommentAsync).WithName("UnsaveComment");
        app.MapGet("/api/users/me/saved-comments", ListSavedCommentsAsync).WithName("ListSavedComments");

        return app;
    }

    private static async Task<Results<NoContent, NotFound>> SaveCommentAsync(
        Guid id,
        ICommentService commentService,
        ISavedCommentService savedCommentService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await savedCommentService.SaveAsync(currentUser.Id, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> UnsaveCommentAsync(
        Guid id,
        ICommentService commentService,
        ISavedCommentService savedCommentService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var comment = await commentService.GetByIdAsync(id, cancellationToken);
        if (comment is null)
        {
            return TypedResults.NotFound();
        }

        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await savedCommentService.UnsaveAsync(currentUser.Id, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResponse<CommentResponse>>> ListSavedCommentsAsync(
        ISavedCommentService savedCommentService,
        ICommentService commentService,
        IVotingService votingService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var (items, totalCount) = await savedCommentService.ListSavedAsync(currentUser.Id, page, DefaultPageSize, cancellationToken);

        var responses = await CommentResponseAssembly.BuildAsync(
            items, commentService, votingService, reactionService, currentUser.Id, dbContext, cancellationToken);

        return TypedResults.Ok(new PagedResponse<CommentResponse>(responses, page, DefaultPageSize, totalCount));
    }
}
