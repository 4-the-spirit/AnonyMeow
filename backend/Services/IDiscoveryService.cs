using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

public interface IDiscoveryService
{
    // communityId null == platform-wide trending; non-null scopes to a single community.
    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetTrendingPostsAsync(
        Guid? communityId, TrendingWindow window, int page, int pageSize, CancellationToken cancellationToken = default);
}
