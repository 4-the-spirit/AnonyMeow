using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.Ranking;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class DiscoveryService(AppDbContext dbContext, IRankingService rankingService) : IDiscoveryService
{
    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetTrendingPostsAsync(
        Guid? communityId, TrendingWindow window, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var windowSpan = window == TrendingWindow.Week ? TimeSpan.FromDays(7) : TimeSpan.FromDays(1);
        var cutoff = DateTimeOffset.UtcNow - windowSpan;

        var query = dbContext.Posts.Where(p => !p.IsRemoved && p.CreatedAtUtc >= cutoff);
        if (communityId is not null)
        {
            query = query.Where(p => p.CommunityId == communityId);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await rankingService.ApplyPostSort(query, SortOrder.Trending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
