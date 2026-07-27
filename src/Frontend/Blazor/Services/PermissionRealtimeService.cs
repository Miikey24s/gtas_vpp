using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.SignalR.Client;

namespace gtas_vpp_fe.Services;

public sealed class PermissionRealtimeService(
    IHttpClientFactory httpClientFactory,
    AuthenticationStateProvider authenticationStateProvider,
    PermissionState permissionState,
    ILogger<PermissionRealtimeService> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _startLock = new(1, 1);
    private HubConnection? _connection;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
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
            var hubUri = new Uri(apiClient.BaseAddress!, "hubs/permissions");

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUri, options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<long>("PermissionsChanged", async _ =>
            {
                try
                {
                    await permissionState.RefreshAsync();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Could not refresh permissions after a realtime update.");
                }
            });

            try
            {
                await _connection.StartAsync(cancellationToken);
            }
            catch
            {
                await _connection.DisposeAsync();
                _connection = null;
                throw;
            }
        }
        finally
        {
            _startLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _startLock.Dispose();
    }
}
