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
    public void SharedTabs_MirrorSidebarGeometryAndPagesDoNotDuplicateTabHeadings()
    {
        var root = GetFrontendRoot();
        var tabsCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tabs.css"));
        var layoutCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var adminCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-admin.css"));
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));
        var tokensCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var interactionsJs = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));
        var libraryPage = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Page_Library.razor"));
        var permissionPage = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Permission", "Page_Permission.razor"));

        Assert.Contains("--vpp-navigation-row-height: 40px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-tabs-bar-height: 44px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-item-hover-bg", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-item-active-bg", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-chrome-bg: var(--vpp-bg-surface);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-chrome-bg: var(--vpp-bg-base);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-navigation-chrome-bg);", tabsCss, StringComparison.Ordinal);
        Assert.DoesNotContain("background: var(--vpp-bg-elevated);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-item-hover-bg", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height) !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 14px !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("font-weight: 400 !important;", tabsCss, StringComparison.Ordinal);
        Assert.DoesNotContain("transition: none !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-tab-shared-indicator", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .vpp-tab-shared-indicator", tabsCss, StringComparison.Ordinal);
        Assert.Contains("content: none;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-layout-body-inset: var(--vpp-space-5);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-layout-body-inset: var(--vpp-space-4);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("width: calc(100% + (2 * var(--vpp-layout-body-inset)));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline: calc(-1 * var(--vpp-layout-body-inset));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-block-start: calc(-1 * var(--vpp-layout-body-inset));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-block-end: var(--vpp-layout-body-inset);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("top: calc(var(--vpp-tabs-sticky-top) - var(--vpp-layout-body-inset));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("display: flow-root;", adminCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-admin-tabs .rz-tabview-nav-container", adminCss, StringComparison.Ordinal);
        Assert.DoesNotContain("ul[role=\"tablist\"]", tabsCss, StringComparison.Ordinal);
        Assert.Contains("var tabIndicatorDuration = 180;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var tabListSelector = \".rz-tabview-nav\";", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("return tabList.closest(\".rz-tabview-nav-container\") || tabList;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var scrollOffset = host === tabList ? tabList.scrollLeft : 0;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var currentScrollOffset = elements.host === tabList ? tabList.scrollLeft : 0;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("if (!indicator.classList.contains(\"is-ready\"))", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("|| tabList.parentElement", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("ul[role='tablist']", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("getPropertyValue(\"--vpp-nav-indicator-inset\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var stretched = movingRight", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("cubic-bezier(0.32, 0.72, 0, 1)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("offset: 0.52", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("prefersReducedMotion()", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppPageHeader", libraryPage, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppPageHeader", permissionPage, StringComparison.Ordinal);
        Assert.DoesNotContain("LibraryPageDescription", libraryPage, StringComparison.Ordinal);
        Assert.DoesNotContain("PermissionPageDescription", permissionPage, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatedShell_KeepsBrandAndToggleInsideTheSidebar()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Layout", "LeftSidebar.razor"));
        var userMenuSource = File.ReadAllText(Path.Combine(root, "Components", "Layout", "UserMenu.razor"));
        var brandMarkSource = File.ReadAllText(Path.Combine(root, "Components", "Shared", "VppBrandMark.razor"));
        var appSource = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
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
        Assert.Contains("user-dropdown-profile", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("UserEmail", source, StringComparison.Ordinal);
        Assert.Contains("user-dropdown-email", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("user-dropdown-department", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("user-dropdown-action-divider", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("@Loc[\"Theme\"]", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("user-dropdown-department-name", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("user-dropdown-department-code", userMenuSource, StringComparison.Ordinal);
        Assert.Contains("ShowDepartmentCode", userMenuSource, StringComparison.Ordinal);
        Assert.DoesNotContain("VppIcons.Apartment", userMenuSource, StringComparison.Ordinal);
        Assert.DoesNotContain("user-dropdown-theme-button", userMenuSource, StringComparison.Ordinal);
        Assert.DoesNotContain("user-dropdown-context", userMenuSource, StringComparison.Ordinal);
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
        Assert.Equal(2, source.Split("<VppBrandMark", StringSplitOptions.None).Length - 1);
        Assert.Contains("vpp-sidebar-expanded-logo", source, StringComparison.Ordinal);
        Assert.Contains("images/vpp-app-icon.svg", brandMarkSource, StringComparison.Ordinal);
        Assert.Contains("images/vpp-app-icon.svg", appSource, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "wwwroot", "images", "vpp-app-icon.svg")));
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
        Assert.Contains("--vpp-sidebar-item-hover-bg: var(--vpp-navigation-item-hover-bg);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-font-sidebar: -apple-system", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-width: 286px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-width: 72px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 14px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("line-height: 20px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("equally legible in every interaction state", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("color: var(--vpp-text-primary) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("opacity: 1 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--vpp-navigation-row-height);", sidebarCss, StringComparison.Ordinal);
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
        Assert.Contains("--vpp-sidebar-row-half-gap: 2px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("4px rhythm optically centered", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar:not(.sidebar-collapsed) .rz-navigation-item.ppjsidebarmenu > .rz-navigation-item-wrapper", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("margin-block: var(--vpp-sidebar-row-half-gap) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain("margin-block-start: var(--vpp-space-1) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-icon-box-size: 24px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline-start: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-rail-content-gap: var(--vpp-space-3);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-child-rail-offset: calc(var(--vpp-space-1) + var(--vpp-space-4) + var(--vpp-sidebar-icon-box-size));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-child-content-offset: calc(var(--vpp-sidebar-child-rail-offset) + var(--vpp-sidebar-rail-content-gap));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-nested-indent-step: calc(var(--vpp-sidebar-icon-box-size) + var(--vpp-sidebar-rail-content-gap));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: var(--vpp-sidebar-child-content-offset);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("every child/grandchild icon exactly one shared rhythm", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-end: var(--vpp-space-4) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-active-indicator: transparent;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("suppress that partial", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("opacity: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("Keep the full row as the single state surface", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".submenu > .rz-navigation-item-wrapper-active:hover", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("inset-inline-start: var(--vpp-sidebar-child-rail-offset);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".rz-navigation-menu .rz-navigation-menu > .rz-navigation-item.ppjsidebarmenu > .rz-navigation-item-wrapper", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: calc(var(--vpp-sidebar-child-content-offset) + var(--vpp-sidebar-nested-indent-step));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("inset-inline-start: calc(var(--vpp-sidebar-child-rail-offset) + var(--vpp-sidebar-nested-indent-step));", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".ppjsidebarmenu.submenu", appCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-item-active-bg", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--vpp-sidebar-item-active-bg) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-nav-indicator-color);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar .rz-navigation-item-icon-children", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".rz-navigation-item-link:focus-visible", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".rz-navigation-item-wrapper:focus-within", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("pointer focus look like a hover surface", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("link transparent so hover/active color", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-color: transparent !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown", polishCss, StringComparison.Ordinal);
        Assert.Contains("box-sizing: border-box;", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: calc(var(--vpp-sidebar-width) - var(--vpp-space-2));", polishCss, StringComparison.Ordinal);
        Assert.Contains("padding: 2px var(--vpp-space-1) var(--vpp-space-1);", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height);", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar:not(.sidebar-collapsed) .vpp-sidebar-user-menu .user-menu-trigger[aria-expanded=\"true\"]", polishCss, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(var(--vpp-navigation-row-height) + var(--vpp-space-4));", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown-profile", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: 72px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: 72px;", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown-email", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown-department", polishCss, StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", polishCss, StringComparison.Ordinal);
        Assert.Contains("border-radius: 999px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-navigation-item-hover-bg);", polishCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".user-dropdown-department-code::before", polishCss, StringComparison.Ordinal);
        Assert.Contains("background: transparent;", polishCss, StringComparison.Ordinal);
        Assert.Contains("justify-content: center;", polishCss, StringComparison.Ordinal);
        Assert.Contains(".user-dropdown-action-divider", polishCss, StringComparison.Ordinal);
        Assert.Contains("color: var(--vpp-danger) !important;", polishCss, StringComparison.Ordinal);
        Assert.Contains("gap: var(--vpp-space-2);", polishCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-navigation-item-hover-bg);", polishCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-navigation-item-active-bg);", polishCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 20px;", polishCss, StringComparison.Ordinal);
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
