using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace gtas_vpp_be.Authorization;

[Authorize]
public sealed class PermissionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var groupId = Context.User?.FindFirst("GroupId")?.Value;
        if (Guid.TryParse(groupId, out var parsedGroupId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupChannel(parsedGroupId));
        }

        var userId = Context.User?.FindFirst("UserID")?.Value;
        if (int.TryParse(userId, out var parsedUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserChannel(parsedUserId));
        }

        await base.OnConnectedAsync();
    }

    internal static string GroupChannel(Guid groupId) => $"permissions-group:{groupId:N}";

    internal static string UserChannel(int userId) => $"permissions-user:{userId}";
}

public interface IPermissionChangeNotifier
{
    Task NotifyGroupChangedAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task NotifyUserChangedAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class PermissionChangeNotifier(IHubContext<PermissionHub> hubContext)
    : IPermissionChangeNotifier
{
    private readonly IHubContext<PermissionHub> _hubContext = hubContext;

    public Task NotifyGroupChangedAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(PermissionHub.GroupChannel(groupId))
            .SendAsync("PermissionsChanged", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), cancellationToken);

    public Task NotifyUserChangedAsync(int userId, CancellationToken cancellationToken = default) =>
        _hubContext.Clients
            .Group(PermissionHub.UserChannel(userId))
            .SendAsync("PermissionsChanged", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), cancellationToken);
}
