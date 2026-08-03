using gtas_vpp_fe.Components.Layout;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class SharedUiFoundationTests
{
    [Fact]
    public void AuthoredStyles_DoNotForceUppercaseUiText()
    {
        var root = GetFrontendRoot();
        var authoredStyles = Directory.EnumerateFiles(Path.Combine(root, "wwwroot", "css"), "*.css", SearchOption.AllDirectories);

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
        var iconComponent = Path.Combine(componentRoot, "DesignSystem", "Primitives", "VppIcon.razor");

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

    [Theory]
    [InlineData("VppIcon.razor")]
    [InlineData("VppIcons.cs")]
    [InlineData("VppBrandMark.razor")]
    public void IconAndBrandPrimitives_AreOwnedByTheDesignSystem(string fileName)
    {
        var componentRoot = Path.Combine(GetFrontendRoot(), "Components");

        Assert.True(File.Exists(Path.Combine(componentRoot, "DesignSystem", "Primitives", fileName)));
        Assert.False(File.Exists(Path.Combine(componentRoot, "Shared", fileName)));
    }

    [Theory]
    [InlineData("SkeletonGrid.razor")]
    [InlineData("SkeletonGrid.razor.css")]
    [InlineData("SkeletonPage.razor")]
    [InlineData("SkeletonStatCards.razor")]
    [InlineData("SkeletonStatCards.razor.css")]
    public void LoadingPrimitives_AreOwnedByTheDesignSystem(string fileName)
    {
        var componentRoot = Path.Combine(GetFrontendRoot(), "Components");

        Assert.True(File.Exists(Path.Combine(componentRoot, "DesignSystem", "Primitives", fileName)));
        Assert.False(File.Exists(Path.Combine(componentRoot, "Shared", fileName)));
    }

    [Fact]
    public void ColumnPicker_OwnsTheIsolatedRadzenReflectionWorkaround()
    {
        var componentRoot = Path.Combine(GetFrontendRoot(), "Components");
        var pickerPath = Path.Combine(
            componentRoot,
            "DesignSystem",
            "Composites",
            "VppColumnPicker.razor");
        var pickerCssPath = Path.Combine(
            componentRoot,
            "DesignSystem",
            "Composites",
            "VppColumnPicker.razor.css");
        var source = File.ReadAllText(pickerPath);
        var scopedCss = File.ReadAllText(pickerCssPath);
        var adminCss = File.ReadAllText(Path.Combine(GetFrontendRoot(), "wwwroot", "css", "vpp-admin.css"));

        Assert.False(File.Exists(Path.Combine(componentRoot, "Shared", "VppColumnPicker.razor")));
        Assert.Contains("BindingFlags.Instance | BindingFlags.NonPublic", source, StringComparison.Ordinal);
        Assert.Contains("\"SetVisible\"", source, StringComparison.Ordinal);
        Assert.Contains("\"ChangeState\"", source, StringComparison.Ordinal);
        Assert.Contains(".vpp-column-picker-trigger", scopedCss, StringComparison.Ordinal);
        Assert.Contains("::deep .vpp-icon", scopedCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-column-picker-", adminCss, StringComparison.Ordinal);

        var otherReflectionConsumers = Directory
            .EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).Equals(
                Path.GetFullPath(pickerPath),
                StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains(
                "BindingFlags.NonPublic",
                StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(componentRoot, path))
            .ToArray();

        Assert.Empty(otherReflectionConsumers);
    }

    [Theory]
    [InlineData("Primitives", "VppInlineAlert.razor")]
    [InlineData("Primitives", "VppAlertTone.cs")]
    [InlineData("Composites", "VppPageHeader.razor")]
    public void GenericSharedUi_IsOwnedByTheCorrectDesignSystemLayer(
        string layer,
        string fileName)
    {
        var componentRoot = Path.Combine(GetFrontendRoot(), "Components");

        Assert.True(File.Exists(Path.Combine(componentRoot, "DesignSystem", layer, fileName)));
        Assert.False(File.Exists(Path.Combine(componentRoot, "Shared", fileName)));
    }

    [Fact]
    public void ComponentsSharedFolder_HasNoRemainingSourceOwner()
    {
        var sharedRoot = Path.Combine(GetFrontendRoot(), "Components", "Shared");
        var remainingFiles = Directory.Exists(sharedRoot)
            ? Directory.EnumerateFiles(sharedRoot, "*", SearchOption.AllDirectories).ToArray()
            : [];

        Assert.Empty(remainingFiles);
    }

    [Fact]
    public void NotificationState_SeparatesApiRealtimeAndUiStateOwnership()
    {
        var root = GetFrontendRoot();
        var statePath = Path.Combine(
            root,
            "Features",
            "Notifications",
            "State",
            "NotificationInboxState.cs");
        var state = File.ReadAllText(statePath);
        var apiClient = File.ReadAllText(Path.Combine(
            root, "Features", "Notifications", "Api", "NotificationApiClient.cs"));
        var realtimeClient = File.ReadAllText(Path.Combine(
            root, "Features", "Notifications", "Realtime", "NotificationRealtimeClient.cs"));

        Assert.Contains("NotificationApiClient", state, StringComparison.Ordinal);
        Assert.Contains("namespace gtas_vpp_fe.Features.Notifications.State;", state, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Services", "NotificationInboxState.cs")));
        Assert.Contains("INotificationRealtimeClient", state, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", state, StringComparison.Ordinal);
        Assert.DoesNotContain("IHttpClientFactory", state, StringComparison.Ordinal);
        Assert.DoesNotContain("HubConnectionBuilder", state, StringComparison.Ordinal);
        Assert.DoesNotContain("api/notifications", state, StringComparison.Ordinal);
        Assert.DoesNotContain("hubs/notifications", state, StringComparison.Ordinal);
        Assert.Contains("/api/notifications", apiClient, StringComparison.Ordinal);
        Assert.Contains("hubs/notifications", realtimeClient, StringComparison.Ordinal);
        Assert.Contains("WithAutomaticReconnect", realtimeClient, StringComparison.Ordinal);
        Assert.Contains("IDisposable? _notificationSubscription", realtimeClient, StringComparison.Ordinal);
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

        // Header cấp hai là navigation typed, không dùng decision selector thay route navigation.
        var librarySource = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Component_Library.razor"));
        Assert.Contains("<VppHeaderTabGroup", librarySource, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppSegmentedSelector", librarySource, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-secondary-tabs", librarySource, StringComparison.Ordinal);
    }

    [Fact]
    public void NestedHeaderNavigation_UsesOneTypedGroupForPeriodAndPricingRoutes()
    {
        var root = GetFrontendRoot();
        var group = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppHeaderTabGroup.razor"));
        var model = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppHeaderSubTab.cs"));
        var shell = File.ReadAllText(Path.Combine(root, "Components", "Layout", "LeftSidebar.razor"));
        var shellCode = File.ReadAllText(Path.Combine(root, "Components", "Layout", "LeftSidebar.razor.cs"));
        var period = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_AdminApproval.razor"));
        var library = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Component_Library.razor"));

        Assert.Contains("IReadOnlyList<VppHeaderSubTab>", group, StringComparison.Ordinal);
        Assert.Contains("sealed record VppHeaderSubTab", model, StringComparison.Ordinal);
        Assert.Contains("<span class=\"vpp-header-tab vpp-header-tab-parent\">", group, StringComparison.Ordinal);
        Assert.DoesNotContain("ParentPath", group, StringComparison.Ordinal);
        Assert.Contains("<VppHeaderTabGroup", shell, StringComparison.Ordinal);
        Assert.Contains("periodChildren", shellCode, StringComparison.Ordinal);
        Assert.Contains("pricingChildren", shellCode, StringComparison.Ordinal);
        Assert.Contains("<VppHeaderTabGroup", period, StringComparison.Ordinal);
        Assert.Contains("<VppHeaderTabGroup", library, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-library-local-selector", library, StringComparison.Ordinal);
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
        var appSource = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        var interactionsJs = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));
        var componentVppRequest = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Component_VPPRequest.razor"));
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
        Assert.Contains("function isDataRowMutationRoot(node)", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("if (containsInteractionHost(root))", interactionsJs, StringComparison.Ordinal);
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
        Assert.Contains(".vpp-tab-indicator-host", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-header-tabs", interactionsJs, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview .rz-tabview-nav:focus-visible", tabsCss, StringComparison.Ordinal);
        Assert.Contains("outline: none !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview-selected > .rz-tabview-nav-link", tabsCss, StringComparison.Ordinal);
        Assert.Contains("button[role=\"tab\"]", tabsCss, StringComparison.Ordinal);
        Assert.Contains(".rz-tabview-title", tabsCss, StringComparison.Ordinal);
        Assert.Contains("box-shadow: inset 0 -1px 0 var(--vpp-border-default);", tabsCss, StringComparison.Ordinal);
        Assert.Contains("content: none;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-layout-body-inset: var(--vpp-page-inset-inline-start);", layoutCss, StringComparison.Ordinal);
        Assert.DoesNotContain("--vpp-layout-body-inset: var(--vpp-space-5);", layoutCss, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "wwwroot", "app.css")));
        Assert.DoesNotContain("app.css", appSource, StringComparison.Ordinal);
        Assert.Contains(".vpp-admin-tabs.vpp-history-shell > .rz-tabview-panels > .rz-tabview-panel", layoutCss, StringComparison.Ordinal);
        Assert.Contains("flex: 1 1 auto;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("UsesHistoryWorkspace", componentVppRequest, StringComparison.Ordinal);
        Assert.Contains("gap: 0 !important;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("width: calc(100% + var(--vpp-page-inset-inline-start) + var(--vpp-page-inset-inline-end));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline-start: calc(-1 * var(--vpp-page-inset-inline-start));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-inline-end: calc(-1 * var(--vpp-page-inset-inline-end));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-block-start: calc(-1 * var(--vpp-page-inset-block-start));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("margin-block-end: var(--vpp-page-inset-block-start);", tabsCss, StringComparison.Ordinal);
        Assert.DoesNotContain("librariestab", tabsCss, StringComparison.Ordinal);
        Assert.Contains("border: 0 !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("background: transparent !important;", tabsCss, StringComparison.Ordinal);
        Assert.Contains("top: calc(var(--vpp-tabs-sticky-top) - var(--vpp-page-inset-block-start));", tabsCss, StringComparison.Ordinal);
        Assert.Contains("display: flow-root;", adminCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-admin-tabs .rz-tabview-nav-container", adminCss, StringComparison.Ordinal);
        Assert.DoesNotContain("ul[role=\"tablist\"]", tabsCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-base-duration: 180ms;", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-easing-standard: cubic-bezier(0.2, 0, 0, 1);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-motion-duration: var(--vpp-motion-base-duration);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-motion-easing: var(--vpp-motion-easing-standard);", tokensCss, StringComparison.Ordinal);
        Assert.Contains("var tabIndicatorDuration = navigationMotionDuration;", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("function normalizePrimaryTabChrome(tabList, host)", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("host.style.setProperty(\"height\", headerHeight, \"important\")", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("tabList.style.setProperty(\"padding-block\", verticalInset, \"important\")", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("var tabListSelector = \".rz-tabview-nav, .vpp-header-tabs, .vpp-local-header-tabs\";", interactionsJs, StringComparison.Ordinal);
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
        var sourceCode = File.ReadAllText(Path.Combine(root, "Components", "Layout", "LeftSidebar.razor.cs"));
        var userMenuSource = File.ReadAllText(Path.Combine(root, "Components", "Layout", "UserMenu.razor"));
        var brandMarkSource = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Primitives", "VppBrandMark.razor"));
        var appSource = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        var layoutCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));
        var polishCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var responsiveCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-responsive.css"));
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
        Assert.Contains("SidebarSubline=\"@RoleBadgeLabel\"", source, StringComparison.Ordinal);
        Assert.Contains("@SidebarSublineDisplay", userMenuSource, StringComparison.Ordinal);
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
        Assert.Contains("vpp-sidebar-header-actions", source, StringComparison.Ordinal);
        Assert.Contains("vpp-sidebar-tree-toggle", source, StringComparison.Ordinal);
        Assert.Contains("Multiple=\"true\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Expanded=\"_dashboardMenuExpanded\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Expanded=\"_periodMenuExpanded\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Expanded=\"_libraryMenuExpanded\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Expanded=\"_pricingMenuExpanded\"", source, StringComparison.Ordinal);
        Assert.Contains("@bind-Expanded=\"_permissionMenuExpanded\"", source, StringComparison.Ordinal);
        Assert.Contains("ToggleAllSidebarGroups", sourceCode, StringComparison.Ordinal);
        Assert.Contains("AreAllSidebarGroupsExpanded", sourceCode, StringComparison.Ordinal);
        Assert.Contains("HasExpandableSidebarGroups", sourceCode, StringComparison.Ordinal);
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
        Assert.Contains(".user-avatar {", polishCss, StringComparison.Ordinal);
        Assert.Contains("font-size: 10px;", polishCss, StringComparison.Ordinal);
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
        // khu vực; nav RadzenTabs cấp cao nhất trong body chỉ còn phục vụ mobile.
        // Nhóm route một cấp con dùng typed header group và local mobile fallback.
        Assert.DoesNotContain("grid-template-rows: 0 1fr;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: var(--vpp-header-height) 1fr;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-admin-tabs > .rz-tabview-nav-container,\n    .vpp-admin-tabs > .rz-tabview-nav {\n        display: none !important;\n    }", layoutCss.Replace("\r\n", "\n"), StringComparison.Ordinal);
        Assert.Contains(".vpp-header-tabs", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-header-tab-group", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-sidebar-header-actions", layoutCss, StringComparison.Ordinal);
        Assert.Contains("flex-basis: var(--vpp-navigation-row-height);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("align-self: stretch;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("height: auto;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-local-header-tabs", layoutCss, StringComparison.Ordinal);
        Assert.Contains("-webkit-text-fill-color: currentColor;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-layout-header .vpp-header-tab-parent", layoutCss, StringComparison.Ordinal);
        Assert.Contains("pointer-events: none;", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-layout-header .vpp-header-tab", layoutCss, StringComparison.Ordinal);
        Assert.Contains("color: var(--vpp-text-secondary);", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-layout-header .vpp-header-tab::before", layoutCss, StringComparison.Ordinal);
        Assert.Contains("gap: var(--vpp-navigation-surface-gap);", layoutCss, StringComparison.Ordinal);
        Assert.Contains("inset: var(--vpp-navigation-surface-cross-inset) 0;", layoutCss, StringComparison.Ordinal);
        Assert.Contains("border-radius: var(--vpp-radius-md);", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-layout-header .vpp-header-tab:hover::before", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-layout-header .vpp-header-tab:focus-visible::before", layoutCss, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-header-breadcrumb", layoutCss, StringComparison.Ordinal);
        Assert.Contains("vpp-header-tabs", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-header-breadcrumb", source, StringComparison.Ordinal);
        Assert.Equal(
            "/dashboard?tab=3&managementTab=department",
            ShellNavigationCatalog.DepartmentSummary.Path);
        Assert.Contains("ShellNavigationCatalog.DepartmentSummary.Path", sourceCode, StringComparison.Ordinal);
        Assert.True(
            Array.IndexOf(ShellNavigationCatalog.Dashboard.Items.ToArray(), ShellNavigationCatalog.History)
            < Array.IndexOf(ShellNavigationCatalog.Dashboard.Items.ToArray(), ShellNavigationCatalog.DepartmentSummary));
        Assert.True(
            Array.IndexOf(ShellNavigationCatalog.Dashboard.Items.ToArray(), ShellNavigationCatalog.DepartmentSummary)
            < Array.IndexOf(ShellNavigationCatalog.Dashboard.Items.ToArray(), ShellNavigationCatalog.Catalog));
        Assert.DoesNotContain("vpp-header-role-badge", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-header-role-badge", layoutCss, StringComparison.Ordinal);
        Assert.Contains("\"rz-sidebar rz-header\"", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar", responsiveCss, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarNavigation_UsesOpenAiInspiredMinimalRowStates()
    {
        var root = GetFrontendRoot();
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));
        var layoutCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var polishCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var tokensCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var interactionsJs = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));

        Assert.Contains("--vpp-sidebar-item-hover-bg: var(--vpp-navigation-item-hover-bg);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-font-sidebar: var(--vpp-font-body);", tokensCss, StringComparison.Ordinal);
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
        Assert.Contains("margin: 0 var(--vpp-navigation-surface-cross-inset);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-margin-inline: var(--vpp-space-1);", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-padding-block: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-item-2nd-level-padding-block: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--rz-panel-menu-2nd-level-vertical-offset: 0;", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("--vpp-sidebar-row-half-gap: calc(var(--vpp-navigation-surface-gap) / 2);", sidebarCss, StringComparison.Ordinal);
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
        Assert.DoesNotContain("radial-gradient(circle", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-sidebar-press-expand", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", sidebarCss, StringComparison.Ordinal);
        Assert.DoesNotContain("function playPressSurface(target)", interactionsJs, StringComparison.Ordinal);
        Assert.DoesNotContain("void surface.offsetWidth", interactionsJs, StringComparison.Ordinal);
        Assert.Contains(".vpp-header-tab", interactionsJs, StringComparison.Ordinal);
        Assert.Contains("position: relative;", polishCss, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-pressing", polishCss, StringComparison.Ordinal);
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
        Assert.False(File.Exists(Path.Combine(root, "wwwroot", "app.css")));
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
    public void MyOrders_UsesTheMinimalOrderWorkspaceContract()
    {
        var root = GetFrontendRoot();
        var source = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor"));
        var codeBehind = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor.cs"));
        var orderPanel = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "VppOrderWorkspacePanel.razor"));
        var orderCreate = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Page_OrderCreate.razor.cs"));
        var submissionCoordinator = File.ReadAllText(Path.Combine(root, "Features", "Requests", "Submission", "OrderSubmissionCoordinator.cs"));
        var submissionFactory = File.ReadAllText(Path.Combine(root, "Features", "Requests", "Submission", "OrderSubmissionRequestFactory.cs"));
        var orderItemsSurface = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor"));
        var orderItemsStyles = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor.css"));
        var kpiStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-kpi.css"));
        var gridStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-datagrid.css"));
        var layoutStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));

        var segmentedSelector = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppSegmentedSelector.razor"));
        Assert.Contains("vpp-orders-view-selector", source, StringComparison.Ordinal);
        Assert.Contains("<VppSegmentedSelector", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-orders-story-commands", source, StringComparison.Ordinal);
        Assert.Contains("PrimaryActionText=\"@SupplementPrimaryActionText\"", source, StringComparison.Ordinal);
        Assert.Contains("data-testid=\"@PrimaryActionTestId\"", orderPanel, StringComparison.Ordinal);
        Assert.Contains("PrimaryActionTestId=\"create-supplement\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("export-pdf-coming-soon", source, StringComparison.Ordinal);
        Assert.DoesNotContain("export-excel-coming-soon", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PdfExportText", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("ExcelExportText", codeBehind, StringComparison.Ordinal);
        Assert.Equal(3, source.Split("<VppOrderWorkspacePanel", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("<RadzenTabs", source, StringComparison.Ordinal);
        Assert.Contains("role=\"group\"", segmentedSelector, StringComparison.Ordinal);
        Assert.Contains("aria-pressed", segmentedSelector, StringComparison.Ordinal);
        Assert.Contains("orderView", codeBehind, StringComparison.Ordinal);
        Assert.Contains("GetUriWithQueryParameter(\"orderView\"", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-orders-deadline-track", source, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-orders-selection-summary", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedOrderViewSummary", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("Badge:", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("CurrentRegularLineCount", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedSupplementLineCount", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviousOrderLineCount", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-native-tab-list", source, StringComparison.Ordinal);
        Assert.Contains("vpp-order-code-static", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-card-statuses", orderPanel, StringComparison.Ordinal);
        Assert.Contains("vpp-data-card-actions", orderPanel, StringComparison.Ordinal);
        Assert.Contains("CanRestore", source, StringComparison.Ordinal);
        Assert.Contains("CanRecreate", source, StringComparison.Ordinal);
        Assert.Equal(2, source.Split("RestoreRequested=\"@RestoreCancelledOrderAsync\"", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, source.Split("RecreateRequested=\"@GoToRecreatePage\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("RestoreRequested", orderPanel, StringComparison.Ordinal);
        Assert.Contains("RecreateRequested", orderPanel, StringComparison.Ordinal);
        Assert.Contains("RestoreOrder", orderPanel, StringComparison.Ordinal);
        Assert.Contains("RecreateOrder", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildRecreateRequest", orderCreate, StringComparison.Ordinal);
        Assert.Contains("BuildRecreateRequest", submissionCoordinator, StringComparison.Ordinal);
        Assert.Contains("VppRequestRecreateReqDTO", submissionFactory, StringComparison.Ordinal);
        Assert.Contains("mode=recreate", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("CanReplace", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CanReplace", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("ReplaceOrder", orderPanel, StringComparison.Ordinal);
        Assert.Contains("VppOrderItemsSurfaceVariant.Workspace", orderPanel, StringComparison.Ordinal);
        Assert.Contains("Property=\"Quantity\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("Property=\"UomName\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("private string QuantityColumnWidth", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("private string UnitColumnWidth", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("vpp-order-items-toolbar", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("<VppFilterSearch", orderItemsSurface, StringComparison.Ordinal);
        Assert.Equal(2, orderItemsSurface.Split("<VppFilterSelect", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("vpp-order-view-filters", orderPanel, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-items-toolbar", orderItemsStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-card-kind", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-orders-state", source, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"@UsePaging\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"false\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.SmallStatic", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("<EmptyTemplate>", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("EmptyActionText", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("EmptyActionClick", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("CreateOrderThisCycle", codeBehind, StringComparison.Ordinal);
        Assert.Contains("AvailableOrders.Count > 1", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("max-width: 1760px;", kpiStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("margin-inline: auto;", kpiStyles, StringComparison.Ordinal);
        Assert.Contains("height: 100%;", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-orders-view-switchbar", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-orders-selected-view", kpiStyles, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-orders-view-tabs", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-view-grid-frame", kpiStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px;", kpiStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-grid-embedded .rz-grid-table tbody > tr:hover", gridStyles, StringComparison.Ordinal);
        Assert.Contains("tbody > tr:nth-child(even)", gridStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-grid-scrollable .rz-grid-table thead", gridStyles, StringComparison.Ordinal);
        Assert.Contains("position: sticky;", gridStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("scrollbar-gutter", gridStyles, StringComparison.Ordinal);
        Assert.Contains("width: 100%;", gridStyles, StringComparison.Ordinal);
        Assert.Contains("background-color: var(--vpp-bg-base) !important;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-admin-tabs.vpp-orders-shell", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("overflow: visible;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("border-right: 0;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("--vpp-shell-sidebar-track: var(--vpp-sidebar-width);", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("inset-inline-start: calc(var(--vpp-shell-sidebar-track) - 1px);", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("transition: inset-inline-start var(--vpp-sidebar-transition);", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("background-image: none;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", layoutStyles, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: inset 0 -1px 0 var(--vpp-border-default);", layoutStyles, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-chrome-bg: var(--vpp-surface-raised);", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspacePatterns_AreTypedAndBackedByRealConsumers()
    {
        var root = GetFrontendRoot();
        var patternsRoot = Path.Combine(root, "Components", "DesignSystem", "Patterns");
        var patternFiles = new[]
        {
            "VppAccountWorkspace.razor",
            "VppCollectionWorkspace.razor",
            "VppListDetailWorkspace.razor",
            "VppSplitEditorWorkspace.razor",
            "VppOperationWorkspace.razor",
            "VppAnalyticsWorkspace.razor"
        };

        foreach (var file in patternFiles)
        {
            Assert.True(File.Exists(Path.Combine(patternsRoot, file)), $"Missing workspace pattern: {file}");
        }

        var patternSource = string.Join(
            Environment.NewLine,
            Directory.GetFiles(patternsRoot, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));
        Assert.DoesNotContain("UniversalPage", patternSource, StringComparison.Ordinal);
        Assert.DoesNotContain("UniversalGrid", patternSource, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", patternSource, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", patternSource, StringComparison.Ordinal);
        Assert.Contains("VppListDetailRatio", patternSource, StringComparison.Ordinal);
        Assert.Contains("VppSplitEditorRatio", patternSource, StringComparison.Ordinal);
        Assert.Contains("RenderFragment", patternSource, StringComparison.Ordinal);

        var accountWorkspace = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Patterns", "VppAccountWorkspace.razor"));
        var authRoot = Path.Combine(root, "Components", "Pages", "Authen");
        var accountConsumerCount = Directory.GetFiles(authRoot, "*.razor", SearchOption.TopDirectoryOnly)
            .Sum(path => File.ReadAllText(path).Split("<VppAccountWorkspace", StringSplitOptions.None).Length - 1);
        Assert.Contains("data-vpp-workspace-pattern=\"account\"", accountWorkspace, StringComparison.Ordinal);
        Assert.True(accountConsumerCount >= 2, "Account pattern needs at least two real route consumers.");

        AssertPatternConsumer(root, "Pages", "VPPRequest", "Tabs", "Tab_ProductCatalog.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_PriceLibrary.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_CategoryLibrary.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_SupplierLibrary.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_ItemLibrary.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_DepartmentLibrary.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_PriceListLibrary.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Permission", "Tabs", "Tab_User.razor", "<VppCollectionWorkspace");
        AssertPatternConsumer(root, "Pages", "Lib", "Tabs", "Tab_LookupLibrary.razor", "<VppSplitEditorWorkspace");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "OrderCreateStep2.razor", "<VppSplitEditorWorkspace");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor", "<HistoryWorkspaceShell");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "Components", "PendingApprovalWorkspace.razor", "<VppOperationWorkspace");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "Tabs", "Tab_History.razor", "<HistoryWorkspaceShell");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "Tabs", "Tab_DepartmentSummary.razor", "<HistoryWorkspaceShell");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "Page_OrderCreate.razor", "<VppWorkflowStepper");
        AssertPatternConsumer(root, "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor", "<VppSegmentedSelector");

        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        foreach (var token in new[]
                 {
                     "--vpp-page-inset-block-start",
                     "--vpp-page-inset-inline-end",
                     "--vpp-page-inset-block-end",
                     "--vpp-page-inset-inline-start"
                 })
        {
            Assert.Contains(token, tokens, StringComparison.Ordinal);
        }

        Assert.Contains("--vpp-navigation-surface-gap", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-navigation-surface-cross-inset", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminDialogs_UseTypedAdaptiveContractAndStickyShell()
    {
        var root = GetFrontendRoot();
        var compositesRoot = Path.Combine(root, "Components", "DesignSystem", "Composites");
        var contracts = File.ReadAllText(Path.Combine(compositesRoot, "VppAdminDialogContracts.cs"));
        var shell = File.ReadAllText(Path.Combine(compositesRoot, "VppAdaptiveDialogShell.razor"));
        var adminStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-admin.css"));
        var accessibilityStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-a11y.css"));
        var lookupTab = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_LookupLibrary.razor.cs"));
        var lookupDialog = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Dialog", "Dialog_AddLookupCategory.razor"));

        foreach (var size in new[] { "Compact", "Standard", "Workspace" })
        {
            Assert.Contains(size, contracts, StringComparison.Ordinal);
        }

        Assert.Contains("VppAdminDialogProfiles", contracts, StringComparison.Ordinal);
        Assert.Contains("CssClass", contracts, StringComparison.Ordinal);
        Assert.Contains("ContentCssClass", contracts, StringComparison.Ordinal);
        Assert.Contains("AutoFocusFirstElement", contracts, StringComparison.Ordinal);
        Assert.Contains("data-vpp-admin-dialog-size", shell, StringComparison.Ordinal);
        Assert.Contains("vpp-adaptive-dialog-body", shell, StringComparison.Ordinal);
        Assert.Contains("vpp-adaptive-dialog-footer", shell, StringComparison.Ordinal);
        Assert.Contains("overflow: auto;", adminStyles, StringComparison.Ordinal);
        Assert.Contains("100dvh", adminStyles, StringComparison.Ordinal);
        Assert.Contains("VppAdminDialogProfiles.Create", lookupTab, StringComparison.Ordinal);
        Assert.Contains("<VppAdaptiveDialogShell", lookupDialog, StringComparison.Ordinal);
        Assert.Contains(".rz-form-field :is(.rz-textbox, .rz-inputtext, .rz-dropdown, .rz-numeric, .rz-datepicker, .rz-textarea):focus-visible", accessibilityStyles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important;", accessibilityStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("border-radius: 2px;", accessibilityStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminActiveSwitch_UsesSemanticSuccessTrackAndNeutralThumb()
    {
        var root = GetFrontendRoot();
        var adminStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-admin.css"));
        var lookup = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_LookupLibrary.razor"));
        var activeToggle = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppAdminActiveToggle.razor"));
        var activeToggleStyles = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppAdminActiveToggle.razor.css"));

        Assert.DoesNotContain(".vpp-admin-active-switch", adminStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-admin-active-switch ::deep .rz-switch", activeToggleStyles, StringComparison.Ordinal);
        Assert.Contains("--rz-switch-checked-background-color: color-mix(in srgb, var(--vpp-success) 82%, var(--vpp-bg-elevated));", activeToggleStyles, StringComparison.Ordinal);
        Assert.Contains("--rz-switch-checked-circle-background-color: var(--vpp-color-white);", activeToggleStyles, StringComparison.Ordinal);
        Assert.Contains("<RadzenSwitch TValue=\"bool\"", activeToggle, StringComparison.Ordinal);
        Assert.Equal(2, lookup.Split("<VppAdminActiveToggle", StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void TransientSurfaces_UseOneDirectionalMotionContract()
    {
        var root = GetFrontendRoot();
        var polish = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var userMenu = File.ReadAllText(Path.Combine(root, "Components", "Layout", "UserMenu.razor"));
        var orderSurface = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor"));
        var cellValuePopover = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppCellValuePopover.razor"));
        var historyList = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var historyKpis = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryKpiCards.razor"));
        var orderCreate = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "OrderCreateStep2.razor"));

        Assert.Contains("--vpp-transient-motion-duration", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-transient-motion-easing", tokens, StringComparison.Ordinal);
        Assert.Contains("@keyframes vpp-transient-enter-down", polish, StringComparison.Ordinal);
        Assert.Contains("@keyframes vpp-transient-enter-up", polish, StringComparison.Ordinal);
        Assert.Contains("@keyframes vpp-transient-enter-center", polish, StringComparison.Ordinal);
        Assert.Contains(".vpp-transient-surface.is-above", polish, StringComparison.Ordinal);
        Assert.Contains("clip-path:", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("transform: translateY(calc(0px - var(--vpp-transient-motion-distance)))", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("transform: translateY(var(--vpp-transient-motion-distance))", polish, StringComparison.Ordinal);
        Assert.Contains("vpp-transient-surface", userMenu, StringComparison.Ordinal);
        Assert.Contains("<VppCellValuePopover", orderSurface, StringComparison.Ordinal);
        Assert.Contains("<VppCellValuePopover", historyList, StringComparison.Ordinal);
        Assert.Contains("vpp-transient-surface", cellValuePopover, StringComparison.Ordinal);
        Assert.Contains("vpp-transient-surface", historyKpis, StringComparison.Ordinal);
        Assert.Contains("vpp-transient-surface--center", orderCreate, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthoredMotion_UsesCanonicalTokensWithoutDuplicateKeyframesOrLayoutRipple()
    {
        var root = GetFrontendRoot();
        var cssRoot = Path.Combine(root, "wwwroot", "css");
        var authoredCss = Directory.EnumerateFiles(cssRoot, "*.css", SearchOption.AllDirectories).ToArray();
        var authoredJavaScript = Directory.EnumerateFiles(Path.Combine(root, "wwwroot", "js"), "*.js", SearchOption.AllDirectories)
            .ToArray();

        var transitionAll = authoredCss
            .Where(path => File.ReadAllText(path).Contains("transition: all", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
        Assert.True(
            transitionAll.Length == 0,
            $"Use explicit paint/compositor properties instead of transition: all: {string.Join(", ", transitionAll)}");

        var keyframeOwners = authoredCss
            .SelectMany(path => System.Text.RegularExpressions.Regex.Matches(
                    File.ReadAllText(path),
                    "@keyframes\\s+([a-zA-Z0-9_-]+)",
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                .Select(match => new
                {
                    Name = match.Groups[1].Value,
                    Path = Path.GetRelativePath(root, path)
                }))
            .GroupBy(owner => owner.Name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key}: {string.Join(", ", group.Select(owner => owner.Path))}")
            .ToArray();
        Assert.True(
            keyframeOwners.Length == 0,
            $"Each project-owned keyframe needs one canonical owner: {string.Join("; ", keyframeOwners)}");

        var script = string.Join("\n", authoredJavaScript.Select(File.ReadAllText));
        Assert.DoesNotContain("void surface.offsetWidth", script, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-pressing", script, StringComparison.Ordinal);
        Assert.DoesNotContain("startViewTransition", script, StringComparison.Ordinal);

        var tokens = File.ReadAllText(Path.Combine(cssRoot, "vpp-tokens.css"));
        var bridge = File.ReadAllText(Path.Combine(cssRoot, "vpp-radzen-theme.css"));
        var polish = File.ReadAllText(Path.Combine(cssRoot, "vpp-polish.css"));
        Assert.Contains("--vpp-motion-instant-duration: 80ms;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-fast-duration: 120ms;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-base-duration: 180ms;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-layout-duration: 220ms;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-skeleton-duration: 1200ms;", tokens, StringComparison.Ordinal);
        Assert.Contains("--vpp-motion-spinner-duration: 900ms;", tokens, StringComparison.Ordinal);
        Assert.Contains(".rz-dropdown-panel", bridge, StringComparison.Ordinal);
        Assert.Contains("@media (prefers-reduced-motion: reduce)", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("scroll-behavior: smooth", polish, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OrderDetailComposite_UsesTypedVariantsAndKeepsRouteActionsOutside()
    {
        var root = GetFrontendRoot();
        var compositesRoot = Path.Combine(root, "Components", "DesignSystem", "Composites");
        var surface = File.ReadAllText(Path.Combine(compositesRoot, "VppOrderItemsSurface.razor"));
        var variant = File.ReadAllText(Path.Combine(compositesRoot, "VppOrderItemsSurfaceVariant.cs"));
        var item = File.ReadAllText(Path.Combine(compositesRoot, "VppOrderDetailItem.cs"));
        var orderPanel = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "VppOrderWorkspacePanel.razor"));
        var historyDrawer = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderDetailSheet.razor"));

        Assert.Contains("VppOrderItemsSurfaceVariant.Workspace", orderPanel, StringComparison.Ordinal);
        Assert.True(
            orderPanel.IndexOf("Loc[\"History\"]", StringComparison.Ordinal)
            < orderPanel.IndexOf("Loc[\"Edit\"]", StringComparison.Ordinal),
            "Các hành động xem/xuất phải đứng trước thao tác thay đổi đơn.");
        Assert.True(
            orderPanel.IndexOf("Loc[\"RestoreOrder\"]", StringComparison.Ordinal)
            < orderPanel.IndexOf("PrimaryActionText", StringComparison.Ordinal),
            "Primary action phải nằm cuối cụm hành động của đơn.");
        Assert.Contains("VppOrderItemsSurfaceVariant.HistoryDrawer", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("Workspace", variant, StringComparison.Ordinal);
        Assert.Contains("HistoryDrawer", variant, StringComparison.Ordinal);
        Assert.Contains("public sealed record VppOrderDetailItem", item, StringComparison.Ordinal);
        Assert.Equal(6, surface.Split("<RadzenDataGridColumn", StringSplitOptions.None).Length - 1);
        Assert.Contains("VppContentStateKind.FilteredEmpty", surface, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"false\"", surface, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ClientSnapshotPaged", surface, StringComparison.Ordinal);
        Assert.Contains("CodeToggled", surface, StringComparison.Ordinal);
        Assert.Contains("ActiveCodeNumber == item.Number", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("ActiveCode == item.Code", surface, StringComparison.Ordinal);
        Assert.Contains("NoteToggled", surface, StringComparison.Ordinal);
        Assert.Contains("SearchChanged", surface, StringComparison.Ordinal);
        Assert.Contains("Opening=\"@TransientSurfacesClosed\"", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("FilterMenuToggled", surface, StringComparison.Ordinal);
        Assert.Contains("CategorySelected", surface, StringComparison.Ordinal);
        Assert.Contains("UnitSelected", surface, StringComparison.Ordinal);
        Assert.Contains("FiltersCleared", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-history-detail-toolbar", orderPanel, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-history-detail-toolbar", historyDrawer, StringComparison.Ordinal);
        Assert.DoesNotContain("HistoryRequested", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("vpp-history-sheet-kicker", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("SubmittedAtLabel", historyDrawer, StringComparison.Ordinal);
        Assert.DoesNotContain("ApiServices", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("Config.", surface, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelRequested", surface, StringComparison.Ordinal);
        Assert.Contains("CancelRequested", orderPanel, StringComparison.Ordinal);
        Assert.Contains("ExportRequested", historyDrawer, StringComparison.Ordinal);
    }

    private static void AssertPatternConsumer(string root, string first, params string[] pathAndMarker)
    {
        var marker = pathAndMarker[^1];
        var path = Path.Combine(new[] { root, "Components", first }.Concat(pathAndMarker[..^1]).ToArray());
        Assert.Contains(marker, File.ReadAllText(path), StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatedShell_AvoidsDecorativeRefreshRevealAndForcedReflow()
    {
        var root = GetFrontendRoot();
        var app = File.ReadAllText(Path.Combine(root, "Components", "App.razor"));
        var polishStyles = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-polish.css"));
        var interactions = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));

        Assert.DoesNotContain("vpp-page-entering", app, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-shell-enter-inline", polishStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-shell-enter-block", polishStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-content-enter", polishStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("function settlePageEntry()", interactions, StringComparison.Ordinal);
        Assert.DoesNotContain("void surface.offsetWidth", interactions, StringComparison.Ordinal);
    }

    [Fact]
    public void HistoryRoute_UsesAlignedPagedSummaryAndBoundedDetailDrawer()
    {
        var root = GetFrontendRoot();
        var history = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor"));
        var historyCode = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.cs"));
        var historyWorkspaceCode = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "HistoryOrderWorkspaceTabBase.cs"));
        var historyStyles = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Components", "HistoryWorkspaceShell.razor.css"));
        var historyScript = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.js"));
        // Sau C-7, markup từng vùng của màn Lịch sử nằm trong các component con presentational.
        var historyComponentsRoot = Path.Combine(root, "Components", "Pages", "VPPRequest", "Components");
        var historyKpis = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryKpiCards.razor"));
        var historyChart = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryTrendChart.razor"));
        var historyOrders = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryOrderList.razor"));
        var historyDrawer = File.ReadAllText(Path.Combine(historyComponentsRoot, "HistoryOrderDetailSheet.razor"));
        var orderItemsSurface = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor"));
        var orderItemsStyles = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor.css"));
        var orderItemsScriptPath = Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor.js");
        var cellValueScript = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppCellValuePopover.razor.js"));
        var clearFiltersButton = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Primitives", "VppClearFiltersButton.razor"));
        var clearFiltersStyles = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Primitives", "VppClearFiltersButton.razor.css"));

        Assert.Contains("vpp-history-kpis", historyKpis, StringComparison.Ordinal);
        Assert.Contains("vpp-history-loading-state", history, StringComparison.Ordinal);
        Assert.Contains("vpp-history-region-loading", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-grid-loading", historyOrders, StringComparison.Ordinal);
        Assert.Contains("vpp-history-chart-refresh", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-refresh", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-no-selection", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("HistoryChartNoData", historyChart, StringComparison.Ordinal);
        Assert.Contains("Summary.Periods.Count > 0 && (ShowRegularSeries || ShowAdditionalSeries)", historyChart, StringComparison.Ordinal);
        Assert.Contains("<VppClearFiltersButton", historyOrders, StringComparison.Ordinal);
        Assert.Contains("<VppClearFiltersButton", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-clear", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("VppIcons.FilterOff", clearFiltersButton, StringComparison.Ordinal);
        Assert.Contains("height: 32px;", clearFiltersStyles, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--vpp-data-footer-height);", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-history-chart-legend-label", historyChart, StringComparison.Ordinal);
        Assert.Contains("Property=\"Note\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("ToggleDetailNote", history, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"#\" Width=\"42px\"", historyOrders, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"#\" Width=\"42px\"", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("VppContentState State=\"VppContentStateKind.Error\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("RadzenStackedColumnSeries", historyChart, StringComparison.Ordinal);
        Assert.Contains("vpp-history-drawer", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("VppOrderItemsSurfaceVariant.HistoryDrawer", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"false\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.SmallStatic", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("vpp-order-grid-scrollable", orderItemsSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-history-detail-grid-header", orderItemsSurface, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpandMode=", history, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpandMode=", historyOrders, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpandMode=", historyDrawer, StringComparison.Ordinal);
        Assert.Contains("PagerAlwaysVisible=\"true\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("OrderHistoryScope.Own", historyCode, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.SplitList.DefaultPageSize", historyWorkspaceCode, StringComparison.Ordinal);
        Assert.DoesNotContain("SetHistoryViewport", historyWorkspaceCode, StringComparison.Ordinal);
        Assert.Contains("HasGridLoadError", history, StringComparison.Ordinal);
        Assert.Contains("_detailError", historyWorkspaceCode, StringComparison.Ordinal);
        Assert.Contains("ClearDetailFiltersAsync", historyWorkspaceCode, StringComparison.Ordinal);
        Assert.Contains("--vpp-history-inline-pill-height: 22px;", historyStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("::deep .vpp-history-detail-toolbar", historyStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("::deep .vpp-history-detail-grid", historyStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("::deep .vpp-history-detail-empty", historyStyles, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-items-surface ::deep .vpp-history-detail-empty", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-history-detail-note-column", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("::deep .vpp-cell-value-popover-copy .vpp-icon", orderItemsStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-history-popover-copy", orderItemsStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject IJSRuntime", orderItemsSurface, StringComparison.Ordinal);
        Assert.False(File.Exists(orderItemsScriptPath));
        Assert.Contains("font-size: 14px;", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("grid-template-rows: auto auto auto minmax(0, 1fr);", historyStyles, StringComparison.Ordinal);
        Assert.Contains("vpp-history-skeleton", historyStyles, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", historyStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("has-vertical-overflow", orderItemsStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("--vpp-history-detail-scrollbar-width", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("::deep .vpp-order-items-grid thead", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("position: sticky;", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("@media (min-width: 1600px)", historyStyles, StringComparison.Ordinal);
        Assert.Contains("grid-row: 2 / 5;", historyStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("text-box: trim-both cap alphabetic;", historyStyles, StringComparison.Ordinal);
        Assert.Contains("overflow-x: hidden;", orderItemsStyles, StringComparison.Ordinal);
        Assert.Contains("text-overflow: ellipsis;", orderItemsStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("new ResizeObserver(() => {", historyScript, StringComparison.Ordinal);
        Assert.DoesNotContain("updateHistoryScrollGutters", historyScript, StringComparison.Ordinal);
        Assert.DoesNotContain("has-vertical-overflow", historyScript, StringComparison.Ordinal);
        Assert.Contains("observeCellValuePopover", cellValueScript, StringComparison.Ordinal);
        Assert.Contains("positionPanel", cellValueScript, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-history-code-popover", historyScript, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-history-note-popover", historyScript, StringComparison.Ordinal);
        Assert.Contains("renderHistoryChartLabels", historyScript, StringComparison.Ordinal);
        Assert.Contains("panel.style.setProperty('max-width'", cellValueScript, StringComparison.Ordinal);
        Assert.Contains("notation: 'compact'", historyScript, StringComparison.Ordinal);
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
        var notifications = File.ReadAllText(Path.Combine(root, "Components", "Layout", "NotificationCenter.razor"));
        var pageMarkup = string.Join("\n", Directory.EnumerateFiles(
            Path.Combine(root, "Components", "Pages"),
            "*.razor",
            SearchOption.AllDirectories).Select(File.ReadAllText));

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
        Assert.DoesNotContain("<VppEmptyState", pageMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppStatePanel", pageMarkup, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Components", "Shared", "EmptyState.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Components", "Shared", "VppEmptyState.razor")));
        Assert.False(File.Exists(Path.Combine(root, "Components", "Shared", "VppStatePanel.razor")));
        Assert.Contains("tabindex=\"-1\"", notifications, StringComparison.Ordinal);
        Assert.Contains("_focusPanelOnRender = true;", notifications, StringComparison.Ordinal);
        Assert.Contains("_focusTriggerOnRender = true;", notifications, StringComparison.Ordinal);
        Assert.Contains("await TryFocusAsync(_panel);", notifications, StringComparison.Ordinal);
        Assert.Contains("await TryFocusAsync(_trigger);", notifications, StringComparison.Ordinal);
        Assert.Contains("catch (JSDisconnectedException", notifications, StringComparison.Ordinal);
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
        var orderItemsSurface = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor"));
        var libraryGrid = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_ItemLibrary.razor"));
        var report = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Report.razor"));

        Assert.Contains("data-vpp-grid-region=\"true\"", orderItemsSurface, StringComparison.Ordinal);
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
    public void InteractionRuntime_CoalescesHighFrequencyWorkAndDisposesRouteObservers()
    {
        var root = GetFrontendRoot();
        var interactions = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "vpp-interactions.js"));
        var history = File.ReadAllText(Path.Combine(
            root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.js"));
        var cellPopover = File.ReadAllText(Path.Combine(
            root, "Components", "DesignSystem", "Composites", "VppCellValuePopover.razor.js"));
        var periodPicker = File.ReadAllText(Path.Combine(
            root, "Components", "DesignSystem", "Composites", "VppPeriodPickerPopover.razor.js"));

        Assert.Contains("scheduleTabIndicatorSync", interactions, StringComparison.Ordinal);
        Assert.Contains("vppSidebarSyncFrame", interactions, StringComparison.Ordinal);
        Assert.Contains("scheduleInteractionTreeFlush", interactions, StringComparison.Ordinal);
        Assert.Contains("removeEventListener(\"scroll\", tabList.vppIndicatorScrollHandler)", interactions, StringComparison.Ordinal);
        Assert.Contains("removeEventListener(\"scroll\", nav.vppSidebarScrollHandler)", interactions, StringComparison.Ordinal);
        Assert.DoesNotContain("new MutationObserver(function (records)", interactions, StringComparison.Ordinal);

        Assert.Contains("scheduleHistoryTransientSurfaces", history, StringComparison.Ordinal);
        Assert.Contains("cancelAnimationFrame(positionFrame)", history, StringComparison.Ordinal);
        Assert.Contains("schedulePositionPanel", cellPopover, StringComparison.Ordinal);
        Assert.Contains("cancelAnimationFrame(frame)", cellPopover, StringComparison.Ordinal);
        Assert.Contains("if (positionFrame) return;", periodPicker, StringComparison.Ordinal);
        Assert.Contains("cancelAnimationFrame(positionFrame)", periodPicker, StringComparison.Ordinal);
    }

    [Fact]
    public void Ds4AdminConsumers_UseCanonicalServerPagedDataSurfaces()
    {
        var root = GetFrontendRoot();
        var categories = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_CategoryLibrary.razor"));
        var items = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_ItemLibrary.razor"));
        var suppliers = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_SupplierLibrary.razor"));
        var departments = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_DepartmentLibrary.razor"));
        var lookup = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_LookupLibrary.razor"));
        var price = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_PriceLibrary.razor"));
        var priceList = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Lib", "Tabs", "Tab_PriceListLibrary.razor"));
        var users = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Permission", "Tabs", "Tab_User.razor"));
        var permissions = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Permission", "Tabs", "Tab_PagePermission.razor"));
        var securityAudit = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Permission", "Tabs", "Tab_SecurityAudit.razor"));
        var collectionHeader = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppCollectionHeader.razor"));
        var dataSurfaceFrame = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppDataSurfaceFrame.razor"));
        var report = File.ReadAllText(Path.Combine(root, "Components", "Pages", "Report.razor"));

        foreach (var source in new[] { categories, items, suppliers, departments, lookup, price, priceList, users, permissions, securityAudit })
        {
            Assert.Contains("VppDataSourceMode.ServerPaging", source, StringComparison.Ordinal);
            Assert.Contains("VppDataDensity.Compact", source, StringComparison.Ordinal);
            Assert.Contains("<VppDataToolbar", source, StringComparison.Ordinal);
            Assert.Contains("vpp-data-grid vpp-data-density-compact", source, StringComparison.Ordinal);
            Assert.Contains("AllowFiltering=\"false\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("FilterMode=\"FilterMode.CheckBoxList\"", source, StringComparison.Ordinal);
            Assert.DoesNotContain("LoadColumnFilterData=", source, StringComparison.Ordinal);
        }

        var adminCodeBehindSources = Directory
            .EnumerateFiles(Path.Combine(root, "Components", "Pages"), "Tab_*.razor.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Lib{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           || path.Contains($"{Path.DirectorySeparatorChar}Permission{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(File.ReadAllText);
        foreach (var source in adminCodeBehindSources)
        {
            Assert.DoesNotContain("DataGridLoadColumnFilterDataEventArgs", source, StringComparison.Ordinal);
            Assert.DoesNotContain("distinctFilter=", source, StringComparison.Ordinal);
            Assert.DoesNotContain("args.Filter", source, StringComparison.Ordinal);
        }

        Assert.Equal(2, lookup.Split("<VppDataSurfaceFrame", StringSplitOptions.None).Length - 1);
        Assert.Contains("Property=\"IsDeleted\"", lookup, StringComparison.Ordinal);
        Assert.Equal(2, lookup.Split("<VppCollectionHeader", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("vpp-admin-is-deleted-cell", lookup, StringComparison.Ordinal);
        Assert.Contains("data-vpp-collection-header", collectionHeader, StringComparison.Ordinal);
        Assert.Contains("vpp-collection-header-add", collectionHeader, StringComparison.Ordinal);
        Assert.Contains("RenderFragment? Header", dataSurfaceFrame, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppActionColumnHeader.razor")));

        foreach (var source in new[] { categories, items, suppliers, departments, lookup, price, priceList, users, permissions, securityAudit })
        {
            var toolbarStart = source.IndexOf("<Toolbar>", StringComparison.Ordinal);
            while (toolbarStart >= 0)
            {
                var toolbarEnd = source.IndexOf("</Toolbar>", toolbarStart, StringComparison.Ordinal);
                Assert.True(toolbarEnd > toolbarStart, "Admin toolbar markup must be balanced.");
                var toolbar = source[toolbarStart..toolbarEnd];
                Assert.DoesNotContain("<RadzenButton", toolbar, StringComparison.Ordinal);
                toolbarStart = source.IndexOf("<Toolbar>", toolbarEnd, StringComparison.Ordinal);
            }
        }

        foreach (var createConsumer in new[] { categories, items, suppliers, departments, lookup, priceList, users })
        {
            Assert.Contains("<VppCollectionHeader", createConsumer, StringComparison.Ordinal);
            Assert.DoesNotContain("<VppActionColumnHeader", createConsumer, StringComparison.Ordinal);
        }

        foreach (var libraryGrid in new[] { categories, items, suppliers, departments, lookup, price, priceList })
        {
            Assert.Contains("AllowAlternatingRows=\"false\"", libraryGrid, StringComparison.Ordinal);
        }

        Assert.Contains("OpenPermissionEditorForGroupAsync", permissions, StringComparison.Ordinal);
        Assert.DoesNotContain("class=\"vpp-library-primary-action\"", permissions, StringComparison.Ordinal);
        Assert.Contains("permission-users-data-surface", users, StringComparison.Ordinal);
        Assert.Contains("permission-groups-data-surface", permissions, StringComparison.Ordinal);
        Assert.Contains("security-audit-data-surface", securityAudit, StringComparison.Ordinal);
        Assert.Equal(2, report.Split("data-vpp-grid-region=\"true\"", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, report.Split("vpp-data-density-compact", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("rzi-person_search", users, StringComparison.Ordinal);
        Assert.DoesNotContain("rzi-price_change", priceList, StringComparison.Ordinal);
        Assert.DoesNotContain("rzi-verified_user", report, StringComparison.Ordinal);
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

    [Fact]
    public void SystemRoutes_UseTypedContentStatesAndNeverRenderABlankLoginTransition()
    {
        var root = GetFrontendRoot();
        var pagesRoot = Path.Combine(root, "Components", "Pages");
        var home = File.ReadAllText(Path.Combine(pagesRoot, "Home.razor"));
        var error = File.ReadAllText(Path.Combine(pagesRoot, "Error.razor"));
        var notFound = File.ReadAllText(Path.Combine(pagesRoot, "NotFound.razor"));
        var loginProcess = File.ReadAllText(Path.Combine(pagesRoot, "Authen", "Login.razor"));
        var routes = File.ReadAllText(Path.Combine(root, "Components", "Routes.razor"));
        var mainLayout = File.ReadAllText(Path.Combine(root, "Components", "Layout", "MainLayout.razor"));
        var loginLayout = File.ReadAllText(Path.Combine(root, "Components", "Layout", "LoginLayout.razor"));

        Assert.Contains("VppContentStateKind.Loading", home, StringComparison.Ordinal);
        Assert.Contains("RedirectingToAllowedWorkspace", home, StringComparison.Ordinal);
        Assert.Contains("<VppAccountWorkspace", loginProcess, StringComparison.Ordinal);
        Assert.Contains("LoginRedirectMessage", loginProcess, StringComparison.Ordinal);
        Assert.Contains("BackToLogin", loginProcess, StringComparison.Ordinal);
        Assert.Contains("VppContentStateKind.Error", error, StringComparison.Ordinal);
        Assert.Contains("PrimaryAction=\"@Reload\"", error, StringComparison.Ordinal);
        Assert.Contains("VppContentStateKind.Empty", notFound, StringComparison.Ordinal);
        Assert.Contains("VppContentStateKind.Denied", routes, StringComparison.Ordinal);
        Assert.Contains("VppContentStateKind.Error", mainLayout, StringComparison.Ordinal);
        Assert.Contains("VppContentStateKind.Error", loginLayout, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppStatePanel", string.Join("\n", home, error, notFound, routes, mainLayout, loginLayout), StringComparison.Ordinal);
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
