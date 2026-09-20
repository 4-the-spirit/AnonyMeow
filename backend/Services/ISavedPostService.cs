using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface ISavedPostService
{
    // Idempotent — saving an already-saved post is a no-op.
    Task SaveAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);

    // Idempotent — unsaving a post that isn't saved is a no-op.
    Task UnsaveAsync(Guid userId, Guid postId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> ListSavedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
