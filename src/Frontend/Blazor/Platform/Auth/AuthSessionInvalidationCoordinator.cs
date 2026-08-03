using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Platform.Auth;

public interface IAuthSessionInvalidationCoordinator
{
    Task InvalidateAsync(string reason);
}

/// <summary>
/// Kết thúc circuit Blazor hiện tại một lần sau khi backend từ chối bearer session.
/// Full navigation cho phép endpoint logout HTTP thu hồi session backend khi có thể
/// và xóa authentication cookie của frontend.
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
