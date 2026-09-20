using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface IFriendshipService
{
    // Throws SelfFriendRequestException if requesterId == addresseeId, or
    // FriendshipAlreadyExistsException if a Pending/Accepted friendship already exists in
    // either direction between the two users.
    Task<Friendship> RequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

    // A pending request where requesterId sent it to addresseeId (direction matters: only the
    // addressee may accept/decline).
    Task<Friendship?> GetPendingRequestAsync(
        Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

    Task AcceptAsync(Friendship friendship, CancellationToken cancellationToken = default);

    Task DeclineAsync(Friendship friendship, CancellationToken cancellationToken = default);

    // No-op (not an error) if no accepted friendship exists between the two users.
    Task RemoveAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);

    // Withdraws a still-Pending request requesterId sent to addresseeId. No-op (not an error)
    // if no such pending request exists.
    Task CancelRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default);

    Task<bool> AreFriendsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<(AppUser Friend, DateTimeOffset FriendsSinceUtc)>> ListFriendsAsync(
        Guid userId, CancellationToken cancellationToken = default);

    // Pending (not yet Accepted/Declined) requests split by direction relative to userId.
    Task<(IReadOnlyList<(AppUser OtherUser, DateTimeOffset CreatedAtUtc)> Incoming,
        IReadOnlyList<(AppUser OtherUser, DateTimeOffset CreatedAtUtc)> Outgoing)> ListPendingAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
