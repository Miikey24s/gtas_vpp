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
        var frameStyles = Read(componentRoot, "VppDataSurfaceFrame.razor.css");
        var toolbar = Read(componentRoot, "VppDataToolbar.razor");
        var filterSelect = Read(componentRoot, "VppFilterSelect.razor");
        var filterSelectStyles = Read(componentRoot, "VppFilterSelect.razor.css");
        var decisionSelect = Read(componentRoot, "VppDecisionSelect.razor");
        var decisionOption = Read(componentRoot, "VppDecisionOption.cs");
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
        Assert.Contains(".vpp-data-surface-body ::deep .vpp-content-state", frameStyles, StringComparison.Ordinal);
        Assert.Contains("height: 100%;", frameStyles, StringComparison.Ordinal);
        Assert.Contains("data-vpp-data-density", frame, StringComparison.Ordinal);
        Assert.Contains("role=\"group\"", toolbar, StringComparison.Ordinal);
        Assert.Contains("? \"true\" : \"false\"", filterSelect, StringComparison.Ordinal);
        Assert.Contains("HighlightSelectedOption", filterSelect, StringComparison.Ordinal);
        Assert.Contains("has-neutral-selection", filterSelectStyles, StringComparison.Ordinal);
        Assert.Contains("role=\"listbox\"", decisionSelect, StringComparison.Ordinal);
        Assert.Contains("VppDecisionOption<TValue>", decisionSelect, StringComparison.Ordinal);
        Assert.Contains("record VppDecisionOption<TValue>", decisionOption, StringComparison.Ordinal);
        Assert.DoesNotContain("VppFilterOption", decisionSelect, StringComparison.Ordinal);
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
    public void OrderPeriodSelection_UsesOneTypedDecisionControlPerContext()
    {
        var root = GetFrontendRoot();
        var orders = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_Orders.razor"));
        var create = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Page_OrderCreate.razor"));

        Assert.Contains("<VppDecisionSelect TValue=\"Guid?\"", orders, StringComparison.Ordinal);
        Assert.Contains("<VppDecisionSelect TValue=\"Guid?\"", create, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", orders, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<select", create, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vpp-orders-story-heading", orders, StringComparison.Ordinal);
    }

    [Fact]
    public void TokensAndRadzenBridge_AreSemanticAndOptIn()
    {
        var root = GetFrontendRoot();
        var tokens = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-tokens.css"));
        var bridge = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-radzen-theme.css"));
        var dataGrid = File.ReadAllText(Path.Combine(root, "wwwroot", "css", "vpp-datagrid.css"));

        foreach (var token in new[]
                 {
                     "--vpp-data-control-height: 32px;",
                     "--vpp-data-toolbar-height: 42px;",
                     "--vpp-data-header-height: 40px;",
                     "--vpp-data-row-compact-height: 40px;",
                     "--vpp-data-row-rich-two-line-height: 52px;",
                     "--vpp-data-footer-height: 42px;",
                     "--vpp-data-grid-min-width: 100%;"
                 })
        {
            Assert.Contains(token, tokens, StringComparison.Ordinal);
        }

        Assert.Contains(".vpp-data-grid.rz-data-grid", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-root-font-size: var(--vpp-root-font-size);", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-grid-cell-font-size: var(--vpp-text-base);", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-grid-header-font-size: var(--vpp-text-sm);", bridge, StringComparison.Ordinal);
        Assert.Contains("[data-vpp-data-surface=\"true\"] .vpp-data-grid.rz-data-grid", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-grid-border-radius: 0;", bridge, StringComparison.Ordinal);
        Assert.Contains("--rz-grid-data-border-shadow: none;", bridge, StringComparison.Ordinal);
        Assert.Contains("> .vpp-data-surface-body > .vpp-data-grid.rz-data-grid", bridge, StringComparison.Ordinal);
        Assert.Contains("border-bottom-color: transparent;", bridge, StringComparison.Ordinal);
        Assert.Contains("z-index: 2;", bridge, StringComparison.Ordinal);
        Assert.Contains("--vpp-data-grid-min-width", bridge, StringComparison.Ordinal);
        Assert.Contains("overflow: auto;", bridge, StringComparison.Ordinal);
        Assert.Contains(".rz-paginator .rz-dropdown", bridge, StringComparison.Ordinal);
        Assert.Contains(".rz-pager .rz-dropdown .rz-dropdown-trigger", bridge, StringComparison.Ordinal);
        Assert.Contains("display: none;", bridge, StringComparison.Ordinal);
        Assert.Contains(".rz-dropdown-panel :is(.rz-dropdown-item, .rz-dropdown-items > li).rz-state-highlight", bridge, StringComparison.Ordinal);
        Assert.Contains(".vpp-data-grid.vpp-data-density-compact", bridge, StringComparison.Ordinal);
        Assert.Contains(".vpp-data-grid.vpp-data-density-rich-two-line", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("body .rz-data-grid", bridge, StringComparison.Ordinal);
        Assert.Contains("th:has(.rz-sortable-column-icon):hover", dataGrid, StringComparison.Ordinal);
        Assert.Contains("th:has(.rz-sortable-column-icon):focus-within", dataGrid, StringComparison.Ordinal);
        Assert.Contains("opacity: 0;", dataGrid, StringComparison.Ordinal);
        Assert.Contains(":is(.rzi-sort-asc, .rzi-sort-desc)", dataGrid, StringComparison.Ordinal);
        Assert.Contains(".rz-grid-table tbody > tr:has(.vpp-data-grid-empty-state) .vpp-data-grid-empty-state", dataGrid, StringComparison.Ordinal);
        Assert.Contains("border-radius: 0;", dataGrid, StringComparison.Ordinal);
        Assert.Contains("box-shadow: none;", dataGrid, StringComparison.Ordinal);
        Assert.DoesNotContain(".vpp-order-grid .rz-datatable-tablewrapper,\n.vpp-order-grid .rz-paginator", dataGrid, StringComparison.Ordinal);
        Assert.Contains(".vpp-order-grid .rz-pager,", dataGrid, StringComparison.Ordinal);
    }

    [Fact]
    public void PendingApproval_UsesFullWidthListWhenTheQueueIsEmpty()
    {
        var root = GetFrontendRoot();
        var patterns = Path.Combine(root, "Components", "DesignSystem", "Patterns");
        var listDetail = Read(patterns, "VppListDetailWorkspace.razor");
        var listDetailStyles = Read(patterns, "VppListDetailWorkspace.razor.css");
        var approval = File.ReadAllText(Path.Combine(
            root,
            "Components",
            "Pages",
            "VPPRequest",
            "Components",
            "PendingApprovalWorkspace.razor"));

        Assert.Contains("bool ShowDetail", listDetail, StringComparison.Ordinal);
        Assert.Contains("is-list-only", listDetail, StringComparison.Ordinal);
        Assert.Contains("grid-template-columns: minmax(0, 1fr);", listDetailStyles, StringComparison.Ordinal);
        Assert.Contains("ShowDetail=\"@ShouldShowDetailPane\"", approval, StringComparison.Ordinal);
        Assert.Contains("ShouldShowDetailPane => TotalCount > 0", approval, StringComparison.Ordinal);
    }

    [Fact]
    public void SortableGrids_UseCanonicalSingleColumnContract()
    {
        var root = GetFrontendRoot();
        var componentRoot = Path.Combine(root, "Components");
        var sortableGridCount = 0;

        foreach (var file in Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var cursor = 0;

            while (true)
            {
                var sortIndex = source.IndexOf("AllowSorting=\"true\"", cursor, StringComparison.Ordinal);
                if (sortIndex < 0) break;

                var gridStart = source.LastIndexOf("<RadzenDataGrid", sortIndex, StringComparison.Ordinal);
                var columnsStart = source.IndexOf("<Columns>", sortIndex, StringComparison.Ordinal);
                Assert.True(gridStart >= 0 && columnsStart > sortIndex,
                    $"Không xác định được opening tag của sortable grid trong {file}.");

                var gridContract = source[gridStart..columnsStart];
                Assert.Contains("AllowMultiColumnSorting=\"false\"", gridContract, StringComparison.Ordinal);
                Assert.Contains("ShowMultiColumnSortingIndex=\"false\"", gridContract, StringComparison.Ordinal);
                Assert.Contains("GotoFirstPageOnSort=\"true\"", gridContract, StringComparison.Ordinal);
                Assert.DoesNotContain("AllowMultiColumnSorting=\"true\"", gridContract, StringComparison.Ordinal);
                Assert.DoesNotContain("ShowMultiColumnSortingIndex=\"true\"", gridContract, StringComparison.Ordinal);

                sortableGridCount++;
                cursor = sortIndex + 1;
            }
        }

        Assert.True(sortableGridCount >= 15,
            $"Sort contract phải bao phủ toàn bộ grid hiện tại; chỉ tìm thấy {sortableGridCount} grid.");
    }

    [Fact]
    public void PagedSurfaces_UseCanonicalRightAlignedFooterContract()
    {
        var root = GetFrontendRoot();
        var componentRoot = Path.Combine(root, "Components");
        var pagedGridCount = 0;
        var standalonePagerCount = 0;

        foreach (var file in Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(file);
            var cursor = 0;

            while (true)
            {
                var pagingIndex = source.IndexOf("AllowPaging=\"", cursor, StringComparison.Ordinal);
                if (pagingIndex < 0) break;

                cursor = pagingIndex + 1;
                if (source.AsSpan(pagingIndex).StartsWith("AllowPaging=\"false\"", StringComparison.Ordinal))
                {
                    continue;
                }

                var gridStart = source.LastIndexOf("<RadzenDataGrid", pagingIndex, StringComparison.Ordinal);
                var columnsStart = source.IndexOf("<Columns>", pagingIndex, StringComparison.Ordinal);
                Assert.True(gridStart >= 0 && columnsStart > pagingIndex,
                    $"Không xác định được opening tag của paged grid trong {file}.");

                var gridContract = source[gridStart..columnsStart];
                Assert.Contains("PagerHorizontalAlign=\"HorizontalAlign.Right\"", gridContract, StringComparison.Ordinal);
                pagedGridCount++;
            }

            foreach (Match pager in Regex.Matches(source, "<RadzenPager(?=\\s|>)"))
            {
                var tagEnd = source.IndexOf("/>", pager.Index, StringComparison.Ordinal);
                Assert.True(tagEnd > pager.Index, $"Không xác định được standalone pager trong {file}.");
                var pagerContract = source[pager.Index..(tagEnd + 2)];
                Assert.Contains("HorizontalAlign=\"HorizontalAlign.Right\"", pagerContract, StringComparison.Ordinal);
                standalonePagerCount++;
            }
        }

        Assert.True(pagedGridCount >= 16,
            $"Footer contract phải bao phủ toàn bộ paged grid hiện tại; chỉ tìm thấy {pagedGridCount} grid.");
        Assert.True(standalonePagerCount >= 1,
            "Footer contract phải bao phủ cả standalone pager trong data surface tùy biến.");
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
        var catalogStyles = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_ProductCatalog.razor.css"));

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
        Assert.Contains("Bordered=\"true\"", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("<section class=\"vpp-catalog-card\"", catalog, StringComparison.Ordinal);
        Assert.Contains("vpp-data-grid vpp-data-density-rich-two-line", catalog, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"280px\"", catalog, StringComparison.Ordinal);
        Assert.Contains("--vpp-data-grid-min-width: 956px;", catalogStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("overflow-x: hidden;", catalogStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkflowConsumers_UseCanonicalChromeAndKeepTheirDataBehavior()
    {
        var root = GetFrontendRoot();
        var create = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "OrderCreateStep2.razor"));
        var department = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Tabs", "Tab_DepartmentSummary.razor"));
        var sharedOrderList = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var settlement = File.ReadAllText(Path.Combine(root, "Components", "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor"));

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

        Assert.Contains("TestId=\"period-settlement-data-surface\"", settlement, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ClientSnapshotPaged", settlement, StringComparison.Ordinal);
        Assert.Contains("<VppDataToolbar", settlement, StringComparison.Ordinal);
        Assert.Equal(5, Regex.Matches(settlement, "<VppFilterSelect\\b").Count);
        Assert.Contains("<VppPeriodPickerPopover", settlement, StringComparison.Ordinal);
        Assert.Contains("<VppCollectionHeader", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-settlement-period-target", settlement, StringComparison.Ordinal);
        var periodPicker = File.ReadAllText(Path.Combine(
            GetFrontendRoot(),
            "Components",
            "DesignSystem",
            "Composites",
            "VppPeriodPickerPopover.razor"));
        Assert.Equal(4, periodPicker.Split("<VppFilterSelect TValue=\"int\"", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("input type=\"month\"", periodPicker, StringComparison.Ordinal);
        Assert.Equal(4, Regex.Matches(settlement, "<RadzenDataGrid(?=\\s|>)").Count);
        Assert.Contains("AggregatedVppItemResDTO", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadData=", settlement, StringComparison.Ordinal);
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

        Assert.Equal(21, consumers.Length);
        Assert.Equal(26, consumers.Sum(consumer => consumer.Count));

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
