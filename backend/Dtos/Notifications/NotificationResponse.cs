using AnonyMeow.Domain;
using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Dtos.Notifications;

public record NotificationResponse(
    Guid Id,
    NotificationType Type,
    NotificationSourceType SourceType,
    Guid SourceId,
    bool IsRead,
    string PreviewText,
    DateTimeOffset CreatedAtUtc)
{
    public static NotificationResponse FromEntity(Notification notification) => new(
        notification.Id,
        notification.Type,
        notification.SourceType,
        notification.SourceId,
        notification.IsRead,
        notification.PreviewText,
        notification.CreatedAtUtc);
}
