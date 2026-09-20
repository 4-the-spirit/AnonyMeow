using AnonyMeow.Domain;
using AnonyMeow.Dtos.Flairs;

namespace AnonyMeow.Services;

public interface IFlairService
{
    Task<Flair> CreateAsync(
        Guid communityId, string name, string colorHex, Guid modId, CancellationToken cancellationToken = default);

    Task<Flair?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Flair>> ListForCommunityAsync(Guid communityId, CancellationToken cancellationToken = default);

    // Throws FlairImmutableException if flair.IsDefault.
    Task DeleteAsync(Flair flair, CancellationToken cancellationToken = default);

    // Throws FlairImmutableException if flair.IsDefault, FlairNameConflictException if another
    // flair in the same community already has `name`.
    Task<Flair> UpdateAsync(Flair flair, string name, string colorHex, CancellationToken cancellationToken = default);

    // Validates the flair (when non-null) belongs to the post's community before assigning.
    Task AssignToPostAsync(Post post, Guid? flairId, CancellationToken cancellationToken = default);

    // Throws FlairNotFoundException / FlairCommunityMismatchException if flairId doesn't resolve
    // to a flair belonging to communityId. Shared by AssignToPostAsync and post creation (a post
    // must always be created with a valid, same-community flair).
    Task EnsureValidForCommunityAsync(Guid flairId, Guid communityId, CancellationToken cancellationToken = default);

    Task<FlairResponse?> GetResponseForPostAsync(Post post, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, FlairResponse>> GetResponseMapAsync(
        IEnumerable<Guid> flairIds, CancellationToken cancellationToken = default);
}
