using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

public interface ICommentService
{
    // Throws PostNotFoundException, PostLockedException, or InvalidParentCommentException
    // (parent belongs to a different post) as appropriate.
    Task<Comment> CreateAsync(
        Guid postId, Guid authorId, string bodyMarkdown, Guid? parentCommentId, CancellationToken cancellationToken = default);

    Task<Comment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListTopLevelAsync(
        Guid postId, SortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListRepliesAsync(
        Guid parentCommentId, SortOrder sortOrder, int page, int pageSize, CancellationToken cancellationToken = default);

    // Profile activity listing: excludes IsRemoved comments unless viewerId is the author.
    Task<(IReadOnlyList<Comment> Items, int TotalCount)> ListByAuthorUsernameAsync(
        string username, Guid? viewerId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> GetReplyCountAsync(Guid commentId, CancellationToken cancellationToken = default);

    // Walks ParentCommentId up from the comment to its root, returning IDs ordered
    // root -> immediate parent (empty for a top-level comment). Used to let the frontend
    // auto-expand a reply's thread when deep-linking to it from elsewhere (e.g. a profile page).
    Task<IReadOnlyList<Guid>> GetAncestorChainAsync(Guid commentId, CancellationToken cancellationToken = default);

    Task<int> GetCommentCountAsync(Guid postId, CancellationToken cancellationToken = default);

    // Batch forms of GetReplyCountAsync/GetCommentCountAsync/GetAncestorChainAsync for list
    // endpoints — a handful of queries for the whole page instead of several per item. Ids absent
    // from a result dictionary mean a count of 0 (or an empty ancestor chain).
    Task<IReadOnlyDictionary<Guid, int>> GetReplyCountsAsync(
        IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetCommentCountsAsync(
        IReadOnlyCollection<Guid> postIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetAncestorChainsAsync(
        IReadOnlyCollection<Guid> commentIds, CancellationToken cancellationToken = default);

    Task UpdateAsync(Comment comment, string bodyMarkdown, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Comment comment, CancellationToken cancellationToken = default);
}
