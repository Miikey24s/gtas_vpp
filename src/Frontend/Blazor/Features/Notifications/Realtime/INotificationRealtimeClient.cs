namespace gtas_vpp_fe.Features.Notifications.Realtime;

public interface INotificationRealtimeClient : IAsyncDisposable
{
    event Func<Task>? NotificationsChanged;

    Task StartAsync(CancellationToken cancellationToken = default);
}
