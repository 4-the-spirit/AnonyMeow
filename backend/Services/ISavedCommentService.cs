using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface ISavedCommentService
{
    // Idempotent — saving an already-saved comment is a no-op.
    Task SaveAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default);

    // Idempotent — unsaving a comment that isn't saved is a no-op.
    Task UnsaveAsync(Guid userId, Guid commentId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListSavedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
