using AnonyMeow.Common.Middleware;
using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Comments;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Dtos.Search;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace AnonyMeow.Endpoints;

public static class SearchEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/search", SearchAsync).WithName("Search").AllowAnonymous();

        return app;
    }

    private static async Task<Results<Ok<SearchResponse>, JsonHttpResult<HttpValidationProblemDetails>>> SearchAsync(
        ISearchService searchService,
        ICommunityService communityService,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        string? q = null,
        string? type = null,
        int page = 1)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return ValidationProblemFactory.Create("q", "Search query is required.");
        }

        page = Math.Max(page, 1);
        var target = Enum.TryParse<SearchTargetType>(type, ignoreCase: true, out var parsedTarget)
            ? parsedTarget
            : SearchTargetType.All;

        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var viewerId = currentUser?.Id ?? Guid.Empty;

        var posts = (IReadOnlyList<PostResponse>)[];
        var postsTotalCount = 0;
        if (target is SearchTargetType.All or SearchTargetType.Posts)
        {
            var (items, totalCount) = await searchService.SearchPostsAsync(q, page, DefaultPageSize, cancellationToken);
            posts = await PostResponseAssembly.BuildAsync(
                items, votingService, commentService, flairService, reactionService, viewerId, dbContext, cancellationToken);
            postsTotalCount = totalCount;
        }

        var comments = (IReadOnlyList<CommentResponse>)[];
        var commentsTotalCount = 0;
        if (target is SearchTargetType.All or SearchTargetType.Comments)
        {
            var (items, totalCount) = await searchService.SearchCommentsAsync(q, page, DefaultPageSize, cancellationToken);
            comments = await CommentResponseAssembly.BuildAsync(
                items, commentService, votingService, reactionService, viewerId, dbContext, cancellationToken);
            commentsTotalCount = totalCount;
        }

        var communities = new List<CommunityResponse>();
        var communitiesTotalCount = 0;
        if (target is SearchTargetType.All or SearchTargetType.Communities)
        {
            var (items, totalCount) = await searchService.SearchCommunitiesAsync(q, page, DefaultPageSize, cancellationToken);
            foreach (var community in items)
            {
                var memberCount = await communityService.GetMemberCountAsync(community.Id, cancellationToken);
                communities.Add(CommunityResponse.FromEntity(community, memberCount));
            }

            communitiesTotalCount = totalCount;
        }

        return TypedResults.Ok(new SearchResponse(
            posts, postsTotalCount, comments, commentsTotalCount, communities, communitiesTotalCount));
    }
}
