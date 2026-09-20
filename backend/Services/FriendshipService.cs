using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class FriendshipService(AppDbContext dbContext, INotificationDispatcher notificationDispatcher) : IFriendshipService
{
    public async Task<Friendship> RequestAsync(
        Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default)
    {
        if (requesterId == addresseeId)
        {
            throw new SelfFriendRequestException();
        }

        var existing = await dbContext.Friendships.SingleOrDefaultAsync(
            f => (f.RequesterId == requesterId && f.AddresseeId == addresseeId) ||
                 (f.RequesterId == addresseeId && f.AddresseeId == requesterId),
            cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == FriendshipStatus.Declined)
            {
                dbContext.Friendships.Remove(existing);
            }
            else
            {
                throw new FriendshipAlreadyExistsException();
            }
        }

        var friendship = new Friendship
        {
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendshipStatus.Pending,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        dbContext.Friendships.Add(friendship);
        await dbContext.SaveChangesAsync(cancellationToken);

        var requester = await dbContext.Users.FindAsync([requesterId], cancellationToken);
        await notificationDispatcher.DispatchAsync(
            addresseeId, NotificationType.FriendRequest, NotificationSourceType.User, requesterId,
            $"{requester?.DisplayName ?? requester?.Username} sent you a friend request.", cancellationToken);

        return friendship;
    }

    public Task<Friendship?> GetPendingRequestAsync(
        Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default) =>
        dbContext.Friendships.SingleOrDefaultAsync(
            f => f.RequesterId == requesterId && f.AddresseeId == addresseeId && f.Status == FriendshipStatus.Pending,
            cancellationToken);

    public async Task AcceptAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        friendship.Status = FriendshipStatus.Accepted;
        friendship.RespondedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeclineAsync(Friendship friendship, CancellationToken cancellationToken = default)
    {
        friendship.Status = FriendshipStatus.Declined;
        friendship.RespondedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default)
    {
        var friendship = await dbContext.Friendships.SingleOrDefaultAsync(
            f => f.Status == FriendshipStatus.Accepted &&
                 ((f.RequesterId == userId && f.AddresseeId == otherUserId) ||
                  (f.RequesterId == otherUserId && f.AddresseeId == userId)),
            cancellationToken);
        if (friendship is null)
        {
            return;
        }

        dbContext.Friendships.Remove(friendship);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelRequestAsync(
        Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default)
    {
        var friendship = await dbContext.Friendships.SingleOrDefaultAsync(
            f => f.RequesterId == requesterId && f.AddresseeId == addresseeId &&
                 f.Status == FriendshipStatus.Pending,
            cancellationToken);
        if (friendship is null)
        {
            return;
        }

        dbContext.Friendships.Remove(friendship);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> AreFriendsAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken = default) =>
        dbContext.Friendships.AnyAsync(
            f => f.Status == FriendshipStatus.Accepted &&
                 ((f.RequesterId == userId && f.AddresseeId == otherUserId) ||
                  (f.RequesterId == otherUserId && f.AddresseeId == userId)),
            cancellationToken);

    public async Task<IReadOnlyList<(AppUser Friend, DateTimeOffset FriendsSinceUtc)>> ListFriendsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var friendships = await dbContext.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.RequesterId == userId || f.AddresseeId == userId))
            .ToListAsync(cancellationToken);

        var friendIds = friendships
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToList();
        var friendUsers = await dbContext.Users
            .Where(u => friendIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return friendships
            .Select(f =>
            {
                var friendId = f.RequesterId == userId ? f.AddresseeId : f.RequesterId;
                return (friendUsers[friendId], f.RespondedAtUtc ?? f.CreatedAtUtc);
            })
            .ToList();
    }

    public async Task<(IReadOnlyList<(AppUser OtherUser, DateTimeOffset CreatedAtUtc)> Incoming,
        IReadOnlyList<(AppUser OtherUser, DateTimeOffset CreatedAtUtc)> Outgoing)> ListPendingAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var pending = await dbContext.Friendships
            .Where(f => f.Status == FriendshipStatus.Pending && (f.RequesterId == userId || f.AddresseeId == userId))
            .ToListAsync(cancellationToken);

        var otherUserIds = pending
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToList();
        var otherUsers = await dbContext.Users
            .Where(u => otherUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var incoming = pending
            .Where(f => f.AddresseeId == userId)
            .Select(f => (otherUsers[f.RequesterId], f.CreatedAtUtc))
            .ToList();
        var outgoing = pending
            .Where(f => f.RequesterId == userId)
            .Select(f => (otherUsers[f.AddresseeId], f.CreatedAtUtc))
            .ToList();

        return (incoming, outgoing);
    }
}
