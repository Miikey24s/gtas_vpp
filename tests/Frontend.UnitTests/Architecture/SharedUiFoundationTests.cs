using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class SharedUiFoundationTests
{
    [Fact]
    public void AuthoredStyles_DoNotForceUppercaseUiText()
    {
        var root = GetFrontendRoot();
        var authoredStyles = Directory.EnumerateFiles(Path.Combine(root, "wwwroot", "css"), "*.css", SearchOption.AllDirectories)
            .Append(Path.Combine(root, "wwwroot", "app.css"));

        var offenders = authoredStyles
            .Where(path => File.ReadAllText(path).Contains("text-transform: uppercase", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        Assert.True(
            offenders.Length == 0,
            $"Preserve localized casing instead of forcing uppercase in authored styles: {string.Join(", ", offenders)}");

        var app = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        var casingCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-casing.css"));
        var radzenThemeIndex = app.IndexOf("material-base.css", StringComparison.Ordinal);
        var casingGuardIndex = app.IndexOf("css/vpp-casing.css", StringComparison.Ordinal);

        Assert.True(radzenThemeIndex >= 0 && casingGuardIndex > radzenThemeIndex, "Load the casing guard after the Radzen Material theme.");
        Assert.Contains("body *", casingCss, StringComparison.Ordinal);
        Assert.Contains("text-transform: none !important;", casingCss, StringComparison.Ordinal);
        Assert.Contains("letter-spacing: normal !important;", casingCss, StringComparison.Ordinal);
    }

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

        // D2/D8: dashboard không còn nested Management tabs (vpp-secondary-tabs);
        // baseline secondary tabs giờ được khóa qua Component_Library.
        var librarySource = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Component_Library.razor"));
        Assert.Contains("vpp-secondary-tabs", librarySource, StringComparison.Ordinal);
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
        Assert.Contains("--vpp-library-primary-tabs-height: var(--vpp-header-height);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-primary-tab-inline-padding: 20px;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-library-primary-tabs-height) !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("max-height: var(--vpp-library-primary-tabs-height) !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("align-items: center;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-primary-tab-row-height: var(--vpp-header-height);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("padding-block: 0 !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("padding: 0 var(--vpp-primary-tab-inline-padding) !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("line-height: 20px;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("translate: none;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("@supports (text-box: trim-both cap alphabetic)", tabsCss, StringComparison.Ordinal);
        Assert.Contains("text-box: trim-both cap alphabetic;", tabsCss, StringComparison.Ordinal);
        Assert.DoesNotContain("alignOpticalTextElement", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("actualBoundingBox", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("vppInk", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("createElement(\"canvas\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function containsInteractionHost(node)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("if (!containsInteractionHost(node))", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("characterData: true", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var title = target.querySelector(\".rz-tabview-title\");", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("--vpp-primary-tab-indicator-preferred-width: 60px;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("function readPrimaryTabIndicatorWidth(tabList)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("Math.min(preferredWidth, shortestLabelWidth)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("Math.min(commonIndicatorWidth, titleRect.width)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("background: transparent !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .rz-tabview-nav > li.rz-tabview-selected", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin: 0 !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-item-hover-bg", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-item-active-bg", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-chrome-bg: var(--vpp-surface-panel);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-chrome-bg: var(--vpp-surface-raised);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-navigation-chrome-bg);", tabsCss, StringComparison.Ordinal);
        Assert.DoesNotContain("background: var(--vpp-bg-elevated);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-item-hover-bg", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-background-color: transparent;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-background-color: transparent;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-3rd-level-background-color: transparent;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-3rd-level-active-background-color", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height) !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 14px !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("font-weight: 400 !important;", tabsCss, StringComparison.Ordinal);
        Assert.DoesNotContain("transition: none !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-tab-shared-indicator", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .vpp-tab-shared-indicator", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .rz-tabview-nav:focus-visible", tabsCss, StringComparison.Ordinal);
        Assert.Contains("outline: none !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview-selected > .rz-tabview-nav-link", tabsCss, StringComparison.Ordinal);
        Assert.Contains("button[role=\"tab\"]", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview-title", tabsCss, StringComparison.Ordinal);
        Assert.Contains("box-shadow: inset 0 -1px 0 var(--vpp-border-default);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("content: none;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-layout-body-inset: var(--rz-layout-body-padding-1, var(--vpp-space-2));", layoutCss, StringComparison.Ordinal);
        Assert.DoesNotContain("--vpp-layout-body-inset: var(--vpp-space-5);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("gap: 0 !important;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("width: calc(100% + (2 * var(--vpp-layout-body-inset)));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline: calc(-1 * var(--vpp-layout-body-inset));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-block-start: calc(-1 * var(--vpp-layout-body-inset));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-block-end: var(--vpp-layout-body-inset);", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".librariestab > .rz-tabview-panels", tabsCss, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("background: transparent !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("top: calc(var(--vpp-tabs-sticky-top) - var(--vpp-layout-body-inset));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("display: flow-root;", adminCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-admin-tabs .rz-tabview-nav-container", adminCss, StringComparison.Ordinal);
        Assert.DoesNotContain("ul[role=\"tablist\"]", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-motion-duration: 200ms;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-motion-easing: cubic-bezier(0.32, 0.72, 0, 1);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("var tabIndicatorDuration = navigationMotionDuration;", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("function normalizePrimaryTabChrome(tabList, host)", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("host.style.setProperty(\"height\", headerHeight, \"important\")", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("tabList.style.setProperty(\"padding-block\", verticalInset, \"important\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var tabListSelector = \".rz-tabview-nav\";", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("return tabList.closest(\".rz-tabview-nav-container\") || tabList;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var scrollOffset = host === tabList ? tabList.scrollLeft : 0;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var currentScrollOffset = elements.host === tabList ? tabList.scrollLeft : 0;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("if (!indicator.classList.contains(\"is-ready\"))", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("|| tabList.parentElement", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("ul[role='tablist']", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("getPropertyValue(\"--vpp-nav-indicator-inset\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("window.getComputedStyle(target)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("parseFloat(targetStyles.paddingLeft)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("targetRect.width - startInset - endInset", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("var stretched = movingRight", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("moveTabIndicator(tabList, target, true, true)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("duration: tabIndicatorDuration", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("easing: navigationMotionEasing", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("offset: 0.52", interactionsJs, StringComparison.Ordinal);
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
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));
        var polishCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var responsiveCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-responsive.css"));
        var appCss = File.ReadAllText(Path.Combine(root, "wwwroot", "app.css"));
        var a11yCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-a11y.css"));

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
        Assert.Contains("vpp-sidebar-expanded-chrome", source, StringComparison.Ordinal);
        Assert.Contains("MenuItemDisplayStyle.IconAndText : MenuItemDisplayStyle.Icon", source, StringComparison.Ordinal);
        Assert.Contains("background: transparent;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".sidebar-collapsed .vpp-sidebar-user-footer", polishCss, StringComparison.Ordinal);
        Assert.Contains("border-top: 0;", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: var(--vpp-sidebar-collapsed-control-width);", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-sidebar-collapsed-control-height);", polishCss, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 var(--vpp-sidebar-collapsed-control-width);", layoutCss, StringComparison.Ordinal);
        Assert.Equal(2, source.Split("<VppBrandMark", StringSplitOptions.None).Length - 1);
        Assert.Contains("vpp-sidebar-expanded-logo", source, StringComparison.Ordinal);
        Assert.Contains("width: 24px;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("flex-basis: 24px;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("width: 24px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("flex: 0 0 24px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: 24px;", appCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 10px;", appCss, StringComparison.Ordinal);
        Assert.Contains(".user-menu-trigger", a11yCss, StringComparison.Ordinal);
        Assert.DoesNotContain("    .user-avatar {", a11yCss, StringComparison.Ordinal);
        Assert.Contains("images/vpp-app-icon.svg", brandMarkSource, StringComparison.Ordinal);
        Assert.Contains("images/vpp-app-icon.svg", appSource, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "wwwroot", "images", "vpp-app-icon.svg")));
        Assert.DoesNotContain("href=\"/dashboard?tab=0\"", source, StringComparison.Ordinal);
        Assert.Contains("@if (_sideBarExpanded)", source, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"@(!_sideBarExpanded)\"", source, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-transition", layoutCss, StringComparison.Ordinal);
        Assert.Contains("var(--vpp-sidebar-transition)", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".sidebar-collapsed .rz-navigation-item-icon-children", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("ShowArrow=\"true\"", source, StringComparison.Ordinal);
        Assert.Contains("ShowName=\"true\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<RadzenPanelMenuItem Text=\"@Loc[\"Logout\"]\"", source, StringComparison.Ordinal);
        Assert.Contains("vpp-mobile-sidebar-toggle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VppIcons.Search", source, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout", layoutCss, StringComparison.Ordinal);
        // W-B.2b: MỘT primary header 72px theo Atlas — desktop chứa tab strip
        // khu vực + role badge; nav RadzenTabs cấp cao nhất trong body chỉ còn
        // phục vụ mobile; breadcrumb vị trí chỉ hiện ở mobile.
        Assert.DoesNotContain("grid-template-rows: 0 1fr;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: var(--vpp-header-height) 1fr;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-admin-tabs > .rz-tabview-nav-container,\n    .vpp-admin-tabs > .rz-tabview-nav {\n        display: none !important;\n    }", layoutCss.Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Contains(".vpp-header-tabs", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-header-breadcrumb", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-header-role-badge", layoutCss, StringComparison.Ordinal);
        Assert.Contains("vpp-header-tabs", source, StringComparison.Ordinal);
        Assert.Contains("vpp-header-breadcrumb", source, StringComparison.Ordinal);
        Assert.Contains("vpp-header-role-badge", source, StringComparison.Ordinal);
        Assert.Contains("\"rz-sidebar rz-header\"", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar", responsiveCss, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarNavigation_UsesAppleMusicInspiredRowStates()
    {
        var root = GetFrontendRoot();
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));
        var layoutCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var polishCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var tokensCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var appCss = File.ReadAllText(Path.Combine(root, "wwwroot", "app.css"));
        var interactionsJs = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));

        Assert.Contains("--vpp-sidebar-item-hover-bg: var(--vpp-navigation-item-hover-bg);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-font-sidebar: -apple-system", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-width: 286px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-width: 72px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-header-height: var(--vpp-sidebar-collapsed-width);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 14px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("line-height: 20px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("color: var(--vpp-text-primary) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("opacity: 1 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--vpp-navigation-row-height);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-block: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 20px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-control-width: calc(var(--vpp-sidebar-collapsed-width) - var(--vpp-space-1));", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-collapsed-control-height: 48px;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("width: var(--vpp-sidebar-collapsed-control-width) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("inset-block-start: calc((var(--vpp-header-height) - var(--vpp-sidebar-collapsed-control-height)) / 2);", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-product", layoutCss, StringComparison.Ordinal);
        Assert.Contains("translate: none;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline: var(--vpp-space-1);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-margin-inline: var(--vpp-space-1);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-padding-block: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-padding-block: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-2nd-level-vertical-offset: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-row-half-gap: 2px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-header-to-nav-overlap: 0px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("border-block-end: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar:not(.sidebar-collapsed) .rz-navigation-item.ppjsidebarmenu > .rz-navigation-item-wrapper", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("margin-block: var(--vpp-sidebar-row-half-gap) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain("margin-block-start: var(--vpp-space-1) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-icon-box-size: 24px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-primary-content-offset: 8px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".sidebar-collapsed .rz-panel-menu", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("width: 100% !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding: 0 0 var(--vpp-space-3);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline-start: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-rail-content-gap: 12px;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-child-rail-offset: calc(var(--vpp-sidebar-primary-content-offset) + var(--vpp-sidebar-icon-box-size));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-child-content-offset: calc(var(--vpp-sidebar-child-rail-offset) + var(--vpp-sidebar-rail-content-gap) - var(--vpp-space-1));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-nested-indent-step: calc(var(--vpp-sidebar-icon-box-size) + var(--vpp-sidebar-rail-content-gap));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar:not(.sidebar-collapsed) .rz-panel-menu > .rz-navigation-item.ppjsidebarmenu > .rz-navigation-item-wrapper", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: calc(var(--vpp-sidebar-primary-content-offset) - var(--vpp-space-1));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar:not(.sidebar-collapsed) .vpp-sidebar-header", File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css")), StringComparison.Ordinal);
        Assert.Contains("var(--vpp-sidebar-primary-content-offset, 8px)", File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css")), StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: calc(var(--vpp-sidebar-primary-content-offset) - var(--vpp-space-1));", polishCss, StringComparison.Ordinal);
        Assert.Contains(".sidebar-collapsed .vpp-sidebar-user-menu", polishCss, StringComparison.Ordinal);
        Assert.Contains("justify-content: center;", polishCss, StringComparison.Ordinal);
        Assert.Contains("gap: 0;", polishCss, StringComparison.Ordinal);
        Assert.Contains("inset-inline-start: calc((100% - var(--vpp-sidebar-collapsed-control-width)) / 2);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: var(--vpp-sidebar-child-content-offset);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("gap: var(--vpp-sidebar-rail-content-gap);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline-end: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-end: var(--vpp-space-4) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-active-indicator: transparent;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("opacity: 0 !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background: radial-gradient(circle, currentColor 1%, transparent 1%) center / 15000%;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-size: 0%;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("transition: background-size 320ms ease-out, opacity 360ms ease-out;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("animation: vpp-sidebar-press-expand 360ms ease-out forwards;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("var pressSurfaceSelector = [", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function playPressSurface(target)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("document.addEventListener(\"pointerdown\"", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("surface.classList.add(\"vpp-pressing\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-brand", interactionsJs, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-menu-trigger", interactionsJs, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .rz-tabview-nav-link", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("button[role='tab']", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("position: relative;", polishCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .rz-tabview-nav-link.vpp-pressing::before", polishCss, StringComparison.Ordinal);
        Assert.Contains("button[role=\"tab\"].vpp-pressing::before", polishCss, StringComparison.Ordinal);
        Assert.Contains(".submenu > .rz-navigation-item-wrapper-active:hover", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".rz-navigation-menu .rz-navigation-menu > .rz-navigation-item.ppjsidebarmenu > .rz-navigation-item-wrapper", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("padding-inline-start: calc(var(--vpp-sidebar-child-content-offset) + var(--vpp-sidebar-nested-indent-step));", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-shared-indicator", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-active-indicator-in", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("function readCssTimeMilliseconds(value, fallback)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("getPropertyValue(\"--vpp-navigation-motion-duration\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("getPropertyValue(\"--vpp-navigation-motion-easing\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var sidebarIndicatorDuration = navigationMotionDuration;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var sidebarLayoutFollowDuration = navigationMotionDuration + 40;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("transition: grid-template-rows var(--vpp-navigation-motion-duration)", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("transition: visibility var(--vpp-navigation-motion-duration)", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("function resolveCssPixelLength(element, value, fallback)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("trimmed.endsWith(\"rem\")", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("element.appendChild(probe)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function moveSidebarIndicator(nav, target, shouldAnimate, forceTarget)", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("var stretched = movingDown", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function hideSidebarIndicator(indicator)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function hasVisibleAreaWithin(element, boundary)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("ancestor.classList.contains(\"rz-navigation-menu\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function scheduleSidebarIndicatorSync(nav, shouldAnimate)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("function followSidebarIndicatorLayout(nav)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("nav.vppSidebarLayoutFollowUntil = performance.now() + sidebarLayoutFollowDuration;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false, true);", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("window.requestAnimationFrame(followFrame)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("mutation.attributeName === \"aria-expanded\"", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("attributeFilter: [\"class\", \"aria-current\", \"aria-expanded\", \"style\"]", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("sidebarIndicatorDuration + 60", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("indicator.classList.remove(\"is-ready\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("return hasVisibleAreaWithin(target, nav) ? target : null;", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("moveSidebarIndicator(nav, sidebarTargetFromLink(nav, link), true, true)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("initializeSidebarIndicators(document);", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain(".ppjsidebarmenu.submenu", appCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-item-active-bg", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--vpp-sidebar-item-active-bg) !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-nav-indicator-color);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar .rz-navigation-item-icon-children", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".sidebar-collapsed .rz-navigation-item-icon-children", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("display: none !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".rz-navigation-item-link:focus-visible", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".rz-navigation-item-wrapper:focus-within", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-color: transparent !important;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown", polishCss, StringComparison.Ordinal);
        Assert.Contains("box-sizing: border-box;", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: calc(var(--vpp-sidebar-width) - var(--vpp-space-2));", polishCss, StringComparison.Ordinal);
        Assert.Contains("padding: 2px var(--vpp-space-1) var(--vpp-space-1);", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: var(--vpp-navigation-row-height);", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar:not(.sidebar-collapsed) .vpp-sidebar-user-menu .user-menu-trigger[aria-expanded=\"true\"]", polishCss, StringComparison.Ordinal);
        Assert.Contains("bottom: calc(var(--vpp-navigation-row-height) + var(--vpp-space-4));", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown-profile", polishCss, StringComparison.Ordinal);
        Assert.Contains("width: 64px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: 64px;", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown-email", polishCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-user-menu .user-dropdown-department", polishCss, StringComparison.Ordinal);
        Assert.Contains("display: inline-flex;", polishCss, StringComparison.Ordinal);
        Assert.Contains("border-radius: 999px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("height: 18px;", polishCss, StringComparison.Ordinal);
        Assert.Contains("align-items: baseline;", polishCss, StringComparison.Ordinal);
        Assert.Contains("vertical-align: baseline;", polishCss, StringComparison.Ordinal);
        Assert.Contains("color: var(--vpp-primary-600);", polishCss, StringComparison.Ordinal);
        Assert.Contains("background: rgba(var(--vpp-primary-rgb), 0.08);", polishCss, StringComparison.Ordinal);
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
    public void MyOrders_UsesTheAppleOrderWorkspaceContract()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor"));
        var codeBehind = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor.cs"));
        var orderPanel = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "VppOrderWorkspacePanel.razor"));
        var kpiStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-kpi.css"));
        var gridStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-datagrid.css"));
        var layoutStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));

        Assert.Contains("vpp-orders-summary-grid", source, StringComparison.Ordinal);
        Assert.Contains("vpp-orders-story-commands", source, StringComparison.Ordinal);
        Assert.DoesNotContain("export-pdf-coming-soon", source, StringComparison.Ordinal);
        Assert.DoesNotContain("export-excel-coming-soon", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PdfExportText", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("ExcelExportText", codeBehind, StringComparison.Ordinal);
        Assert.Equal(3, source.Split("<VppOrderWorkspacePanel", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("<RadzenTabs", source, StringComparison.Ordinal);
        Assert.Contains("role=\"group\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("role=\"radio\"", source, StringComparison.Ordinal);
        Assert.Equal(3, source.Split("aria-pressed", StringSplitOptions.None).Length - 1);
        Assert.Contains("orderView", codeBehind, StringComparison.Ordinal);
        Assert.Contains("GetUriWithQueryParameter(\"orderView\"", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-orders-deadline-track", source, StringComparison.Ordinal);
        Assert.Contains("PreviousCycleSummaryTitle", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-native-tab-list", source, StringComparison.Ordinal);
        Assert.Contains("vpp-order-card-statuses", orderPanel, StringComparison.Ordinal);
        Assert.Contains("vpp-data-card-actions", orderPanel, StringComparison.Ordinal);
        Assert.Contains("Title=\"#\" Width=\"56px\" TextAlign=\"TextAlign.Center\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("Property=\"Qty\" Title=\"@Loc[\"Quantity\"]\" Width=\"120px\" TextAlign=\"TextAlign.Right\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("Property=\"UomName\" Title=\"@Loc[\"UOM\"]\" Width=\"96px\" TextAlign=\"TextAlign.Center\"", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-card-kind", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-orders-state", source, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"false\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"true\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("VirtualizationOverscanCount=\"10\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("<EmptyTemplate>", orderPanel, StringComparison.Ordinal);
        Assert.Contains("EmptyActionText", orderPanel, StringComparison.Ordinal);
        Assert.Contains("EmptyActionClick", orderPanel, StringComparison.Ordinal);
        Assert.Contains("CreateOrderThisCycle", source, StringComparison.Ordinal);
        Assert.Contains("AvailableOrders.Count > 1", orderPanel, StringComparison.Ordinal);
        Assert.Contains("max-width: 1760px;", kpiStyles, StringComparison.Ordinal);
        Assert.Contains("height: 100%;", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-orders-summary-grid article", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-orders-selected-view", kpiStyles, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-orders-view-tabs", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-view-grid-frame", kpiStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px;", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-grid-embedded .rz-grid-table tbody > tr:hover", gridStyles, StringComparison.Ordinal);
        Assert.Contains("tbody > tr:nth-child(even)", gridStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-grid-scrollable .rz-grid-table thead", gridStyles, StringComparison.Ordinal);
        Assert.Contains("position: sticky;", gridStyles, StringComparison.Ordinal);
        Assert.Contains("scrollbar-gutter: stable both-edges;", gridStyles, StringComparison.Ordinal);
        Assert.Contains("width: 100%;", gridStyles, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--vpp-bg-base) !important;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-admin-tabs.vpp-orders-shell", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("border-right: 0;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("var(--vpp-border-default) var(--vpp-header-height) 100%", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: inset 0 -1px 0 var(--vpp-border-default);", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-chrome-bg: var(--vpp-surface-raised);", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatedShell_UsesOneReducedMotionSafeRefreshReveal()
    {
        var root = GetFrontendRoot();
        var app = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        var polishStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var interactions = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));

        Assert.Contains("vpp-page-entering", app, StringComparison.Ordinal);
        Assert.Contains("navigation.type === 'reload'", app, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion: reduce", app, StringComparison.Ordinal);
        Assert.Contains("vpp-shell-enter-inline", polishStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-shell-enter-block", polishStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-content-enter", polishStyles, StringComparison.Ordinal);
        Assert.Contains("cubic-bezier(0.32, 0.72, 0, 1)", polishStyles, StringComparison.Ordinal);
        Assert.Contains("animation: none !important;", polishStyles, StringComparison.Ordinal);
        Assert.Contains("function settlePageEntry()", interactions, StringComparison.Ordinal);
        Assert.Contains("event.persisted", interactions, StringComparison.Ordinal);
    }

    [Fact]
    public void HistoryRoute_UsesAlignedPagedSummaryAndBoundedDetailDrawer()
    {
        var root = GetFrontendRoot();
        var history = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor"));
        var historyCode = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.cs"));
        var historyStyles = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.css"));
        var historyScript = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.js"));
        // Sau C-7, markup từng vùng của màn Lịch sử nằm trong các component con presentational.
        var historyComponentsRoot = Path.Combine(root, "Components", "Pages", "VPPRequest", "Components");
        var historyKpis = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryKpiCards.razor"));
        var historyChart = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryTrendChart.razor"));
        var historyOrders = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryOrderList.razor"));
        var historyDrawer = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryOrderDetailSheet.razor"));

        Assert.Contains("vpp-history-kpis", historyKpis, StringComparison.Ordinal);
        Assert.Contains("vpp-history-loading-state", history, StringComparison.Ordinal);
        Assert.Contains("vpp-history-region-loading", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-grid-loading", historyOrders, StringComparison.Ordinal);
        Assert.Contains("vpp-history-chart-refresh", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-refresh", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-no-selection", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("HistoryChartNoData", historyChart, StringComparison.Ordinal);
        Assert.Contains("Summary.Periods.Count > 0 && (ShowRegularSeries || ShowAdditionalSeries)", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-clear", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("vpp-history-chart-legend-label", historyChart, StringComparison.Ordinal);
        Assert.Contains("<VppIcon Name=\"filter_none\" />", historyOrders, StringComparison.Ordinal);
        Assert.Contains("<VppIcon Name=\"filter_none\" />", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("Property=\"Item.Description\" Title=\"@Loc[\"Note\"]\"", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("ToggleDetailNote", history, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"#\" Width=\"42px\"", historyOrders, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"#\" Width=\"42px\"", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("VppContentState State=\"VppContentStateKind.Error\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("RadzenStackedColumnSeries", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-drawer", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"true\"", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("VirtualizationOverscanCount=\"4\"", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-grid-header", historyDrawer, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpandMode=", history, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpandMode=", historyOrders, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpandMode=", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("PageSize = 6;", historyCode, StringComparison.Ordinal);
        Assert.Contains("Math.Clamp(pageSize, 3, 20)", historyCode, StringComparison.Ordinal);
        Assert.Contains("SetHistoryViewport", historyCode, StringComparison.Ordinal);
        Assert.Contains("HasGridLoadError", history, StringComparison.Ordinal);
        Assert.Contains("_detailError", historyCode, StringComparison.Ordinal);
        Assert.Contains("ClearDetailFiltersAsync", historyCode, StringComparison.Ordinal);
        Assert.Contains("--vpp-history-inline-pill-height: 22px;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-note-column", historyStyles, StringComparison.Ordinal);
        Assert.Contains("::deep .vpp-history-popover-copy .vpp-icon", historyStyles, StringComparison.Ordinal);
        Assert.Contains("font-size: 14px;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: auto auto auto minmax(0, 1fr);", historyStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-history-skeleton", historyStyles, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", historyStyles, StringComparison.Ordinal);
        Assert.Contains(".rz-data-grid-data.has-vertical-overflow", historyStyles, StringComparison.Ordinal);
        Assert.Contains("--vpp-history-detail-scrollbar-width", historyStyles, StringComparison.Ordinal);
        Assert.Contains("position: absolute !important;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 1600px)", historyStyles, StringComparison.Ordinal);
        Assert.Contains("grid-row: 1 / 5;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("overflow-x: hidden !important;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("text-overflow: ellipsis;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("new ResizeObserver(() => {", historyScript, StringComparison.Ordinal);
        Assert.Contains("updateHistoryScrollGutters", historyScript, StringComparison.Ordinal);
        Assert.Contains("surface.offsetWidth - surface.clientWidth", historyScript, StringComparison.Ordinal);
        Assert.Contains("renderHistoryChartLabels", historyScript, StringComparison.Ordinal);
        Assert.Contains("surface.style.removeProperty('min-width')", historyScript, StringComparison.Ordinal);
        Assert.Contains("notation: 'compact'", historyScript, StringComparison.Ordinal);
        Assert.Contains("if (height >= 1100) return 8;", historyScript, StringComparison.Ordinal);
        Assert.Contains("if (height >= 680) return 4;", historyScript, StringComparison.Ordinal);
        Assert.Contains("return 3;", historyScript, StringComparison.Ordinal);
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

    [Fact]
    public void SharedStates_ExposeLiveRegionAndFocusContracts()
    {
        var root = GetFrontendRoot();
        var contentState = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Primitives", "VppContentState.razor"));
        var contentStateKind = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Primitives", "VppContentStateKind.cs"));
        var emptyState = File.ReadAllText(Path.Combine(root, "Components", "Shared", "VppEmptyState.razor"));
        var notifications = File.ReadAllText(Path.Combine(root, "Components", "Layout", "NotificationCenter.razor"));

        Assert.Contains("VppContentStateKind State", contentState, StringComparison.Ordinal);
        foreach (var state in new[] { "Empty", "FilteredEmpty", "Loading", "Error", "Denied", "Disabled", "Success", "Warning" })
        {
            Assert.Contains(state, contentStateKind, StringComparison.Ordinal);
        }
        Assert.Contains("vpp-content-state-@StateCssClass", contentState, StringComparison.Ordinal);
        Assert.Contains("vpp-state-panel-@StateCssClass", contentState, StringComparison.Ordinal);
        Assert.Contains("Icon=\"@PrimaryActionIcon\"", contentState, StringComparison.Ordinal);
        Assert.Contains("role=\"@SemanticRole\"", contentState, StringComparison.Ordinal);
        Assert.Contains("aria-busy=\"@IsLoading\"", contentState, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"@(HasDescription ? DescriptionId : null)\"", contentState, StringComparison.Ordinal);
        Assert.Contains("role=\"status\"", emptyState, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"@TitleId\"", emptyState, StringComparison.Ordinal);
        Assert.Contains("tabindex=\"-1\"", notifications, StringComparison.Ordinal);
        Assert.Contains("await _panel.FocusAsync();", notifications, StringComparison.Ordinal);
        Assert.Contains("await _trigger.FocusAsync();", notifications, StringComparison.Ordinal);
        Assert.Contains("vpp-notification-empty--loading", notifications, StringComparison.Ordinal);

        var historyOrders = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var catalog = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_ProductCatalog.razor"));
        Assert.Contains("VppContentStateKind.FilteredEmpty", historyOrders, StringComparison.Ordinal);
        Assert.Contains("VppContentStateKind.Error", catalog, StringComparison.Ordinal);
        Assert.Contains("PrimaryActionIcon=\"@VppIcons.Refresh\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("PrimaryActionIcon=\"@VppIcons.Refresh\"", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void PrintStyles_RemoveInteractiveChromeAndFlattenScrollableContent()
    {
        var root = GetFrontendRoot();
        var styles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));

        Assert.Contains("@media print", styles, StringComparison.Ordinal);
        Assert.Contains("#components-reconnect-modal", styles, StringComparison.Ordinal);
        Assert.Contains(".vpp-notification-panel", styles, StringComparison.Ordinal);
        Assert.Contains("overflow: visible !important;", styles, StringComparison.Ordinal);
        Assert.Contains("background: #fff !important;", styles, StringComparison.Ordinal);
        Assert.Contains("break-inside: avoid;", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void RadzenGridRegions_UseTheAccessibilityCompatibilityLayer()
    {
        var root = GetFrontendRoot();
        var script = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));
        var orderPanel = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "VppOrderWorkspacePanel.razor"));
        var libraryGrid = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Component_ShareGrid.razor"));
        var report = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Report.razor"));

        Assert.Contains("data-vpp-grid-region=\"true\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("data-vpp-grid-region=\"true\"", libraryGrid, StringComparison.Ordinal);
        Assert.Equal(2, report.Split("data-vpp-grid-region=\"true\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("normalizeGridRegions", script, StringComparison.Ordinal);
        Assert.Contains("grid.setAttribute(\"role\", \"region\")", script, StringComparison.Ordinal);
        Assert.Contains("grid.removeAttribute(\"aria-rowcount\")", script, StringComparison.Ordinal);
        Assert.Contains("table.setAttribute(\"role\", \"table\")", script, StringComparison.Ordinal);
        Assert.Contains("scrollRegion.setAttribute(\"tabindex\", \"0\")", script, StringComparison.Ordinal);
        Assert.Contains("normalizeRadzenAriaValues", script, StringComparison.Ordinal);
        Assert.Contains("element.setAttribute(\"aria-disabled\", disabled ? \"true\" : \"false\")", script, StringComparison.Ordinal);
        Assert.Contains("new MutationObserver", script, StringComparison.Ordinal);
    }

    [Fact]
    public void ReconnectModal_UsesTheAccessibleActionableStateContract()
    {
        var root = GetFrontendRoot();
        var componentRoot = Path.Combine(root, "Components", "Layout");
        var source = File.ReadAllText(Path.Combine(componentRoot, "ReconnectModal.razor"));
        var styles = File.ReadAllText(Path.Combine(componentRoot, "ReconnectModal.razor.css"));
        var script = File.ReadAllText(Path.Combine(componentRoot, "ReconnectModal.razor.js"));

        Assert.Contains("aria-label=\"@Loc[\"ConnectionStatus\"]\"", source, StringComparison.Ordinal);
        Assert.Contains("components-seconds-to-next-attempt", source, StringComparison.Ordinal);
        Assert.Contains("components-reconnect-button", source, StringComparison.Ordinal);
        Assert.Contains("components-resume-button", source, StringComparison.Ordinal);
        Assert.Contains("components-reconnect-spinner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("components-rejoining-animation", source, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px;", styles, StringComparison.Ordinal);
        Assert.Contains("outline: none !important;", styles, StringComparison.Ordinal);
        Assert.Contains(":focus-visible", styles, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion: reduce", styles, StringComparison.Ordinal);
        Assert.Contains("forced-colors: active", styles, StringComparison.Ordinal);
        Assert.Contains("state === \"show\" || state === \"retrying\"", script, StringComparison.Ordinal);
        Assert.Contains("state === \"failed\"", script, StringComparison.Ordinal);
        Assert.Contains("state === \"paused\" || state === \"resume-failed\"", script, StringComparison.Ordinal);
        Assert.Contains("location.reload();", script, StringComparison.Ordinal);
    }

    private static string GetFrontendRoot()
    {
        var repositoryRoot = FindRepositoryRoot();
        return Path.Combine(repositoryRoot, "src", "Frontend", "Blazor");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
