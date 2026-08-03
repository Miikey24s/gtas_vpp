using gtas_vpp_fe.Features.Notifications.Api;
using gtas_vpp_fe.Features.Notifications.Realtime;
using gtas_vpp_shared.DTOs.Res.Notifications;

namespace gtas_vpp_fe.Features.Notifications.State;

public sealed class NotificationInboxState : IAsyncDisposable
{
    private readonly NotificationApiClient _api;
    private readonly INotificationRealtimeClient _realtime;
    private readonly ILogger<NotificationInboxState> _logger;
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private bool _started;
    private int _disposeStarted;

    public NotificationInboxState(
        NotificationApiClient api,
        INotificationRealtimeClient realtime,
        ILogger<NotificationInboxState> logger)
    {
        _api = api;
        _realtime = realtime;
        _logger = logger;
        _realtime.NotificationsChanged += OnRealtimeNotificationsChangedAsync;
    }

    public IReadOnlyList<NotificationResDTO> Items { get; private set; } = [];
    public int UnreadCount { get; private set; }
    public bool IsLoading { get; private set; }
    public bool HasError { get; private set; }
    public event Action? Changed;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _startLock.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            if (_started)
            {
                return;
            }

            await RefreshAsync(cancellationToken);
            await _realtime.StartAsync(cancellationToken);
            _started = true;
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            IsLoading = true;
            HasError = false;
            Changed?.Invoke();
            try
            {
                var inbox = await _api.GetInboxAsync(new NotificationInboxQuery());
                Items = inbox?.Items ?? [];
                UnreadCount = inbox?.UnreadCount ?? 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Giữ dữ liệu cũ để người dùng vẫn đọc được thông báo đã tải,
                // đồng thời component có thể hiển thị trạng thái retry.
                HasError = true;
                _logger.LogWarning(ex, "Could not load the notification inbox.");
            }
        }
        finally
        {
            IsLoading = false;
            _refreshLock.Release();
            if (_disposeStarted == 0)
            {
                Changed?.Invoke();
            }
        }
    }

    public async Task MarkReadAsync(NotificationResDTO item)
    {
        ThrowIfDisposed();
        if (item.IsRead)
        {
            return;
        }

        await _api.MarkReadAsync(item.Id);
        await RefreshAsync();
    }

    public async Task MarkAllReadAsync()
    {
        ThrowIfDisposed();
        if (UnreadCount == 0)
        {
            return;
        }

        await _api.MarkAllReadAsync();
        await RefreshAsync();
    }

    private async Task OnRealtimeNotificationsChangedAsync()
    {
        if (_disposeStarted != 0)
        {
            return;
        }

        try
        {
            await RefreshAsync();
        }
        catch (ObjectDisposedException) when (_disposeStarted != 0)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not refresh the notification inbox after a realtime update.");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposeStarted != 0, this);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        await _startLock.WaitAsync();
        try
        {
            _realtime.NotificationsChanged -= OnRealtimeNotificationsChangedAsync;
            await _realtime.DisposeAsync();
            Changed = null;
        }
        finally
        {
            _startLock.Release();
        }
    }
}
