using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Services;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.UnitTests.Services;

public class NotificationServiceTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Notification MakeNotification(Guid recipientId, bool isRead = false) => new()
    {
        Id = Guid.NewGuid(),
        RecipientId = recipientId,
        Type = NotificationType.Reply,
        SourceType = NotificationSourceType.Comment,
        SourceId = Guid.NewGuid(),
        IsRead = isRead,
        PreviewText = "preview",
        CreatedAtUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task ListAsync_ReturnsOnlyCurrentUsersNotifications_NewestFirst()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var older = MakeNotification(userId);
        older.CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5);
        var newer = MakeNotification(userId);
        dbContext.Notifications.AddRange(older, newer, MakeNotification(otherUserId));
        await dbContext.SaveChangesAsync();
        var service = new NotificationService(dbContext);

        var (items, totalCount) = await service.ListAsync(userId, unreadOnly: false, page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(2, totalCount);
        Assert.Equal([newer.Id, older.Id], items.Select(n => n.Id));
    }

    [Fact]
    public async Task ListAsync_UnreadOnly_FiltersOutReadNotifications()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var unread = MakeNotification(userId);
        var read = MakeNotification(userId, isRead: true);
        dbContext.Notifications.AddRange(unread, read);
        await dbContext.SaveChangesAsync();
        var service = new NotificationService(dbContext);

        var (items, totalCount) = await service.ListAsync(userId, unreadOnly: true, page: 1, pageSize: 20, CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Equal(unread.Id, Assert.Single(items).Id);
    }

    [Fact]
    public async Task MarkAsReadAsync_SetsIsRead()
    {
        var dbContext = CreateDbContext();
        var notification = MakeNotification(Guid.NewGuid());
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync();
        var service = new NotificationService(dbContext);

        await service.MarkAsReadAsync(notification, CancellationToken.None);

        Assert.True((await dbContext.Notifications.SingleAsync()).IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_SetsIsReadForAllOfUsersUnreadNotifications_ButNotOthers()
    {
        var dbContext = CreateDbContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var mine = MakeNotification(userId);
        var otherUsers = MakeNotification(otherUserId);
        dbContext.Notifications.AddRange(mine, otherUsers);
        await dbContext.SaveChangesAsync();
        var service = new NotificationService(dbContext);

        await service.MarkAllAsReadAsync(userId, CancellationToken.None);

        Assert.True((await dbContext.Notifications.SingleAsync(n => n.Id == mine.Id)).IsRead);
        Assert.False((await dbContext.Notifications.SingleAsync(n => n.Id == otherUsers.Id)).IsRead);
    }
}
