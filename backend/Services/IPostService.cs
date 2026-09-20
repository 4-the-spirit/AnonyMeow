using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Posts;

namespace AnonyMeow.Services;

public interface IPostService
{
    Task<Post> CreateAsync(
        Guid communityId, Guid authorId, CreatePostRequest request, CancellationToken cancellationToken = default);

    // Soft-delete visibility rule: null (as if not found) when the post is removed and the
    // viewer is neither the author nor a moderator of its community.
    Task<Post?> GetByIdAsync(Guid id, Guid? viewerId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> ListByCommunityAsync(
        Guid communityId, int page, int pageSize, SortOrder sortOrder, CancellationToken cancellationToken = default);

    // Profile activity listing: excludes IsRemoved posts unless viewerId is the author (their
    // own profile). Empty result (not an error) if the username doesn't exist.
    Task<(IReadOnlyList<Post> Items, int TotalCount)> ListByAuthorUsernameAsync(
        string username, Guid? viewerId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task UpdateAsync(Post post, string? title, string? bodyMarkdown, string? url, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Post post, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PollOptionResponse>> GetPollOptionsAsync(Guid postId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetImageUrlsAsync(Guid postId, CancellationToken cancellationToken = default);

    // Upserts respecting the unique (PostId, AppUserId) constraint — single-select, so a second
    // call from the same voter changes their selection rather than adding a second vote.
    Task<IReadOnlyList<PollOptionResponse>> CastPollVoteAsync(
        Guid postId, Guid voterId, Guid pollOptionId, CancellationToken cancellationToken = default);
}
