using AnonyMeow.Dtos.Messages;
using AnonyMeow.Dtos.Notifications;

namespace AnonyMeow.Hubs;

// Strongly-typed SignalR client contract for /hubs/notifications. DM delivery (Phase 5) reuses
// this same hub via ReceiveMessage rather than a second hub — SignalR knowledge stays confined to
// NotificationDispatcher and MessageService, both talking through this one typed client contract.
public interface INotificationClient
{
    Task ReceiveNotification(NotificationResponse notification);

    Task ReceiveMessage(MessageResponse message);
}
