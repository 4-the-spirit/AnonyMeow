using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Messages;
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Hubs;
using AnonyMeow.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class NotificationDispatcherTests
{
    // No mocking library is referenced in this test project (see the rest of Services/*Tests.cs),
    // so IHubContext<NotificationHub, INotificationClient> is faked by hand, same convention as
    // every other hand-rolled fake in this suite.
    private class FakeNotificationClient : INotificationClient
    {
        public List<NotificationResponse> Received { get; } = [];

        public Task ReceiveNotification(NotificationResponse notification)
        {
            Received.Add(notification);
            return Task.CompletedTask;
        }

        public Task ReceiveMessage(MessageResponse message) => Task.CompletedTask;
    }

    private class FakeHubClients : IHubClients<INotificationClient>
    {
        private readonly Dictionary<string, FakeNotificationClient> _groups = [];

        public IReadOnlyDictionary<string, FakeNotificationClient> JoinedGroups => _groups;

        public INotificationClient All => throw new NotSupportedException();
        public INotificationClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public INotificationClient Client(string connectionId) => throw new NotSupportedException();
        public INotificationClient Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();

        public INotificationClient Group(string groupName)
        {
            if (!_groups.TryGetValue(groupName, out var client))
            {
                client = new FakeNotificationClient();
                _groups[groupName] = client;
            }

            return client;
        }

        public INotificationClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public INotificationClient Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public INotificationClient OthersInGroup(string groupName) => throw new NotSupportedException();
        public INotificationClient User(string userId) => throw new NotSupportedException();
        public INotificationClient Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private class FakeHubContext : IHubContext<NotificationHub, INotificationClient>
    {
        public FakeHubClients FakeClients { get; } = new();
        public IHubClients<INotificationClient> Clients => FakeClients;
        public IGroupManager Groups => throw new NotSupportedException();
    }

    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task DispatchAsync_PersistsNotification_AndPushesToRecipientGroup()
    {
        var dbContext = CreateDbContext();
        var hubContext = new FakeHubContext();
        var dispatcher = new NotificationDispatcher(dbContext, hubContext);
        var recipientId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();

        await dispatcher.DispatchAsync(
            recipientId, NotificationType.Reply, NotificationSourceType.Comment, sourceId, "someone replied",
            CancellationToken.None);

        var persisted = await dbContext.Notifications.SingleAsync();
        Assert.Equal(recipientId, persisted.RecipientId);
        Assert.Equal(NotificationType.Reply, persisted.Type);
        Assert.Equal(NotificationSourceType.Comment, persisted.SourceType);
        Assert.Equal(sourceId, persisted.SourceId);
        Assert.Equal("someone replied", persisted.PreviewText);
        Assert.False(persisted.IsRead);

        var group = Assert.Single(hubContext.FakeClients.JoinedGroups);
        Assert.Equal(NotificationHub.GroupName(recipientId), group.Key);
        var pushed = Assert.Single(group.Value.Received);
        Assert.Equal(persisted.Id, pushed.Id);
        Assert.Equal("someone replied", pushed.PreviewText);
    }
}
