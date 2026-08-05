using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using System.Security.Claims;

namespace gtas_vpp_fe.Features.IdentityAccess.State;

public sealed class PermissionState : IDisposable
{
    private sealed record RouteTarget(string PageCode, string? PermissionCode, string Path);

    // RouteCatalog là nguồn sự thật duy nhất cho deep link. Giữ thứ tự landing ưu tiên
    // tại đây giúp tránh danh sách thứ hai phải bảo trì thủ công và có thể lệch khỏi
    // sidebar hoặc browser audit catalog.
    private static readonly RouteTarget[] PreferredRoutes = BuildPreferredRoutes();

    private static RouteTarget[] BuildPreferredRoutes()
    {
        return RouteCatalog.Authenticated
            .Where(route => !route.IsDynamic)
            .SelectMany(route =>
            {
                var permissions = route.AnyOfPermissions.Length == 0
                    ? new string?[] { null }
                    : route.AnyOfPermissions.Select(permission => (string?)permission).ToArray();

                return permissions.Select(permission =>
                    new RouteTarget(route.PageCode, permission, route.Path));
            })
            .ToArray();
    }

    private readonly AuthHelper _authHelper;
    private readonly PermissionRefreshSignal _permissionRefreshSignal;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public PermissionState(AuthHelper authHelper, PermissionRefreshSignal permissionRefreshSignal)
    {
        _authHelper = authHelper;
        _permissionRefreshSignal = permissionRefreshSignal;
        permissionRefreshSignal.Requested += HandleRefreshRequestedAsync;
    }

    public IEnumerable<Claim> IdentityClaims { get; private set; } = Array.Empty<Claim>();

    public IReadOnlyDictionary<string, PagePermissionResDTO> PagePermissions { get; private set; } =
        new Dictionary<string, PagePermissionResDTO>(StringComparer.OrdinalIgnoreCase);

    public bool IsLoaded { get; private set; }

    public long Version { get; private set; }

    public IReadOnlySet<string> EffectivePermissions { get; private set; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public int CurrentUserId => IdentityClaims.GetInt(ClaimKeys.UserID);

    public Guid CurrentGroupId { get; private set; }

    public event Action? Changed;

    public Task EnsureLoadedAsync()
    {
        return IsLoaded ? Task.CompletedTask : RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        await _reloadLock.WaitAsync();
        try
        {
            await RefreshCoreAsync();
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private async Task HandleRefreshRequestedAsync()
    {
        // APIServices phát signal khi nhận 403. Nếu chính permission refresh đang giữ lock,
        // không chờ lồng nhau vì request ngoài sẽ tự hoàn tất hoặc ném lỗi với snapshot mới nhất.
        if (!await _reloadLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            await RefreshCoreAsync();
        }
        finally
        {
            _reloadLock.Release();
        }
    }

    private async Task RefreshCoreAsync()
    {
        var (isAuthenticated, claims) = await _authHelper.EnsureAuthenticatedAsync();
        if (!isAuthenticated)
        {
            SetState(
                Array.Empty<Claim>(),
                new Dictionary<string, PagePermissionResDTO>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Guid.Empty,
                0,
                false);
            return;
        }

        var currentClaims = claims.ToArray();
        var userId = currentClaims.GetInt(ClaimKeys.UserID);
        if (userId <= 0)
        {
            SetState(
                currentClaims,
                new Dictionary<string, PagePermissionResDTO>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Guid.Empty,
                0,
                false);
            return;
        }

        var snapshot = await _authHelper.GetMyPermissionsAsync();
        var pagePermissions = snapshot.Pages.ToDictionary(
            page => page.PageCode,
            page => new PagePermissionResDTO
            {
                PageCode = page.PageCode,
                Components = page.Components.Select(component =>
                    new childModel_Authentication_GetPermissionSinglePage_Component
                    {
                        ComponentCode = component.ComponentCode,
                        IsVisible = component.IsVisible,
                        IsEnable = component.IsEnable
                    }).ToList()
            },
            StringComparer.OrdinalIgnoreCase);

        SetState(
            currentClaims,
            pagePermissions,
            snapshot.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase),
            snapshot.GroupId,
            snapshot.Version,
            true);
    }

    public PagePermissionResDTO GetPagePermission(string pageCode)
    {
        if (!string.IsNullOrWhiteSpace(pageCode) && PagePermissions.TryGetValue(pageCode, out var permission))
        {
            return permission;
        }

        return new PagePermissionResDTO { PageCode = pageCode };
    }

    public IReadOnlyList<childModel_Authentication_GetPermissionSinglePage_Component> GetVisibleComponents(string pageCode)
    {
        return GetPagePermission(pageCode)
            .Components
            .Where(component => component.IsVisible
                && !Permissions.IsActionCode(component.ComponentCode))
            .ToArray();
    }

    public bool HasPageAccess(string pageCode)
    {
        return GetVisibleComponents(pageCode).Count > 0;
    }

    public bool HasMenuAccess(string menuCode)
    {
        return HasVisibleComponent(Config.Page_ComponentCode.PageCode.Sidebar, menuCode);
    }

    public bool HasPermission(string componentCode)
    {
        return EffectivePermissions.Contains(componentCode);
    }

    public bool HasEnabledPermission(string componentCode)
    {
        return EffectivePermissions.Contains(componentCode);
    }

    public bool HasVisibleComponent(string pageCode, string componentCode)
    {
        return GetPagePermission(pageCode)
            .Components
            .Any(component => string.Equals(component.ComponentCode, componentCode, StringComparison.OrdinalIgnoreCase) && component.IsVisible);
    }

    public bool HasEnabledComponent(string pageCode, string componentCode)
    {
        return GetPagePermission(pageCode)
            .Components
            .Any(component => string.Equals(component.ComponentCode, componentCode, StringComparison.OrdinalIgnoreCase) && component.IsVisible && component.IsEnable);
    }

    public string? GetFirstAccessibleRoute()
    {
        return GetFirstAccessibleRoute(PreferredRoutes);
    }

    public string? GetFirstAccessibleRouteForPage(string pageCode)
    {
        return GetFirstAccessibleRoute(PreferredRoutes.Where(route =>
            string.Equals(route.PageCode, pageCode, StringComparison.OrdinalIgnoreCase)));
    }

    private string? GetFirstAccessibleRoute(IEnumerable<RouteTarget> routes)
    {
        foreach (var route in routes)
        {
            if (string.IsNullOrWhiteSpace(route.PermissionCode))
            {
                if (HasPageAccess(route.PageCode))
                {
                    return route.Path;
                }

                continue;
            }

            var canUseRoute = Permissions.IsActionCode(route.PermissionCode)
                ? HasPermission(route.PermissionCode)
                : HasVisibleComponent(route.PageCode, route.PermissionCode);

            if (canUseRoute)
            {
                return route.Path;
            }
        }

        return null;
    }

    private void SetState(
        IEnumerable<Claim> claims,
        IReadOnlyDictionary<string, PagePermissionResDTO> pagePermissions,
        IReadOnlySet<string> effectivePermissions,
        Guid groupId,
        long version,
        bool isLoaded)
    {
        IdentityClaims = claims.ToArray();
        PagePermissions = pagePermissions;
        EffectivePermissions = effectivePermissions;
        CurrentGroupId = groupId;
        Version = version;
        IsLoaded = isLoaded;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _permissionRefreshSignal.Requested -= HandleRefreshRequestedAsync;
    }
}
