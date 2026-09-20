using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface ISearchService
{
    Task<(IReadOnlyList<Post> Items, int TotalCount)> SearchPostsAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Comment> Items, int TotalCount)> SearchCommentsAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Community> Items, int TotalCount)> SearchCommunitiesAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default);
}
