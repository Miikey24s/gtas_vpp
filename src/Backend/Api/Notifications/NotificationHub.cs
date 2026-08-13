using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace gtas_vpp_be.Notifications;

[Authorize]
public sealed class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (int.TryParse(Context.User?.FindFirst("UserID")?.Value, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserChannel(userId));
        }

        await base.OnConnectedAsync();
    }

    internal static string UserChannel(int userId) => $"notifications-user:{userId}";
}

public sealed class NotificationRealtimeNotifier(IHubContext<NotificationHub> hubContext)
    : INotificationRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hubContext = hubContext;

    public Task NotifyUserAsync(int userId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(NotificationHub.UserChannel(userId))
            .SendAsync("NotificationsChanged", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), cancellationToken);
}
