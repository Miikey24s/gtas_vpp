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
        var layoutCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-layout.css"));
        var responsiveCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-responsive.css"));

        var sidebarStart = source.IndexOf("<RadzenSidebar", StringComparison.Ordinal);
        var brand = source.IndexOf("vpp-sidebar-brand", StringComparison.Ordinal);
        var desktopToggle = source.IndexOf("vpp-sidebar-toggle", StringComparison.Ordinal);

        Assert.True(sidebarStart >= 0 && brand > sidebarStart, "the GTAS VPP brand should live inside the sidebar");
        Assert.True(desktopToggle > sidebarStart, "the desktop expand/collapse control should live inside the sidebar");
        Assert.Contains("vpp-mobile-sidebar-toggle", source, StringComparison.Ordinal);
        Assert.DoesNotContain("VppIcons.Search", source, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout", layoutCss, StringComparison.Ordinal);
        Assert.Contains("\"rz-sidebar rz-header\"", layoutCss, StringComparison.Ordinal);
        Assert.Contains(".rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar", responsiveCss, StringComparison.Ordinal);
    }

    [Fact]
    public void SidebarHover_UsesTheSharedTabInteractionTint()
    {
        var root = GetFrontendRoot();
        var sidebarCss = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-sidebar.css"));

        Assert.Contains("same tint, radius and timing as tabs", sidebarCss, StringComparison.Ordinal);
        Assert.Contains("background-color: rgba(14, 165, 233, 0.04);", sidebarCss, StringComparison.Ordinal);
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
