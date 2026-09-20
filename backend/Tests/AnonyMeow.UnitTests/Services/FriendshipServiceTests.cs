using AnonyMeow.Common.Exceptions;
using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class FriendshipServiceTests
{
    private class FakeNotificationDispatcher : INotificationDispatcher
    {
        public List<(Guid RecipientId, NotificationType Type, NotificationSourceType SourceType, Guid SourceId)> Calls { get; } = [];

        public Task DispatchAsync(
            Guid recipientId, NotificationType type, NotificationSourceType sourceType, Guid sourceId, string previewText,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((recipientId, type, sourceType, sourceId));
            return Task.CompletedTask;
        }
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static FriendshipService CreateService(AppDbContext dbContext, FakeNotificationDispatcher? dispatcher = null) =>
        new(dbContext, dispatcher ?? new FakeNotificationDispatcher());

    private static async Task<(AppUser A, AppUser B)> SeedUsersAsync(AppDbContext dbContext)
    {
        var a = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "usera", CreatedAtUtc = DateTimeOffset.UtcNow };
        var b = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "userb", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.AddRange(a, b);
        await dbContext.SaveChangesAsync();
        return (a, b);
    }

    [Fact]
    public async Task RequestAsync_DispatchesFriendRequestNotification_ToAddressee()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var dispatcher = new FakeNotificationDispatcher();
        var service = CreateService(dbContext, dispatcher);

        await service.RequestAsync(a.Id, b.Id);

        var call = Assert.Single(dispatcher.Calls);
        Assert.Equal(b.Id, call.RecipientId);
        Assert.Equal(NotificationType.FriendRequest, call.Type);
        Assert.Equal(NotificationSourceType.User, call.SourceType);
        Assert.Equal(a.Id, call.SourceId);
    }

    [Fact]
    public async Task RequestAsync_Throws_OnSelfRequest()
    {
        var dbContext = CreateDbContext();
        var (a, _) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        await Assert.ThrowsAsync<SelfFriendRequestException>(() => service.RequestAsync(a.Id, a.Id));
    }

    [Fact]
    public async Task RequestAsync_Throws_WhenPendingRequestAlreadyExists()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        await service.RequestAsync(a.Id, b.Id);

        await Assert.ThrowsAsync<FriendshipAlreadyExistsException>(() => service.RequestAsync(a.Id, b.Id));
    }

    [Fact]
    public async Task RequestAsync_Throws_WhenReverseRequestAlreadyExists()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        await service.RequestAsync(a.Id, b.Id);

        await Assert.ThrowsAsync<FriendshipAlreadyExistsException>(() => service.RequestAsync(b.Id, a.Id));
    }

    [Fact]
    public async Task RequestAsync_AllowsRetry_AfterPriorRequestWasDeclined()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        var firstRequest = await service.RequestAsync(a.Id, b.Id);
        await service.DeclineAsync(firstRequest);

        var secondRequest = await service.RequestAsync(a.Id, b.Id);

        Assert.Equal(FriendshipStatus.Pending, secondRequest.Status);
    }

    [Fact]
    public async Task AcceptAsync_MarksFriendshipAccepted_AndListedForBothUsers()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        var request = await service.RequestAsync(a.Id, b.Id);

        await service.AcceptAsync(request);

        Assert.True(await service.AreFriendsAsync(a.Id, b.Id));
        var aFriends = await service.ListFriendsAsync(a.Id);
        Assert.Contains(aFriends, f => f.Friend.Id == b.Id);
        var bFriends = await service.ListFriendsAsync(b.Id);
        Assert.Contains(bFriends, f => f.Friend.Id == a.Id);
    }

    [Fact]
    public async Task GetPendingRequestAsync_ReturnsNull_WhenNoneExists()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        var result = await service.GetPendingRequestAsync(a.Id, b.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_DeletesAcceptedFriendship_RegardlessOfDirection()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        var request = await service.RequestAsync(a.Id, b.Id);
        await service.AcceptAsync(request);

        await service.RemoveAsync(b.Id, a.Id);

        Assert.False(await service.AreFriendsAsync(a.Id, b.Id));
        Assert.Equal(0, await dbContext.Friendships.CountAsync());
    }

    [Fact]
    public async Task RemoveAsync_NoExistingFriendship_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        await service.RemoveAsync(a.Id, b.Id);

        Assert.Equal(0, await dbContext.Friendships.CountAsync());
    }

    [Fact]
    public async Task CancelRequestAsync_DeletesPendingRequest_SentByRequester()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        await service.RequestAsync(a.Id, b.Id);

        await service.CancelRequestAsync(a.Id, b.Id);

        Assert.Equal(0, await dbContext.Friendships.CountAsync());
    }

    [Fact]
    public async Task CancelRequestAsync_NoExistingRequest_IsNoOp()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);

        await service.CancelRequestAsync(a.Id, b.Id);

        Assert.Equal(0, await dbContext.Friendships.CountAsync());
    }

    [Fact]
    public async Task CancelRequestAsync_DoesNotCancel_WhenCalledByAddressee()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        await service.RequestAsync(a.Id, b.Id);

        // b is the addressee, not the requester — cancelling as "b -> a" must not remove a's
        // real "a -> b" pending request (that's what accept/decline are for).
        await service.CancelRequestAsync(b.Id, a.Id);

        Assert.Equal(1, await dbContext.Friendships.CountAsync());
    }

    [Fact]
    public async Task CancelRequestAsync_DoesNotCancel_AcceptedFriendship()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        var request = await service.RequestAsync(a.Id, b.Id);
        await service.AcceptAsync(request);

        await service.CancelRequestAsync(a.Id, b.Id);

        Assert.True(await service.AreFriendsAsync(a.Id, b.Id));
    }

    [Fact]
    public async Task ListPendingAsync_SplitsRequestsByDirection()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var c = new AppUser { Id = Guid.NewGuid(), B2CObjectId = Guid.NewGuid().ToString(), Username = "userc", CreatedAtUtc = DateTimeOffset.UtcNow };
        dbContext.Users.Add(c);
        await dbContext.SaveChangesAsync();
        var service = CreateService(dbContext);

        // a -> b is outgoing for a, incoming for b.
        await service.RequestAsync(a.Id, b.Id);
        // c -> a is incoming for a, outgoing for c.
        await service.RequestAsync(c.Id, a.Id);

        var (incoming, outgoing) = await service.ListPendingAsync(a.Id);

        Assert.Single(incoming);
        Assert.Equal(c.Id, incoming[0].OtherUser.Id);
        Assert.Single(outgoing);
        Assert.Equal(b.Id, outgoing[0].OtherUser.Id);
    }

    [Fact]
    public async Task ListPendingAsync_ExcludesAcceptedAndDeclinedFriendships()
    {
        var dbContext = CreateDbContext();
        var (a, b) = await SeedUsersAsync(dbContext);
        var service = CreateService(dbContext);
        var request = await service.RequestAsync(a.Id, b.Id);
        await service.AcceptAsync(request);

        var (incoming, outgoing) = await service.ListPendingAsync(a.Id);

        Assert.Empty(incoming);
        Assert.Empty(outgoing);
    }
}
