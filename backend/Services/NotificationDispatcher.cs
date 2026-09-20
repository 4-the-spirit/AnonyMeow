using AnonyMeow.Data;
using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AnonyMeow.Services;

public class NotificationDispatcher(
    AppDbContext dbContext,
    IHubContext<NotificationHub, INotificationClient> hubContext) : INotificationDispatcher
{
    public async Task DispatchAsync(
        Guid recipientId,
        NotificationType type,
        NotificationSourceType sourceType,
        Guid sourceId,
        string previewText,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            Type = type,
            SourceType = sourceType,
            SourceId = sourceId,
            PreviewText = previewText,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);

        await hubContext.Clients
            .Group(NotificationHub.GroupName(recipientId))
            .ReceiveNotification(NotificationResponse.FromEntity(notification));
    }
}
