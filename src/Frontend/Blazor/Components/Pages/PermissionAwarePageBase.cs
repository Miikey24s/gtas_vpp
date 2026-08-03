using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Platform.State;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages;

public sealed record PermissionPageOptions(
    string PageCode,
    string AccessDeniedDetail,
    string RedirectPath = "/dashboard?tab=0",
    string AccessDeniedSummary = "Access Denied",
    string ErrorSummary = "Error",
    string ErrorDetailPrefix = "Error when call api: ",
    bool NotifyOnAccessDenied = true);

public abstract class PermissionAwarePageBase : ComponentBase
{
    [Inject] protected AuthHelper PageAuthHelper { get; set; } = default!;
    [Inject] protected PermissionState PagePermissionState { get; set; } = default!;
    [Inject] protected NavigationManager PageNavigationManager { get; set; } = default!;
    [Inject] protected IToastService Toast { get; set; } = default!;
    [Inject] protected UiBusyState PageBusyState { get; set; } = default!;
    [Inject] protected Microsoft.Extensions.Localization.IStringLocalizer<App> PageLocalizer { get; set; } = default!;

    protected async Task<bool> LoadPageAccessAsync(
        PermissionPageOptions options,
        Func<Task<bool>>? beforePermissionLoad = null)
    {
        // Page được bảo vệ render shell nhẹ khi prerender. Phần gọi API có xác thực
        // chỉ chạy một lần sau khi Server circuit global hoạt động.
        if (!RendererInfo.IsInteractive)
        {
            return false;
        }

        var (isAuthenticated, userClaims) = await PageAuthHelper.EnsureAuthenticatedAsync();
        if (!isAuthenticated)
        {
            PageNavigationManager.NavigateTo(Config.LogoutProcessPath, true);
            return false;
        }

        ApplyClaims(userClaims);

        if (beforePermissionLoad is not null && !await beforePermissionLoad())
        {
            return false;
        }

        using var busy = PageBusyState.Begin();
        try
        {
            await PagePermissionState.EnsureLoadedAsync();
            SyncPermissionState(options.PageCode);

            if (!PagePermissionState.HasPageAccess(options.PageCode))
            {
                NotifyAccessDenied(options);
                PageNavigationManager.NavigateTo(options.RedirectPath, true);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = options.ErrorSummary,
                Detail = UiErrorMapper.GetMessage(ex, PageLocalizer),
                Duration = 10000
            });
            return false;
        }
    }

    protected bool HandlePermissionStateChanged(
        PermissionPageOptions options,
        Func<bool>? additionalGuard = null,
        Action? onStateChanged = null)
    {
        SyncPermissionState(options.PageCode);

        if ((additionalGuard is not null && !additionalGuard()) ||
            !PagePermissionState.HasPageAccess(options.PageCode))
        {
            PageNavigationManager.NavigateTo(options.RedirectPath, true);
            return false;
        }

        onStateChanged?.Invoke();
        _ = InvokeAsync(StateHasChanged);
        return true;
    }

    protected bool NotifyAndRedirect(string summary, string detail, string redirectPath)
    {
        Toast.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = summary,
            Detail = detail,
            Duration = 5000
        });
        PageNavigationManager.NavigateTo(redirectPath, true);
        return false;
    }

    protected void SyncPermissionState(string pageCode)
    {
        if (PagePermissionState.IdentityClaims.Any())
        {
            ApplyClaims(PagePermissionState.IdentityClaims);
        }

        ApplyPagePermission(PagePermissionState.GetPagePermission(pageCode));
    }

    protected virtual void ApplyClaims(IEnumerable<Claim> claims)
    {
    }

    protected virtual void ApplyPagePermission(PagePermissionResDTO permission)
    {
    }

    private void NotifyAccessDenied(PermissionPageOptions options)
    {
        if (!options.NotifyOnAccessDenied)
        {
            return;
        }

        Toast.Notify(new NotificationMessage
        {
            Severity = NotificationSeverity.Warning,
            Summary = options.AccessDeniedSummary,
            Detail = options.AccessDeniedDetail,
            Duration = 5000
        });
    }
}
