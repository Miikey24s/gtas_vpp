using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class UiMotifCatalogTests
{
    [Fact]
    public void EveryLogicalRouteHasExactlyOneTypedUiProfile()
    {
        var routeKeys = RouteCatalog.Authenticated
            .Concat(RouteCatalog.Anonymous)
            .Select(route => route.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var profileKeys = UiRouteCatalog.Profiles.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(routeKeys, profileKeys);
        Assert.Equal(routeKeys.Count, UiRouteCatalog.Profiles.Count);
    }

    [Fact]
    public void ProfilesDeclareCompositionMotifsAndStateCoverage()
    {
        Assert.All(UiRouteCatalog.Profiles.Values, profile =>
        {
            Assert.False(string.IsNullOrWhiteSpace(profile.RouteKey));
            Assert.NotEmpty(profile.Motifs);
            Assert.NotEmpty(profile.States);
            Assert.Contains(profile.Motifs, motif => motif is "SHELL-NAV" or "ACCOUNT" or "COLLECTION" or "LIST-DETAIL" or "SPLIT-EDITOR" or "OPERATION" or "ANALYTICS" or "CONTENT-STATE");
        });
    }

    [Fact]
    public void MotifAuthorityIsLinkedFromTheUiSkillAndExecutionDocs()
    {
        var root = FindRepositoryRoot();
        var skill = File.ReadAllText(Path.Combine(root, ".agents", "skills", "gtas-vpp-ui-system", "SKILL.md"));
        var execution = File.ReadAllText(Path.Combine(root, "docs", "execution", "UI-SYSTEM-001.md"));
        var catalogPath = Path.Combine(root, "docs", "design", "VPP-UI-MOTIF-CATALOG.md");

        Assert.True(File.Exists(catalogPath));
        Assert.Contains("VPP-UI-MOTIF-CATALOG.md", skill, StringComparison.Ordinal);
        Assert.Contains("VPP-UI-MOTIF-CATALOG.md", execution, StringComparison.Ordinal);
    }

    [Fact]
    public void StableCapabilitySurfaceContractIsRecordedForFutureRefactorWaves()
    {
        var root = FindRepositoryRoot();
        var catalog = File.ReadAllText(Path.Combine(root, "docs", "design", "VPP-UI-MOTIF-CATALOG.md"));
        var ledger = File.ReadAllText(Path.Combine(root, "docs", "design", "VPP-DATA-SURFACE-CONSUMER-LEDGER.md"));
        var refactor = File.ReadAllText(Path.Combine(root, "docs", "execution", "FRONTEND-REFACTOR-001.md"));
        var skill = File.ReadAllText(Path.Combine(root, ".agents", "skills", "gtas-vpp-ui-system", "SKILL.md"));

        Assert.Contains("`CAPABILITY-SURFACE`", catalog, StringComparison.Ordinal);
        Assert.Contains("Action chưa đủ điều kiện nghiệp vụ vẫn hiện nhưng ở trạng thái disabled", catalog, StringComparison.Ordinal);
        Assert.Contains("Filtered-empty giữ filter", catalog, StringComparison.Ordinal);
        Assert.Contains("Stable Capability Surface retrofit queue", ledger, StringComparison.Ordinal);
        Assert.Contains("FR9 — Stable Capability Surface retrofit", refactor, StringComparison.Ordinal);
        Assert.Contains("CAPABILITY-SURFACE", skill, StringComparison.Ordinal);
    }

    [Fact]
    public void DataSurfaceOrderContractIsRecordedInTheCatalogAndConsumerLedger()
    {
        var root = FindRepositoryRoot();
        var catalog = File.ReadAllText(Path.Combine(root, "docs", "design", "VPP-UI-MOTIF-CATALOG.md"));
        var ledger = File.ReadAllText(Path.Combine(root, "docs", "design", "VPP-DATA-SURFACE-CONSUMER-LEDGER.md"));

        Assert.Contains("`DATA-SURFACE-ORDER`", catalog, StringComparison.Ordinal);
        Assert.Contains("Tìm kiếm → filter theo thứ tự cột từ trái sang phải", catalog, StringComparison.Ordinal);
        Assert.Contains("Dialog footer", catalog, StringComparison.Ordinal);
        Assert.Contains("Filter, column và action order matrix", ledger, StringComparison.Ordinal);
        Assert.Contains("Kỳ đặt hàng | Năm → Trạng thái", ledger, StringComparison.Ordinal);
        Assert.Contains("Người dùng | Trạng thái → Nhóm quyền → Phòng ban", ledger, StringComparison.Ordinal);
    }

    [Fact]
    public void StableCapabilitySurface_IsImplementedBySharedComponentsAndRepresentativeRoutes()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var actionItem = File.ReadAllText(Path.Combine(frontend, "Components", "DesignSystem", "Composites", "VppAdminActionMenuItem.cs"));
        var actionMenu = File.ReadAllText(Path.Combine(frontend, "Components", "DesignSystem", "Composites", "VppAdminActionMenu.razor"));
        var actionMenuJs = File.ReadAllText(Path.Combine(frontend, "Components", "DesignSystem", "Composites", "VppAdminActionMenu.razor.js"));
        var dataFrame = File.ReadAllText(Path.Combine(frontend, "Components", "DesignSystem", "Composites", "VppDataSurfaceFrame.razor"));
        var dataGridCss = File.ReadAllText(Path.Combine(frontend, "wwwroot", "css", "vpp-datagrid.css"));
        var periods = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "OrderPeriodManagementWorkspace.razor.cs"));
        var users = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "Permission", "Tabs", "Tab_User.razor"));
        var approvals = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "PendingApprovalWorkspace.razor"));
        var orders = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "VppOrderWorkspacePanel.razor"));
        var history = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"));
        var catalogRoute = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Tabs", "Tab_ProductCatalog.razor"));
        var priceLists = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "Lib", "Tabs", "Tab_PriceListLibrary.razor"));
        var lookup = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "Lib", "Tabs", "Tab_LookupLibrary.razor"));

        Assert.Contains("string? DisabledReason", actionItem, StringComparison.Ordinal);
        Assert.Contains("Items.Select(item => item.Disabled ? item.DisabledReason : null)", actionMenu, StringComparison.Ordinal);
        Assert.Contains("applyDisabledReasons", actionMenuJs, StringComparison.Ordinal);
        Assert.Contains("data-vpp-capability-surface=\"true\"", dataFrame, StringComparison.Ordinal);
        Assert.Contains("VppDataSurfaceState State", dataFrame, StringComparison.Ordinal);
        Assert.Contains(".rz-datatable-empty", dataGridCss, StringComparison.Ordinal);
        Assert.Contains("Disabled: !period.CanExtendDeadline", periods, StringComparison.Ordinal);
        Assert.DoesNotContain("Disabled: !period.CanEditSchedule", periods, StringComparison.Ordinal);
        Assert.Contains("!period.CanRestore : !period.CanDeactivate", periods, StringComparison.Ordinal);
        Assert.Contains("Disabled: !period.CanHardDelete", periods, StringComparison.Ordinal);
        Assert.Contains("Visible=\"@CanManageUsers\"", users, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(!SelectedOrder.CanApproveSupplement", approvals, StringComparison.Ordinal);
        Assert.Contains("ShowEditAction", orders, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(!CanEdit)\"", orders, StringComparison.Ordinal);
        Assert.Contains("<VppDataGridEmptyState", history, StringComparison.Ordinal);
        Assert.Contains("State=\"@CatalogSurfaceState\"", catalogRoute, StringComparison.Ordinal);
        Assert.Contains("<VppDataGridEmptyState", catalogRoute, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyText=", priceLists, StringComparison.Ordinal);
        Assert.DoesNotContain("EmptyText=", lookup, StringComparison.Ordinal);
    }

    [Fact]
    public void StableCapabilitySurface_DataFramesDeclareTypedStateAndDataGridsKeepEmptyTemplates()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var componentRoot = Path.Combine(frontend, "Components");

        var frameOffenders = Directory
            .EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => file.Source.Contains("<VppDataSurfaceFrame", StringComparison.Ordinal))
            .SelectMany(file => System.Text.RegularExpressions.Regex
                .Matches(file.Source, @"<VppDataSurfaceFrame[\s\S]*?>")
                .Select((match, index) => new
                {
                    File = Path.GetRelativePath(frontend, file.Path),
                    Index = index + 1,
                    Markup = match.Value
                }))
            .Where(frame => !frame.Markup.Contains(" State=", StringComparison.Ordinal))
            .Select(frame => $"{frame.File}#{frame.Index}")
            .ToArray();

        Assert.True(
            frameOffenders.Length == 0,
            $"Every data frame must expose its typed surface state: {string.Join(", ", frameOffenders)}");

        var excludedDataGridConsumers = new[]
        {
            Path.Combine("Components", "DesignSystem", "Composites", "VppColumnPicker.razor"),
            Path.Combine("Components", "Pages", "VPPRequest", "Tabs", "Tab_DepartmentSummary.razor")
        };
        var gridOffenders = Directory
            .EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !excludedDataGridConsumers.Contains(Path.GetRelativePath(frontend, path), StringComparer.OrdinalIgnoreCase))
            .Select(path => new { Path = path, Source = File.ReadAllText(path) })
            .Where(file => file.Source.Contains("<RadzenDataGrid", StringComparison.Ordinal)
                && !file.Source.Contains("<EmptyTemplate>", StringComparison.Ordinal))
            .Select(file => Path.GetRelativePath(frontend, file.Path))
            .ToArray();

        Assert.True(
            gridOffenders.Length == 0,
            $"Every authored data grid must keep a canonical empty template: {string.Join(", ", gridOffenders)}");

        var report = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "Report.razor"));
        var periodSettings = File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "Permission", "Tabs", "Tab_OrderPeriodSettings.razor"));
        Assert.Contains("<RadzenDataGrid TItem=\"ReportDepartmentPointResDTO\"", report, StringComparison.Ordinal);
        Assert.Contains("<RadzenDataGrid TItem=\"ReportProductPointResDTO\"", report, StringComparison.Ordinal);
        Assert.Contains("<RadzenDataGrid TItem=\"VppOrderPeriodSettingsResDTO\"", periodSettings, StringComparison.Ordinal);
        Assert.DoesNotContain("@if (FilteredDepartmentBreakdown.Count == 0)", report, StringComparison.Ordinal);
        Assert.DoesNotContain("@if (History.Count == 0)", periodSettings, StringComparison.Ordinal);
    }

    [Fact]
    public void RetiredUiAdaptersAndLegacySelectorsCannotReturn()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var retiredFiles = new[]
        {
            Path.Combine(frontend, "wwwroot", "app.css"),
            Path.Combine(frontend, "wwwroot", "css", "vpp-status-badge.css"),
            Path.Combine(frontend, "Components", "Shared", "KpiCard.razor"),
            Path.Combine(frontend, "Components", "Shared", "KpiVariant.cs"),
            Path.Combine(frontend, "Components", "Shared", "StatusBadge.razor"),
            Path.Combine(frontend, "Components", "Shared", "VppAccountShell.razor"),
            Path.Combine(frontend, "Components", "Shared", "VppMobileCard.razor"),
            Path.Combine(frontend, "Helpers", "StatusDisplayRadzen.cs")
        };

        Assert.All(retiredFiles, path => Assert.False(File.Exists(path), $"Retired UI file returned: {path}"));

        var authoredSource = string.Join("\n", Directory
            .EnumerateFiles(frontend, "*.*", SearchOption.AllDirectories)
            .Where(path => new[] { ".razor", ".cs", ".css", ".js" }.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(File.ReadAllText));

        foreach (var retiredIdentifier in new[]
                 {
                     "librariestab",
                     "VppAccountShell",
                     "VppMobileCard",
                     "KpiVariant",
                     "StatusDisplayRadzen",
                     "GetCssClass(",
                     "<RadzenBadge",
                     "<RadzenAlert",
                     "BadgeStyle.",
                     "class=\"kpi-card",
                     "class=\"vpp-badge",
                     "class=\"shimmer"
                 })
        {
            Assert.DoesNotContain(retiredIdentifier, authoredSource, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PeriodLifecycleBadge_IsCanonicalAndUsedByRealConsumers()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var badge = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "DesignSystem",
            "Composites",
            "VppPeriodStateBadge.razor"));

        Assert.Contains("<VppStatusBadge", badge, StringComparison.Ordinal);
        Assert.Contains("PeriodStateDisplay.GetTone", badge, StringComparison.Ordinal);

        var categoryChip = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "DesignSystem",
            "Composites",
            "VppCategoryChip.razor"));
        Assert.Contains("VppCategoryTone.Primary", categoryChip, StringComparison.Ordinal);
        Assert.Contains("VppCategoryTone.Accent", categoryChip, StringComparison.Ordinal);
        Assert.Contains("Muted=\"@Muted\"", categoryChip, StringComparison.Ordinal);
        Assert.Contains("Emphasized=\"@Emphasized\"", categoryChip, StringComparison.Ordinal);

        var statusBadge = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "DesignSystem",
            "Primitives",
            "VppStatusBadge.razor"));
        Assert.Contains("[Parameter] public bool Muted", statusBadge, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public bool Emphasized", statusBadge, StringComparison.Ordinal);

        var statusBadgeCss = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "DesignSystem",
            "Primitives",
            "VppStatusBadge.razor.css"));
        Assert.Contains(".vpp-status-badge.is-muted", statusBadgeCss, StringComparison.Ordinal);
        Assert.Contains(".vpp-status-badge.is-emphasized", statusBadgeCss, StringComparison.Ordinal);

        var toneContract = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "DesignSystem",
            "Primitives",
            "VppStatusToneContract.cs"));
        Assert.Contains("\"ACTIVE\" or \"OPEN\" or \"SCHEDULED\" or \"SUBMITTED\"", toneContract, StringComparison.Ordinal);
        Assert.Contains("\"SETTLED\"", toneContract, StringComparison.Ordinal);
        Assert.Contains("VppStatusTone.Success", toneContract, StringComparison.Ordinal);

        foreach (var consumer in new[]
                 {
                     Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor"),
                     Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Tabs", "Tab_DepartmentSummary.razor"),
                     Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "OrderPeriodManagementWorkspace.razor")
                 })
        {
            Assert.Contains("<VppPeriodStateBadge", File.ReadAllText(consumer), StringComparison.Ordinal);
        }

        Assert.Contains(
            "<VppCategoryChip",
            File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "VPPRequest", "Components", "HistoryOrderList.razor")),
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "vpp-permission-matrix-badge",
            File.ReadAllText(Path.Combine(frontend, "Components", "Pages", "Permission", "Tabs", "Tab_PagePermission.razor")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void OrderPeriodManagement_UsesCanonicalAdminCollectionMotif()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var workspace = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "Pages",
            "VPPRequest",
            "Components",
            "OrderPeriodManagementWorkspace.razor"));
        var profile = UiRouteCatalog.Get("dashboard.period.periods");

        Assert.Equal(UiRouteCatalog.WorkspacePattern.Collection, profile.Workspace);
        Assert.Contains("COLLECTION-HEADER", profile.Motifs);
        Assert.Contains("ADMIN-ROW-ACTIONS", profile.Motifs);
        Assert.Contains("<VppCollectionHeader", workspace, StringComparison.Ordinal);
        Assert.Contains("<RadzenDataGrid", workspace, StringComparison.Ordinal);
        Assert.Contains("Text=\"Xem\"", workspace, StringComparison.Ordinal);
        Assert.Contains("Text=\"Chốt kỳ\"", workspace, StringComparison.Ordinal);
        Assert.Contains("Click=\"@(() => NavigateToSettlement(row))\"", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowOpenWhenAllDisabled=\"true\"", workspace, StringComparison.Ordinal);
        Assert.Contains("<VppAdminActionMenu", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppAdminActiveToggle", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppListDetailWorkspace", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("post-settlement-corrections", workspace, StringComparison.Ordinal);

        var settlement = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "Pages",
            "VPPRequest",
            "Components",
            "PeriodSettlementPanel.razor"));
        Assert.Contains("pendingPostSettlementCorrections.Count > 0", settlement, StringComparison.Ordinal);
        Assert.Contains("TestId=\"post-settlement-corrections\"", settlement, StringComparison.Ordinal);
        Assert.Contains("<RadzenDataGrid TItem=\"PostSettlementOrderCorrectionResDTO\"", settlement, StringComparison.Ordinal);
        Assert.Contains("<VppAdminIconAction", settlement, StringComparison.Ordinal);
        Assert.Contains("<VppDecisionSelect TValue=\"Guid?\"", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("<table class=\"vpp-settlement-correction-table\"", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-post-settlement-backdrop", settlement, StringComparison.Ordinal);

        var correctionDialog = File.ReadAllText(Path.Combine(
            frontend,
            "Components",
            "Pages",
            "VPPRequest",
            "Components",
            "Dialog_PostSettlementOrderCorrection.razor"));
        Assert.Contains("<VppAdaptiveDialogShell", correctionDialog, StringComparison.Ordinal);
        Assert.Contains("<VppDialogActions", correctionDialog, StringComparison.Ordinal);
        Assert.Contains("<VppDecisionSelect TValue=\"string\"", correctionDialog, StringComparison.Ordinal);
        Assert.Contains("RemoveFromOrder", correctionDialog, StringComparison.Ordinal);
        Assert.Contains("Items.Where(item => !item.IsRemoved)", correctionDialog, StringComparison.Ordinal);
        Assert.Contains("ActiveItemCount > 0", correctionDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("<select", correctionDialog, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdaptiveDialogAndPageSizeMotion_KeepCanonicalLifecycleContracts()
    {
        var root = FindRepositoryRoot();
        var frontend = Path.Combine(root, "src", "Frontend", "Blazor");
        var adminCss = File.ReadAllText(Path.Combine(frontend, "wwwroot", "css", "vpp-admin.css"));
        var radzenCss = File.ReadAllText(Path.Combine(frontend, "wwwroot", "css", "vpp-radzen-theme.css"));

        Assert.Contains("grid-template-areas:", adminCss, StringComparison.Ordinal);
        Assert.Contains("grid-area: body;", adminCss, StringComparison.Ordinal);
        Assert.Contains("grid-area: footer;", adminCss, StringComparison.Ordinal);
        Assert.Contains("--rz-on-primary: var(--vpp-text-on-action);", radzenCss, StringComparison.Ordinal);
        Assert.Contains(
            ".rz-dropdown-panel.vpp-page-size-panel.rz-open.vpp-transient-surface--above",
            radzenCss,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            ".rz-dropdown-panel.vpp-page-size-panel.vpp-transient-surface--above {",
            radzenCss,
            StringComparison.Ordinal);
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
