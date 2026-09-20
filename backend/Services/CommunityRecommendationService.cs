using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class CommunityRecommendationService(AppDbContext dbContext) : ICommunityRecommendationService
{
    public async Task<(IReadOnlyList<Community> Items, int TotalCount)> GetRecommendedCommunitiesAsync(
        Guid? userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Communities.AsQueryable();
        if (userId is { } viewerId)
        {
            var joinedCommunityIds = dbContext.CommunityMemberships
                .Where(m => m.AppUserId == viewerId)
                .Select(m => m.CommunityId);
            query = query.Where(c => !joinedCommunityIds.Contains(c.Id));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => dbContext.CommunityMemberships.Count(m => m.CommunityId == c.Id))
            .ThenByDescending(c => c.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
