using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class UiConsolidationArchitectureTests
{
    [Fact]
    public void DialogAndAdminActions_UseCanonicalComposites()
    {
        var root = GetFrontendRoot();
        var componentRoot = Path.Combine(root, "Components");
        var dialogActions = Read(componentRoot, "DesignSystem", "Composites", "VppDialogActions.razor");
        var iconAction = Read(componentRoot, "DesignSystem", "Composites", "VppAdminIconAction.razor");
        var activeToggle = Read(componentRoot, "DesignSystem", "Composites", "VppAdminActiveToggle.razor");

        Assert.Contains("PrimaryButtonStyle", dialogActions, StringComparison.Ordinal);
        Assert.Contains("vpp-admin-icon-action", iconAction, StringComparison.Ordinal);
        Assert.Contains("@onclick:stopPropagation=\"true\"", iconAction, StringComparison.Ordinal);
        Assert.Contains("RadzenSwitch", activeToggle, StringComparison.Ordinal);

        var duplicateDialogFooters = Directory.EnumerateFiles(componentRoot, "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("VppDialogActions.razor", StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("class=\"vpp-adaptive-dialog-actions\"", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
        Assert.True(
            duplicateDialogFooters.Length == 0,
            $"Dialog footer must use VppDialogActions: {string.Join(", ", duplicateDialogFooters)}");

        var adminTabs = new[]
        {
            "Tab_CategoryLibrary.razor", "Tab_SupplierLibrary.razor", "Tab_DepartmentLibrary.razor",
            "Tab_ItemLibrary.razor", "Tab_LookupLibrary.razor", "Tab_PriceLibrary.razor",
            "Tab_PriceListLibrary.razor", "Tab_User.razor", "Tab_PagePermission.razor"
        };
        foreach (var fileName in adminTabs)
        {
            var source = Directory.EnumerateFiles(componentRoot, fileName, SearchOption.AllDirectories).Single();
            Assert.Contains("VppAdmin", File.ReadAllText(source), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void FileDownloadAndSettlementCorrection_HaveOneRuntimeOwner()
    {
        var root = GetFrontendRoot();
        var browserPlatformRoot = Path.Combine(root, "Platform", "Browser");
        var downloadServicePath = Path.Combine(browserPlatformRoot, "BrowserFileDownloadService.cs");
        var downloadService = File.ReadAllText(downloadServicePath);

        Assert.Contains("namespace gtas_vpp_fe.Platform.Browser;", downloadService, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(root, "Services", "BrowserFileDownloadService.cs")));
        Assert.Equal(1, downloadService.Split("vppDownload.fromStream", StringSplitOptions.None).Length - 1);
        Assert.Contains("DotNetStreamReference", downloadService, StringComparison.Ordinal);
        Assert.Contains("file.ContentType", downloadService, StringComparison.Ordinal);
        Assert.Contains("BrowserFileDownloadResult", downloadService, StringComparison.Ordinal);

        var exportActions = Read(root, "Components", "DesignSystem", "Composites", "VppFileExportActions.razor");
        Assert.Contains("VppFileExportFormat", exportActions, StringComparison.Ordinal);
        Assert.Contains("BusyFormat", exportActions, StringComparison.Ordinal);

        var duplicatePipelines = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !Path.GetFullPath(path).Equals(Path.GetFullPath(downloadServicePath), StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("DotNetStreamReference", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
        Assert.True(
            duplicatePipelines.Length == 0,
            $"Browser downloads must use IBrowserFileDownloadService: {string.Join(", ", duplicatePipelines)}");

        var duplicateExportActions = Directory.EnumerateFiles(Path.Combine(root, "Components"), "*.razor", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("VppFileExportActions.razor", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("Loc[\"ExportPdf\"]", StringComparison.Ordinal)
                    || source.Contains("Loc[\"ExportExcel\"]", StringComparison.Ordinal)
                    || source.Contains("Loc[\"ExportCsv\"]", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
        Assert.True(
            duplicateExportActions.Length == 0,
            $"Export actions must use VppFileExportActions: {string.Join(", ", duplicateExportActions)}");

        var settlement = Read(root, "Components", "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor");
        var settlementCode = Read(root, "Components", "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor.cs");
        var correctionDialog = Read(root, "Components", "Pages", "VPPRequest", "Components", "Dialog_SettlementCorrection.razor");
        Assert.DoesNotContain("vpp-settlement-correction-dialog", settlement, StringComparison.Ordinal);
        Assert.Contains("OpenAsync<Dialog_SettlementCorrection>", settlementCode, StringComparison.Ordinal);
        Assert.Contains("VppAdaptiveDialogShell", correctionDialog, StringComparison.Ordinal);
        Assert.Contains("VppDialogActions", correctionDialog, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectAndPagerChrome_HaveOneCssAuthority()
    {
        var root = GetFrontendRoot();
        var bridge = Read(root, "wwwroot", "css", "vpp-radzen-theme.css");
        var polish = Read(root, "wwwroot", "css", "vpp-polish.css");
        var report = Read(root, "Components", "Pages", "Report.razor");

        Assert.Contains(".rz-paginator .rz-dropdown", bridge, StringComparison.Ordinal);
        Assert.Contains(".rz-dropdown-panel", bridge, StringComparison.Ordinal);
        Assert.DoesNotContain("\n.rz-dropdown {", polish, StringComparison.Ordinal);
        Assert.DoesNotContain("<RadzenDropDown", report, StringComparison.Ordinal);
        Assert.Equal(3, report.Split("<VppFilterSelect", StringSplitOptions.None).Length - 1);

        var pagedGridOffenders = Directory.EnumerateFiles(Path.Combine(root, "Components"), "*.razor", SearchOption.AllDirectories)
            .SelectMany(path => System.Text.RegularExpressions.Regex.Matches(
                    File.ReadAllText(path),
                    "(?s)<RadzenDataGrid\\b.*?</RadzenDataGrid>")
                .Cast<System.Text.RegularExpressions.Match>()
                .Where(match => match.Value.Contains("AllowPaging=\"true\"", StringComparison.Ordinal)
                    && !match.Value.Contains("PagerHorizontalAlign=\"HorizontalAlign.Right\"", StringComparison.Ordinal))
                .Select(_ => Path.GetRelativePath(root, path)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.True(
            pagedGridOffenders.Length == 0,
            $"Paged grids must align the canonical footer to the right: {string.Join(", ", pagedGridOffenders)}");
    }

    [Fact]
    public void PricingAndReports_UseCanonicalWorkspaceContracts()
    {
        var root = GetFrontendRoot();
        var priceList = Read(root, "Components", "Pages", "Lib", "Tabs", "Tab_PriceListLibrary.razor");
        var priceListCode = Read(root, "Components", "Pages", "Lib", "Tabs", "Tab_PriceListLibrary.razor.cs");
        var prices = Read(root, "Components", "Pages", "Lib", "Tabs", "Tab_PriceLibrary.razor");
        var pricesCode = Read(root, "Components", "Pages", "Lib", "Tabs", "Tab_PriceLibrary.razor.cs");
        var pricingClient = Read(root, "Features", "CatalogPricing", "Api", "PricingApiClient.cs");
        var report = Read(root, "Components", "Pages", "Report.razor");
        var reportCode = Read(root, "Components", "Pages", "Report.razor.cs");
        var adminCss = Read(root, "wwwroot", "css", "vpp-admin.css");
        var layoutCss = Read(root, "wwwroot", "css", "vpp-layout.css");

        Assert.Contains("ContextMenuService.Open", priceListCode, StringComparison.Ordinal);
        Assert.Contains("pricingTab=prices", priceListCode, StringComparison.Ordinal);
        Assert.DoesNotContain("/library?tab=4", priceListCode, StringComparison.Ordinal);
        Assert.DoesNotContain("rz-col-actions-xwide", priceList, StringComparison.Ordinal);
        Assert.DoesNotContain("rz-col-actions-xwide", adminCss, StringComparison.Ordinal);

        Assert.Contains("data-testid=\"price-context\"", prices, StringComparison.Ordinal);
        Assert.DoesNotContain("OnSupplierChangedAsync", prices, StringComparison.Ordinal);
        Assert.Contains("PricingApi.GetItemPriceCategoriesAsync", pricesCode, StringComparison.Ordinal);
        Assert.Contains("PricingApi.GetItemPricesAsync", pricesCode, StringComparison.Ordinal);
        Assert.Contains("distinct=CategoryName", pricingClient, StringComparison.Ordinal);
        Assert.Contains("MappingStatus switch", pricingClient, StringComparison.Ordinal);
        Assert.Contains("VppAdminActiveToggle", prices, StringComparison.Ordinal);
        Assert.Contains("!row.IsDeleted || !IsSelectedPriceListDraft", prices, StringComparison.Ordinal);

        Assert.Contains("<VppAnalyticsWorkspace", report, StringComparison.Ordinal);
        Assert.Equal(2, report.Split("<VppDataSurfaceFrame", StringSplitOptions.None).Length - 1);
        Assert.Equal(2, report.Split("<VppDataSummaryFooter", StringSplitOptions.None).Length - 1);
        Assert.Contains("<details class=\"vpp-report-insight\"", report, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"height:", report, StringComparison.Ordinal);
        Assert.DoesNotContain("#0EA5E9", reportCode, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-report-filter-controls", layoutCss, StringComparison.Ordinal);
    }

    [Fact]
    public void ToastFeedback_HasOneInjectedRuntimeService()
    {
        var root = GetFrontendRoot();
        var service = Read(root, "Services", "ToastService.cs");
        Assert.Contains("public sealed class ToastService(NotificationService notificationService)", service, StringComparison.Ordinal);

        var directConsumers = Directory.EnumerateFiles(Path.Combine(root, "Components"), "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("@inject NotificationService", StringComparison.Ordinal)
                    || source.Contains("[Inject] private NotificationService", StringComparison.Ordinal)
                    || source.Contains("[Inject] protected NotificationService", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();
        Assert.True(
            directConsumers.Length == 0,
            $"Components must depend on IToastService: {string.Join(", ", directConsumers)}");
    }

    [Fact]
    public void SettlementExports_AreVisibleInCanonicalCollectionHeader()
    {
        var root = GetFrontendRoot();
        var settlement = Read(root, "Components", "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor");
        var settlementCode = Read(root, "Components", "Pages", "VPPRequest", "Components", "PeriodSettlementPanel.razor.cs");
        var settlementClient = Read(root, "Features", "Settlement", "Api", "SettlementApiClient.cs");

        Assert.Contains("<VppCollectionHeader", settlement, StringComparison.Ordinal);
        Assert.Contains("<Actions>", settlement, StringComparison.Ordinal);
        Assert.Contains("<VppStatusBadge", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementStatusText", settlement, StringComparison.Ordinal);
        Assert.Contains("@if (CanExportSettlement)", settlement, StringComparison.Ordinal);
        Assert.Contains("VppFileExportActions", settlement, StringComparison.Ordinal);
        Assert.Contains("status is { IsSettled: true, SettlementId: not null }", settlementCode, StringComparison.Ordinal);
        Assert.Contains("exportingSettlementFormat.HasValue || !CanExportSettlement", settlementCode, StringComparison.Ordinal);
        Assert.Contains("Settlement.ExportAsync", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IBrowserFileDownloadService", settlementCode, StringComparison.Ordinal);
        Assert.Contains("IBrowserFileDownloadService", settlementClient, StringComparison.Ordinal);
        Assert.Contains("export.pdf", settlementClient, StringComparison.Ordinal);
        Assert.Contains("export.xlsx", settlementClient, StringComparison.Ordinal);
    }

    private static string Read(string root, params string[] parts)
        => File.ReadAllText(Path.Combine([root, .. parts]));

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

        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
