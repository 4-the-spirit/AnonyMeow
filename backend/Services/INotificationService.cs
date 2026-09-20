using AnonyMeow.Domain;

namespace AnonyMeow.Services;

public interface INotificationService
{
    Task<(IReadOnlyList<Notification> Items, int TotalCount)> ListAsync(
        Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
