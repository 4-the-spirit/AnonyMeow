using AnonyMeow.Services;
using Microsoft.AspNetCore.SignalR;

namespace AnonyMeow.Hubs;

public class NotificationHub(ICurrentUserAccessor currentUserAccessor) : Hub<INotificationClient>
{
    public static string GroupName(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var user = await currentUserAccessor.GetCurrentUserAsync(Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(user.Id), Context.ConnectionAborted);
        await base.OnConnectedAsync();
    }
}
