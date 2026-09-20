using AnonyMeow.Domain.Enums;

namespace AnonyMeow.Services;

// Single seam for notification fan-out — persists a Notification row and pushes it over SignalR.
// Isolates the SignalR dependency to one implementation (DIP); callers (CommentService,
// MentionParsingService's caller, ModerationActionService, FriendshipService) never touch the hub.
public interface INotificationDispatcher
{
    Task DispatchAsync(
        Guid recipientId,
        NotificationType type,
        NotificationSourceType sourceType,
        Guid sourceId,
        string previewText,
        CancellationToken cancellationToken = default);
}
