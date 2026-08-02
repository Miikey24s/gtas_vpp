using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace gtas_vpp_fe.Features.Notifications.Realtime;

public sealed class NotificationRealtimeClient(
    IHttpClientFactory httpClientFactory,
    AuthenticationStateProvider authenticationStateProvider,
    ILogger<NotificationRealtimeClient> logger) : INotificationRealtimeClient
{
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private HubConnection? _connection;
    private IDisposable? _notificationSubscription;
    private int _disposeStarted;

    public event Func<Task>? NotificationsChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposeStarted != 0, this);
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null)
            {
                return;
            }

            var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
            var accessToken = authState.User.Claims.Get(ClaimKeys.AccessToken);
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return;
            }

            var apiClient = httpClientFactory.CreateClient(Config.HttpClientName);
            var hubUri = new Uri(apiClient.BaseAddress!, "hubs/notifications");
            var connection = new HubConnectionBuilder()
                .WithUrl(hubUri, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                })
                .WithAutomaticReconnect()
                .Build();
            var subscription = connection.On<long>(
                "NotificationsChanged",
                _ => RaiseNotificationsChangedAsync());

            try
            {
                await connection.StartAsync(cancellationToken);
                _connection = connection;
                _notificationSubscription = subscription;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                subscription.Dispose();
                await connection.DisposeAsync();
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Notification realtime connection is unavailable; manual refresh remains available.");
                subscription.Dispose();
                await connection.DisposeAsync();
            }
        }
        finally
        {
            _startLock.Release();
        }
    }

    private async Task RaiseNotificationsChangedAsync()
    {
        var handlers = NotificationsChanged?.GetInvocationList().Cast<Func<Task>>().ToArray() ?? [];
        foreach (var handler in handlers)
        {
            await handler();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        await _startLock.WaitAsync();
        try
        {
            _notificationSubscription?.Dispose();
            _notificationSubscription = null;
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _startLock.Release();
        }
    }
}
