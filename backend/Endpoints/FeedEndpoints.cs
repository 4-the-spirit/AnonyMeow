using AnonyMeow.Data;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services;
using AnonyMeow.Services.Ranking;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AnonyMeow.Endpoints;

public static class FeedEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feed", GetFeedAsync).WithName("GetFeed").AllowAnonymous();

        return app;
    }

    private static async Task<Ok<PagedResponse<PostResponse>>> GetFeedAsync(
        IFeedService feedService,
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
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var (items, totalCount) = await feedService.GetFeedAsync(
            currentUser?.Id, SortOrderParser.Parse(sort), page, DefaultPageSize, cancellationToken);

        var responses = await PostResponseAssembly.BuildAsync(
            items, votingService, commentService, flairService, reactionService, currentUser?.Id ?? Guid.Empty, dbContext, cancellationToken);

        return TypedResults.Ok(new PagedResponse<PostResponse>(responses, page, DefaultPageSize, totalCount));
    }
}
