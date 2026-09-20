using AnonyMeow.Data;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AnonyMeow.Endpoints;

public static class SavedPostEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapSavedPostEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/posts/{id:guid}/save", SavePostAsync).WithName("SavePost");
        app.MapDelete("/api/posts/{id:guid}/save", UnsavePostAsync).WithName("UnsavePost");
        app.MapGet("/api/users/me/saved-posts", ListSavedPostsAsync).WithName("ListSavedPosts");

        return app;
    }

    private static async Task<Results<NoContent, NotFound>> SavePostAsync(
        Guid id,
        IPostService postService,
        ISavedPostService savedPostService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        await savedPostService.SaveAsync(currentUser.Id, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> UnsavePostAsync(
        Guid id,
        IPostService postService,
        ISavedPostService savedPostService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var post = await postService.GetByIdAsync(id, currentUser.Id, cancellationToken);
        if (post is null)
        {
            return TypedResults.NotFound();
        }

        await savedPostService.UnsaveAsync(currentUser.Id, id, cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResponse<PostResponse>>> ListSavedPostsAsync(
        ISavedPostService savedPostService,
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
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var (items, totalCount) = await savedPostService.ListSavedAsync(currentUser.Id, page, DefaultPageSize, cancellationToken);

        var responses = await PostResponseAssembly.BuildAsync(
            items, votingService, commentService, flairService, reactionService, currentUser.Id, dbContext, cancellationToken);

        return TypedResults.Ok(new PagedResponse<PostResponse>(responses, page, DefaultPageSize, totalCount));
    }
}
