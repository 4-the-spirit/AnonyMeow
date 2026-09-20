using AnonyMeow.Dtos.Common;
using AnonyMeow.Dtos.Notifications;
using AnonyMeow.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AnonyMeow.Endpoints;

public static class NotificationEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications");

        group.MapGet("", ListNotificationsAsync).WithName("ListNotifications");
        group.MapPost("/{id:guid}/read", MarkReadAsync).WithName("MarkNotificationRead");
        group.MapPost("/read-all", MarkAllReadAsync).WithName("MarkAllNotificationsRead");

        return app;
    }

    private static async Task<Ok<PagedResponse<NotificationResponse>>> ListNotificationsAsync(
        INotificationService notificationService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken,
        bool unreadOnly = false,
        int page = 1)
    {
        page = Math.Max(page, 1);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        var (items, totalCount) = await notificationService.ListAsync(
            currentUser.Id, unreadOnly, page, DefaultPageSize, cancellationToken);

        var responses = items.Select(NotificationResponse.FromEntity).ToList();
        return TypedResults.Ok(new PagedResponse<NotificationResponse>(responses, page, DefaultPageSize, totalCount));
    }

    // Returns 404 (not 403) when the notification belongs to another user — a private
    // per-user record shouldn't have its existence confirmed to a non-owner.
    private static async Task<Results<Ok, NotFound>> MarkReadAsync(
        Guid id,
        INotificationService notificationService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var notification = await notificationService.GetByIdAsync(id, cancellationToken);
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        if (notification is null || notification.RecipientId != currentUser.Id)
        {
            return TypedResults.NotFound();
        }

        await notificationService.MarkAsReadAsync(notification, cancellationToken);
        return TypedResults.Ok();
    }

    private static async Task<Ok> MarkAllReadAsync(
        INotificationService notificationService,
        ICurrentUserAccessor currentUserAccessor,
        CancellationToken cancellationToken)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync(cancellationToken);
        await notificationService.MarkAllAsReadAsync(currentUser.Id, cancellationToken);
        return TypedResults.Ok();
    }
}
