using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Notifications;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace gtas_vpp_fe.Services;

public sealed class NotificationInboxState(
    IAPIServices api,
    IHttpClientFactory httpClientFactory,
    AuthenticationStateProvider authenticationStateProvider,
    ILogger<NotificationInboxState> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private HubConnection? _connection;
    private bool _started;

    public IReadOnlyList<NotificationResDTO> Items { get; private set; } = [];
    public int UnreadCount { get; private set; }
    public bool IsLoading { get; private set; }
    public bool HasError { get; private set; }
    public event Action? Changed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            await RefreshAsync(cancellationToken);
            await ConnectRealtimeAsync(cancellationToken);
            _started = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            IsLoading = true;
            HasError = false;
            Changed?.Invoke();
            try
            {
                var inbox = await api.GetFromApiAsync<NotificationInboxResDTO>(
                    "api/notifications?skip=0&take=20");
                Items = inbox?.Items ?? [];
                UnreadCount = inbox?.UnreadCount ?? 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Keep already loaded items visible and let the component render
                // a retry state instead of surfacing an unhandled circuit error.
                HasError = true;
                logger.LogWarning(ex, "Could not load the notification inbox.");
            }
        }
        finally
        {
            IsLoading = false;
            _refreshLock.Release();
            Changed?.Invoke();
        }
    }

    public async Task MarkReadAsync(NotificationResDTO item)
    {
        if (!item.IsRead)
        {
            await api.PostFromApiAsync<object>($"api/notifications/{item.Id}/read", null);
            await RefreshAsync();
        }
    }

    public async Task MarkAllReadAsync()
    {
        if (UnreadCount == 0)
        {
            return;
        }

        await api.PostFromApiAsync<object>("api/notifications/read-all", null);
        await RefreshAsync();
    }

    private async Task ConnectRealtimeAsync(CancellationToken cancellationToken)
    {
        var authState = await authenticationStateProvider.GetAuthenticationStateAsync();
        var accessToken = authState.User.Claims.Get(ClaimKeys.AccessToken);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return;
        }

        var apiClient = httpClientFactory.CreateClient(Config.HttpClientName);
        var hubUri = new Uri(apiClient.BaseAddress!, "hubs/notifications");
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUri, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<long>("NotificationsChanged", async _ =>
        {
            try
            {
                await RefreshAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not refresh the notification inbox after a realtime update.");
            }
        });

        try
        {
            await _connection.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Notification realtime connection is unavailable; manual refresh remains available.");
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _startLock.Dispose();
        _refreshLock.Dispose();
    }
}
