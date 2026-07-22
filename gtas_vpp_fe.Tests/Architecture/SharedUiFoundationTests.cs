using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class SharedUiFoundationTests
{
    [Fact]
    public void MaterialSymbolMarkup_IsCentralizedInVppIcon()
    {
        var root = GetFrontendRoot();
        var componentRoot = Path.Combine(root, "Components");
        var iconComponent = Path.Combine(componentRoot, "Shared", "VppIcon.razor");

        var offenders = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).Equals(Path.GetFullPath(iconComponent), StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("material-symbols-outlined", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.True(offenders.Length == 0, $"Use VppIcon instead of direct icon markup: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void ExceptionMessages_AreNotExposedThroughUserFacingComponents()
    {
        var root = GetFrontendRoot();
        var offenders = new List<string>();

        foreach (var path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                     .Where(path => Path.GetExtension(path) is ".cs" or ".razor")
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                         && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)))
        {
            var lines = File.ReadAllLines(path);
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                if (!line.Contains("ex.Message", StringComparison.Ordinal)
                    && !line.Contains("exception.Message", StringComparison.Ordinal))
                {
                    continue;
                }

                var isDiagnosticOnly = line.Contains("Console.", StringComparison.Ordinal)
                    || line.Contains("Logger.", StringComparison.Ordinal);

                if (!isDiagnosticOnly)
                {
                    offenders.Add($"{Path.GetRelativePath(root, path)}:{index + 1}");
                }
            }
        }

        Assert.True(offenders.Count == 0, $"Map exceptions with UiErrorMapper before displaying them: {string.Join(", ", offenders)}");
    }

    [Fact]
    public void NotificationCenter_UsesTheSharedIconSystem()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Layout", "NotificationCenter.razor"));

        Assert.DoesNotContain("class=\"rzi", source, StringComparison.Ordinal);
        Assert.Contains("<VppIcon", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MyOrders_UsesTheSharedIconSystem()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor"));

        Assert.DoesNotContain("class=\"rzi", source, StringComparison.Ordinal);
        Assert.Contains("<VppIcon", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardTabs_KeepTheRadzenAccessibilityBaseline()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Component_VPPRequest.razor"));

        Assert.Contains("<RadzenTabs", source, StringComparison.Ordinal);
        Assert.Contains("vpp-admin-tabs", source, StringComparison.Ordinal);
        Assert.Contains("vpp-secondary-tabs", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatedShell_KeepsBrandAndToggleInsideTheSidebar()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Layout", "LeftSidebar.razor"));
        var userMenuSource = File.ReadAllText(Path.Combine(root, "Components", "Layout", "UserMenu.razor"));
        var layoutCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var polishCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var responsiveCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-responsive.css"));

        var sidebarStart = source.IndexOf("<RadzenSidebar", StringComparison.Ordinal);
        var brand = source.IndexOf("vpp-sidebar-brand", StringComparison.Ordinal);
        var desktopToggle = source.IndexOf("vpp-sidebar-toggle", StringComparison.Ordinal);
        var userFooter = source.IndexOf("vpp-sidebar-user-footer", StringComparison.Ordinal);
        var userMenu = source.IndexOf("<UserMenu", StringComparison.Ordinal);

        Assert.True(sidebarStart >= 0 && brand > sidebarStart, "the GTAS VPP brand should live inside the sidebar");
        Assert.True(desktopToggle > sidebarStart, "the desktop expand/collapse control should live inside the sidebar");
        Assert.True(userFooter > sidebarStart && userMenu > userFooter, "the account trigger should live at the bottom of the sidebar");
        Assert.DoesNotContain("vpp-sidebar-utilities", source, StringComparison.Ordinal);
        Assert.Contains("user-dropdown-actions", userMenuSource, StringComparison.Ordinal);
        Assert.DoesNotContain("<HeaderControls", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("<NotificationCenter MenuMode=\"true\"", userMenuSource, StringComparison.Ordinal);
        Assert.Equal(1, source.Split("<UserMenu", StringSplitOptions.None).Length - 1);
        Assert.Contains("vpp-sidebar-collapsed-brand", source, StringComparison.Ordinal);
        Assert.Contains("vpp-sidebar-collapsed-logo", source, StringComparison.Ordinal);
        Assert.Contains("vpp-sidebar-collapsed-expand-icon", source, StringComparison.Ordinal);
        Assert.Contains("vpp-sidebar-expanded-brand", source, StringComparison.Ordinal);
        Assert.Contains("background: transparent;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".sidebar-collapsed .vpp-sidebar-user-footer", polishCss, StringComparison.Ordinal);
        Assert.Contains("border-top: 0;", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: var(--vpp-sidebar-collapsed-control-width);", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-sidebar-collapsed-control-height);", polishCss, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 var(--vpp-sidebar-collapsed-control-width);", layoutCss, StringComparison.Ordinal);
        Assert.Equal(1, source.Split("<VppBrandMark", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("href=\"/dashboard?tab=0\"", source, StringComparison.Ordinal);
        Assert.Contains("@if (_sideBarExpanded)", source, StringComparison.Ordinal);
        Assert.Contains("ShowArrow=\"true\"", source, StringComparison.Ordinal);
        Assert.Contains("ShowName=\"true\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<RadzenPanelMenuItem Text=\"@Loc[\"Logout\"]\"", source, StringComparison.Ordinal);
        Assert.Contains("vpp-mobile-sidebar-toggle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VppIcons.Search", source, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout", layoutCss, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: 0 1fr;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("display: none !important;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("min-height: 0 !important;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("\"rz-sidebar rz-header\"", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar", responsiveCss, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarNavigation_UsesAppleMusicInspiredRowStates()
    {
        var root = GetFrontendRoot();
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));
        var polishCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var tokensCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var appCss = File.ReadAllText(Path.Combine(root, "wwwroot", "app.css"));

        Assert.Contains("Apple Music-inspired hover", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-item-hover-bg: rgba(9, 30, 66, 0.075);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-font-sidebar: -apple-system", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-width: 286px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-width: 72px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 14px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("line-height: 20px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: 40px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("min-height: 40px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-block: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 20px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-control-width: calc(var(--vpp-sidebar-collapsed-width) - var(--vpp-space-1));", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-control-height: 48px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("width: var(--vpp-sidebar-collapsed-control-width) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-sidebar-collapsed-control-height) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("Collapsed active state stays line-only", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("Parent-child surface edge parity", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline: var(--vpp-space-1);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-margin-inline: var(--vpp-space-1);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-padding-block: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-padding-block: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-2nd-level-vertical-offset: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: var(--vpp-space-8);", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".ppjsidebarmenu.submenu", appCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-item-active-bg", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--vpp-sidebar-item-active-bg) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-nav-indicator-color);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar .rz-navigation-item-icon-children", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".rz-navigation-item-link:focus-visible", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".rz-navigation-item-wrapper:focus-within", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("pointer focus look like a hover surface", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: calc(var(--vpp-sidebar-width) - 16px);", polishCss, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(52px + var(--vpp-space-2));", polishCss, StringComparison.Ordinal);
        Assert.DoesNotContain("transform: translateX(2px);", sidebarCss, StringComparison.Ordinal);
    }

    [Fact]
    public void MyOrders_UsesTheRoundSixCommandCenterContract()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor"));
        var codeBehind = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor.cs"));

        Assert.Contains("vpp-orders-evidence", source, StringComparison.Ordinal);
        Assert.Contains("vpp-orders-story-commands", source, StringComparison.Ordinal);
        Assert.Contains("vpp-orders-deadline-track", source, StringComparison.Ordinal);
        Assert.Contains("CurrentOrderDetails", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-native-tab-list", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedDesignTokens_KeepThePreAngularRadiusSystem()
    {
        var root = GetFrontendRoot();
        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));

        Assert.Contains("--vpp-radius-sm: 4px;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-radius-md: 6px;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-radius-lg: 8px;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-radius-badge: var(--vpp-radius-full);", tokens, StringComparison.Ordinal);
    }

    private static string GetFrontendRoot()
    {
        var repositoryRoot = FindRepositoryRoot();
        return Path.Combine(repositoryRoot, "gtas_vpp_fe", "gtas_vpp_fe", "gtas_vpp_fe");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
