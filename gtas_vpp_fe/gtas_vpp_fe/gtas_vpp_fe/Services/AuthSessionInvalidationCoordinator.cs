using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Services;

public interface IAuthSessionInvalidationCoordinator
{
    Task InvalidateAsync(string reason);
}

/// <summary>
/// Ends the current Blazor circuit once after the backend rejects the bearer
/// session. A full navigation lets the HTTP logout endpoint revoke the backend
/// session when possible and delete the frontend authentication cookie.
/// </summary>
public sealed class AuthSessionInvalidationCoordinator(
    NavigationManager navigationManager,
    ILogger<AuthSessionInvalidationCoordinator> logger)
    : IAuthSessionInvalidationCoordinator
{
    private int _started;

    public Task InvalidateAsync(string reason)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return Task.CompletedTask;
        }

        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "session-invalid"
            : reason.Trim();
        logger.LogInformation(
            "Ending frontend session after backend authentication rejection: {Reason}.",
            normalizedReason);
        navigationManager.NavigateTo(
            $"/logoutprocess?reason={Uri.EscapeDataString(normalizedReason)}",
            forceLoad: true);
        return Task.CompletedTask;
    }
}
