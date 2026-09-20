using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

public interface IFeedService
{
    // Posts from communities the user has joined, ordered per the same ISortStrategy family
    // Phase 1.4 uses for community post listings. A null userId (anonymous visitor) falls back to
    // posts from every community, since there's no membership set to scope the feed to.
    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetFeedAsync(
        Guid? userId, SortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken = default);
}
