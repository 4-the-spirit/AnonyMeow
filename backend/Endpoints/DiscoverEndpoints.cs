using AnonyMeow.Data;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Communities;
using AnonyMeow.Dtos.Posts;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AnonyMeow.Endpoints;

public static class DiscoverEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapDiscoverEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/discover/trending", GetTrendingAsync).WithName("GetTrending").AllowAnonymous();
        app.MapGet("/api/discover/recommended-communities", GetRecommendedCommunitiesAsync).WithName("GetRecommendedCommunities").AllowAnonymous();

        return app;
    }

    private static async Task<Results<Ok<PagedResponse<PostResponse>>, NotFound>> GetTrendingAsync(
        IDiscoveryService discoveryService,
        ICommunityService communityService,
        IVotingService votingService,
        ICommentService commentService,
        IFlairService flairService,
        IReactionService reactionService,
        ICurrentUserAccessor currentUserAccessor,
        AppDbContext dbContext,
        CancellationToken cancellationToken,
        string? scope = null,
        string? community = null,
        string? window = null,
        int page = 1)
    {
        page = Math.Max(page, 1);

        Guid? communityId = null;
        var parsedScope = Enum.TryParse<DiscoveryScope>(scope, ignoreCase: true, out var scopeValue)
            ? scopeValue
            : DiscoveryScope.Platform;

        if (parsedScope == DiscoveryScope.Community)
        {
            if (string.IsNullOrWhiteSpace(community))
            {
                return TypedResults.NotFound();
            }

            var communityEntity = await communityService.GetEntityByNameAsync(community, cancellationToken);
            if (communityEntity is null)
            {
                return TypedResults.NotFound();
            }

            communityId = communityEntity.Id;
        }

        var parsedWindow = Enum.TryParse<TrendingWindow>(window, ignoreCase: true, out var windowValue)
            ? windowValue
            : TrendingWindow.Day;

        var (items, totalCount) = await discoveryService.GetTrendingPostsAsync(
            communityId, parsedWindow, page, DefaultPageSize, cancellationToken);

        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var responses = await PostResponseAssembly.BuildAsync(
            items, votingService, commentService, flairService, reactionService, currentUser?.Id ?? Guid.Empty, dbContext, cancellationToken);

        return TypedResults.Ok(new PagedResponse<PostResponse>(responses, page, DefaultPageSize, totalCount));
    }

    private static async Task<Ok<PagedResponse<CommunityResponse>>> GetRecommendedCommunitiesAsync(
        ICommunityRecommendationService recommendationService,
        ICommunityService communityService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserOrNullAsync(cancellationToken);
        var (items, totalCount) = await recommendationService.GetRecommendedCommunitiesAsync(
            currentUser?.Id, page, DefaultPageSize, cancellationToken);

        var responses = new List<CommunityResponse>(items.Count);
        foreach (var community in items)
        {
            var memberCount = await communityService.GetMemberCountAsync(community.Id, cancellationToken);
            responses.Add(CommunityResponse.FromEntity(community, memberCount));
        }

        return TypedResults.Ok(new PagedResponse<CommunityResponse>(responses, page, DefaultPageSize, totalCount));
    }
}
