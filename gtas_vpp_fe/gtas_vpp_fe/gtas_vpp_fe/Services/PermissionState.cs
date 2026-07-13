using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using System.Security.Claims;

namespace gtas_vpp_fe.Services;

public sealed class PermissionState
{
    private sealed record RouteTarget(string PageCode, string? PermissionCode, string Path);

    private static readonly RouteTarget[] PreferredRoutes =
    [
        new(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestOrder, "/dashboard?tab=0"),
        new(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestHistory, "/dashboard?tab=1"),
        new(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestProductCatalog, "/dashboard?tab=2"),
        new(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestDepartmentSummary, "/dashboard?tab=3"),
        new(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestAllOrdersSummary, "/dashboard?tab=4"),
        new(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestAdminApproval, "/dashboard?tab=5"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibraryClass, "/library?tab=0"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibraryCategory, "/library?tab=1"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibraryItem, "/library?tab=2"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibrarySupplier, "/library?tab=3"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibraryPrice, "/library?tab=4"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibraryDepartment, "/library?tab=5"),
        new(Config.Page_ComponentCode.PageCode.Library, Permissions.LibraryPriceList, "/library?tab=6"),
        new(Config.Page_ComponentCode.PageCode.Permission, Permissions.PermissionUser, "/permission?tab=0"),
        new(Config.Page_ComponentCode.PageCode.Permission, Permissions.PermissionComponent, "/permission?tab=1"),
        new(Config.Page_ComponentCode.PageCode.Report, null, "/report")
    ];

    private readonly AuthHelper _authHelper;
    private readonly SemaphoreSlim _reloadLock = new(1, 1);

    public PermissionState(AuthHelper authHelper, PermissionRefreshSignal permissionRefreshSignal)
    {
        _authHelper = authHelper;
        permissionRefreshSignal.Requested += RefreshAsync;
    }

    public IEnumerable<Claim> IdentityClaims { get; private set; } = Array.Empty<Claim>();

    public IReadOnlyDictionary<string, sp_Authentication_GetPermissionSinglePage> PagePermissions { get; private set; } =
        new Dictionary<string, sp_Authentication_GetPermissionSinglePage>(StringComparer.OrdinalIgnoreCase);

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
            var (isAuthenticated, claims) = await _authHelper.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                SetState(
                    Array.Empty<Claim>(),
                    new Dictionary<string, sp_Authentication_GetPermissionSinglePage>(StringComparer.OrdinalIgnoreCase),
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
                    new Dictionary<string, sp_Authentication_GetPermissionSinglePage>(StringComparer.OrdinalIgnoreCase),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    Guid.Empty,
                    0,
                    false);
                return;
            }

            var snapshot = await _authHelper.GetMyPermissionsAsync();
            var pagePermissions = snapshot.Pages.ToDictionary(
                page => page.PageCode,
                page => new sp_Authentication_GetPermissionSinglePage
                {
                    PageCode = page.PageCode,
                    List_Component = page.Components.Select(component =>
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
        finally
        {
            _reloadLock.Release();
        }
    }

    public sp_Authentication_GetPermissionSinglePage GetPagePermission(string pageCode)
    {
        if (!string.IsNullOrWhiteSpace(pageCode) && PagePermissions.TryGetValue(pageCode, out var permission))
        {
            return permission;
        }

        return new sp_Authentication_GetPermissionSinglePage { PageCode = pageCode };
    }

    public IReadOnlyList<childModel_Authentication_GetPermissionSinglePage_Component> GetVisibleComponents(string pageCode)
    {
        return GetPagePermission(pageCode)
            .List_Component
            .Where(component => component.IsVisible)
            .ToArray();
    }

    public bool HasPageAccess(string pageCode)
    {
        return GetVisibleComponents(pageCode).Count > 0;
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
            .List_Component
            .Any(component => string.Equals(component.ComponentCode, componentCode, StringComparison.OrdinalIgnoreCase) && component.IsVisible);
    }

    public bool HasEnabledComponent(string pageCode, string componentCode)
    {
        return GetPagePermission(pageCode)
            .List_Component
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

    private IEnumerable<childModel_Authentication_GetPermissionSinglePage_Component> FindComponents(string componentCode)
    {
        if (string.IsNullOrWhiteSpace(componentCode))
        {
            return Enumerable.Empty<childModel_Authentication_GetPermissionSinglePage_Component>();
        }

        return PagePermissions.Values
            .SelectMany(page => page.List_Component)
            .Where(component => string.Equals(component.ComponentCode, componentCode, StringComparison.OrdinalIgnoreCase));
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

            if (HasVisibleComponent(route.PageCode, route.PermissionCode))
            {
                return route.Path;
            }
        }

        return null;
    }

    private void SetState(
        IEnumerable<Claim> claims,
        IReadOnlyDictionary<string, sp_Authentication_GetPermissionSinglePage> pagePermissions,
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
}
