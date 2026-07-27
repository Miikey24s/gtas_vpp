using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace gtas_vpp_fe.Components;

public partial class Routes
{
    [Inject] private AuthHelper AuthHelper { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private PermissionState PermissionState { get; set; } = default!;
    [Inject] private ILogger<Routes> Logger { get; set; } = default!;

    private bool _isRedirecting;

    private async Task OnNavigateAsync(NavigationContext context)
    {
        if (!RendererInfo.IsInteractive || _isRedirecting)
        {
            return;
        }

        try
        {
            await ApplyDynamicPermissionGuardAsync(context);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            // Điều hướng mới hơn đã thay thế lần kiểm tra quyền này.
        }
        catch (Exception exception)
        {
            // Mỗi page được bảo vệ có UI loading/error kèm guard riêng.
            // Không để tối ưu pre-navigation tùy chọn làm dừng circuit.
            Logger.LogError(
                exception,
                "Dynamic permission guard failed while navigating to {TargetPath}",
                context.Path);
        }
    }

    private async Task ApplyDynamicPermissionGuardAsync(NavigationContext context)
    {
        var targetPath = NormalizePath(context.Path);
        if (!RequiresDynamicPermissionGuard(targetPath) || IsAnonymousPath(targetPath))
        {
            return;
        }

        var (isAuthenticated, _) = await AuthHelper.EnsureAuthenticatedAsync();
        if (!isAuthenticated)
        {
            return;
        }

        await PermissionState.EnsureLoadedAsync();
        context.CancellationToken.ThrowIfCancellationRequested();

        var redirectPath = GetRedirectPath(targetPath);
        if (string.IsNullOrWhiteSpace(redirectPath) ||
            string.Equals(targetPath, NormalizePath(redirectPath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _isRedirecting = true;
        try
        {
            NavigationManager.NavigateTo(redirectPath, replace: true);
        }
        finally
        {
            _isRedirecting = false;
        }
    }

    private string? GetRedirectPath(string targetPath)
    {
        if (string.Equals(targetPath, "/dashboard/order-create", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestOrder)
                ? null
                : GetFallbackRoute(Config.Page_ComponentCode.PageCode.Dashboard);
        }

        if (targetPath.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Dashboard)
                ? null
                : GetFallbackRoute();
        }

        if (string.Equals(targetPath, "/library", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Library)
                ? null
                : GetFallbackRoute();
        }

        if (string.Equals(targetPath, "/permission", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Permission)
                ? null
                : GetFallbackRoute();
        }

        if (string.Equals(targetPath, "/report", StringComparison.OrdinalIgnoreCase))
        {
            return PermissionState.HasPageAccess(Config.Page_ComponentCode.PageCode.Report)
                ? null
                : GetFallbackRoute();
        }

        return null;
    }

    private string GetFallbackRoute(string? preferredPageCode = null)
    {
        return (!string.IsNullOrWhiteSpace(preferredPageCode)
                ? PermissionState.GetFirstAccessibleRouteForPage(preferredPageCode)
                : null)
            ?? PermissionState.GetFirstAccessibleRoute()
            ?? "/not-found";
    }

    private static bool RequiresDynamicPermissionGuard(string targetPath)
    {
        return targetPath.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, "/library", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, "/permission", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, "/report", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAnonymousPath(string targetPath)
    {
        return string.Equals(targetPath, NormalizePath(Config.LoginPagePath), StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, NormalizePath(Config.LoginProcessPath), StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, NormalizePath(Config.LogoutProcessPath), StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, "/not-found", StringComparison.OrdinalIgnoreCase)
            || string.Equals(targetPath, "/error", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Split('?', 2, StringSplitOptions.TrimEntries)[0];
        if (!normalized.StartsWith('/'))
        {
            normalized = "/" + normalized;
        }

        normalized = normalized.TrimEnd('/');
        return string.IsNullOrWhiteSpace(normalized) ? "/" : normalized;
    }
}
