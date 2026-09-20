using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services.Ranking;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class FeedService(AppDbContext dbContext, IRankingService rankingService) : IFeedService
{
    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetFeedAsync(
        Guid? userId, SortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Posts.Where(p => !p.IsRemoved);
        if (userId is { } id)
        {
            var communityIds = dbContext.CommunityMemberships
                .Where(m => m.AppUserId == id)
                .Select(m => m.CommunityId);
            query = query.Where(p => communityIds.Contains(p.CommunityId));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await rankingService.ApplyPostSort(query, sortOrder)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
