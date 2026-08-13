namespace gtas_vpp_be.Notifications;

public interface INotificationRealtimeNotifier
{
    Task NotifyUserAsync(int userId, CancellationToken cancellationToken = default);
}
