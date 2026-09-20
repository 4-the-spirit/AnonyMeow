using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface ICommunityRecommendationService
{
    // Simple popularity heuristic (member count), not ML — excludes communities the user already
    // joined. A null userId (anonymous viewer) skips the exclusion entirely.
    Task<(IReadOnlyList<Community> Items, int TotalCount)> GetRecommendedCommunitiesAsync(
        Guid? userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
