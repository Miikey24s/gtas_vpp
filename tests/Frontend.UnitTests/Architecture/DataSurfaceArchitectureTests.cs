using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class DataSurfaceArchitectureTests
{
    [Fact]
    public void SharedFoundation_IsTypedComposableAndRouteAgnostic()
    {
        var root = GetFrontendRoot();
        var componentRoot = Path.Combine(root, "Components", "DesignSystem", "Composites");
        var contracts = Read(componentRoot, "VppDataSurfaceContracts.cs");
        var frame = Read(componentRoot, "VppDataSurfaceFrame.razor");
        var toolbar = Read(componentRoot, "VppDataToolbar.razor");
        var footer = Read(componentRoot, "VppDataSummaryFooter.razor");
        var cellPopover = Read(componentRoot, "VppCellValuePopover.razor");
        var cellPopoverStyles = Read(componentRoot, "VppCellValuePopover.razor.css");
        var combined = string.Join('\n', frame, toolbar, footer, cellPopover);

        foreach (var contract in new[]
                 {
                     "VppDataSourceMode", "ServerPaging", "ClientSnapshotPaged", "ClientSnapshotVirtualized",
                     "ServerVirtualizedPrefetch", "VppDataDensity", "Compact", "RichTwoLine",
                     "VppDataFooterMode", "VppCellValueKind"
                 })
        {
            Assert.Contains(contract, contracts, StringComparison.Ordinal);
        }

        Assert.Contains("RenderFragment? Toolbar", frame, StringComparison.Ordinal);
        Assert.Contains("RenderFragment ChildContent", frame, StringComparison.Ordinal);
        Assert.Contains("RenderFragment? Footer", frame, StringComparison.Ordinal);
        Assert.Contains("data-vpp-data-source-mode", frame, StringComparison.Ordinal);
        Assert.Contains("data-vpp-data-density", frame, StringComparison.Ordinal);
        Assert.Contains("role=\"group\"", toolbar, StringComparison.Ordinal);
        Assert.Contains("data-vpp-data-footer-mode", footer, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", footer, StringComparison.Ordinal);
        Assert.Contains("vpp-transient-surface", cellPopover, StringComparison.Ordinal);
        Assert.Contains("VppCellValueKind Kind", cellPopover, StringComparison.Ordinal);
        Assert.Contains("outline: 0;", cellPopoverStyles, StringComparison.Ordinal);
        Assert.Contains("box-shadow: inset 0 0 0 1px var(--vpp-border-focus);", cellPopoverStyles, StringComparison.Ordinal);

        foreach (var forbidden in new[] { "ApiServices", "HttpClient", "LoadData=", "System.Reflection", "/api/" })
        {
            Assert.DoesNotContain(forbidden, combined, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void TokensAndRadzenBridge_AreSemanticAndOptIn()
    {
        var root = GetFrontendRoot();
        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var bridge = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-radzen-theme.css"));

        foreach (var token in new[]
                 {
                     "--vpp-data-control-height: 32px;",
                     "--vpp-data-toolbar-height: 42px;",
                     "--vpp-data-header-height: 40px;",
                     "--vpp-data-row-compact-height: 40px;",
                     "--vpp-data-row-rich-two-line-height: 52px;",
                     "--vpp-data-footer-height: 42px;"
                 })
        {
            Assert.Contains(token, tokens, StringComparison.Ordinal);
        }

        Assert.Contains(".vpp-data-grid.rz-data-grid", bridge, StringComparison.Ordinal);
        Assert.Contains("[data-vpp-data-surface=\"true\"] .vpp-data-grid.rz-data-grid", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-grid-border-radius: 0;", bridge, StringComparison.Ordinal);
        Assert.Contains(".rz-paginator .rz-dropdown", bridge, StringComparison.Ordinal);
        Assert.Contains(".rz-dropdown-panel :is(.rz-dropdown-item, .rz-dropdown-items > li).rz-state-highlight", bridge, StringComparison.Ordinal);
        Assert.Contains(".vpp-data-grid.vpp-data-density-compact", bridge, StringComparison.Ordinal);
        Assert.Contains(".vpp-data-grid.vpp-data-density-rich-two-line", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("body .rz-data-grid", bridge, StringComparison.Ordinal);
    }

    [Fact]
    public void RepresentativeConsumers_KeepServerAndClientPagingBehaviorDistinct()
    {
        var root = GetFrontendRoot();
        var history = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var orderItems = File.ReadAllText(Path.Combine(root, "Components", "DesignSystem", "Composites", "VppOrderItemsSurface.razor"));

        Assert.Contains("VppDataSourceMode.ServerPaging", history, StringComparison.Ordinal);
        Assert.Contains("<VppDataToolbar", history, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"true\"", history, StringComparison.Ordinal);
        Assert.Contains("LoadData=\"@LoadRequested\"", history, StringComparison.Ordinal);
        Assert.Contains("vpp-data-grid vpp-data-density-compact", history, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(history, "<VppCellValuePopover\\b").Count);

        Assert.Contains("VppDataSourceMode.ClientSnapshotPaged", orderItems, StringComparison.Ordinal);
        Assert.Contains("<Toolbar>", orderItems, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"@UsePaging\"", orderItems, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"false\"", orderItems, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.SmallStatic", orderItems, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadData=", orderItems, StringComparison.Ordinal);
        Assert.Contains("vpp-data-density-rich-two-line", orderItems, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(orderItems, "<VppCellValuePopover\\b").Count);
    }

    [Fact]
    public void ReferenceConsumers_UseCanonicalFiltersAndTypedFramesWithoutLegacyMenus()
    {
        var root = GetFrontendRoot();
        var history = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var historyCode = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_History.razor.cs"));
        var historyStyles = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryWorkspaceShell.razor.css"));
        var catalog = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_ProductCatalog.razor"));

        Assert.Contains("<Toolbar>", history, StringComparison.Ordinal);
        Assert.Contains("<VppFilterSearch", history, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(history, "<VppFilterSelect\\b").Count);
        Assert.DoesNotContain("vpp-history-select-menu", history, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenFilterMenu", history, StringComparison.Ordinal);
        Assert.DoesNotContain("_openFilterMenu", historyCode, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-history-select-menu", historyStyles, StringComparison.Ordinal);

        Assert.Contains("TestId=\"catalog-data-surface\"", catalog, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ServerPaging", catalog, StringComparison.Ordinal);
        Assert.Contains("VppDataDensity.RichTwoLine", catalog, StringComparison.Ordinal);
        Assert.Contains("vpp-data-grid vpp-data-density-rich-two-line", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowConsumers_UseCanonicalChromeAndKeepTheirDataBehavior()
    {
        var root = GetFrontendRoot();
        var create = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "OrderCreateStep2.razor"));
        var department = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_DepartmentSummary.razor"));
        var sharedOrderList = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var review = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "PeriodReviewPanel.razor"));

        Assert.Contains("TestId=\"order-create-catalog-data-surface\"", create, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ClientSnapshotPaged", create, StringComparison.Ordinal);
        Assert.Contains("<VppDataToolbar", create, StringComparison.Ordinal);
        Assert.Contains("<VppCellValuePopover", create, StringComparison.Ordinal);
        Assert.Contains("<RadzenPager", create, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.LargeWorkingSet", create, StringComparison.Ordinal);
        Assert.Contains("VisibleProductOptions", create, StringComparison.Ordinal);
        Assert.DoesNotContain("<RadzenDataGrid", create, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadData=", create, StringComparison.Ordinal);

        Assert.Contains("TestId=\"department-summary-data-surface\"", department, StringComparison.Ordinal);
        Assert.Contains("<HistoryWorkspaceShell", department, StringComparison.Ordinal);
        Assert.Contains("<HistoryOrderList", department, StringComparison.Ordinal);
        Assert.Contains("Property=\"RequesterName\"", department, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ServerPaging", sharedOrderList, StringComparison.Ordinal);
        Assert.Contains("<VppDataToolbar", sharedOrderList, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(sharedOrderList, "<VppFilterSelect\\b").Count);
        Assert.Contains("AllowPaging=\"true\"", sharedOrderList, StringComparison.Ordinal);
        Assert.Contains("LoadData=\"@LoadRequested\"", sharedOrderList, StringComparison.Ordinal);

        Assert.Contains("TestId=\"period-review-data-surface\"", review, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ServerPaging", review, StringComparison.Ordinal);
        Assert.Contains("<VppDataToolbar", review, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(review, "<VppFilterSelect\\b").Count);
        Assert.Contains("AllowPaging=\"true\"", review, StringComparison.Ordinal);
        Assert.Contains("LoadData=\"@OnLoadData\"", review, StringComparison.Ordinal);
    }

    [Fact]
    public void ConsumerLedger_CoversEveryRadzenDataGridFileAndCount()
    {
        var repositoryRoot = FindRepositoryRoot();
        var frontendRoot = Path.Combine(repositoryRoot, "src", "Frontend", "Blazor");
        var componentRoot = Path.Combine(frontendRoot, "Components");
        var ledger = File.ReadAllText(Path.Combine(repositoryRoot, "docs", "design", "VPP-DATA-SURFACE-CONSUMER-LEDGER.md"));
        var gridPattern = new Regex("<RadzenDataGrid(?=\\s|>)", RegexOptions.CultureInvariant);
        var consumers = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Select(path => new { Path = path, Count = gridPattern.Matches(File.ReadAllText(path)).Count })
            .Where(consumer => consumer.Count > 0)
            .OrderBy(consumer => consumer.Path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(18, consumers.Length);
        Assert.Equal(23, consumers.Sum(consumer => consumer.Count));

        foreach (var consumer in consumers)
        {
            var relative = Path.GetRelativePath(frontendRoot, consumer.Path).Replace('\\', '/');
            Assert.Contains($"| `{relative}` | {consumer.Count} |", ledger, StringComparison.Ordinal);
        }
    }

    private static string Read(string root, string fileName) => File.ReadAllText(Path.Combine(root, fileName));

    private static string GetFrontendRoot() => Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");

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
