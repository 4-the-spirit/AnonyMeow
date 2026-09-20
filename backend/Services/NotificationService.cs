using AnonyMeow.Data;
using AnonyMeow.Domain;
using Microsoft.EntityFrameworkCore;

namespace AnonyMeow.Services;

public class NotificationService(AppDbContext dbContext) : INotificationService
{
    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)> ListAsync(
        Guid userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Notifications.Where(n => n.RecipientId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications.FindAsync([id], cancellationToken);

    public async Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unread = await dbContext.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);
        foreach (var notification in unread)
        {
            notification.IsRead = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
