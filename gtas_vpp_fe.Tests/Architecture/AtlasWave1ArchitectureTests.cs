using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class AtlasWave1ArchitectureTests
{
    [Theory]
    [InlineData("Components/Pages/Lib/Component_ShareGrid.razor")]
    [InlineData("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor")]
    [InlineData("Components/Pages/Permission/Tabs/Tab_User.razor")]
    public void AdministrativeCollections_ExposeAResponsiveInspector(string relativePath)
    {
        var source = ReadFrontendSource(relativePath);

        Assert.Contains("vpp-atlas-admin-workspace", source, StringComparison.Ordinal);
        Assert.Contains("Component_RecordInspector", source, StringComparison.Ordinal);
        Assert.Contains("DataGridSelectionMode.Single", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ItemGrid_KeepsLocalizationAndPricingEvidenceInTheInspectorByDefault()
    {
        var source = ReadFrontendSource("Components/Pages/Lib/Component_ShareGrid.razor.cs");

        Assert.Contains("IsInspectorFirstProperty", source, StringComparison.Ordinal);
        Assert.Contains("OriginalLanguageCode", source, StringComparison.Ordinal);
        Assert.Contains("DefaultSupplierName", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PeriodOperations_UsePreviewBeforeConfirmation()
    {
        var host = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor");
        var review = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodReviewPanel.razor");
        var settlement = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor");

        Assert.Contains("<PeriodSettlementPanel", host, StringComparison.Ordinal);
        // Copy đã chuyển sang resx (W-D): khóa qua key SettleQuotesHeading thay vì chuỗi cứng.
        Assert.Contains("Loc[\"SettleQuotesHeading\"]", settlement, StringComparison.Ordinal);
        Assert.Contains("preview.Blockers", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("Click=\"@SettleAsync\"", review, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_ShowSettlementEvidenceOnlyWhenTheApiProvidesIt()
    {
        var report = ReadFrontendSource("Components/Pages/Report.razor");

        Assert.Contains("Summary.SettlementId.HasValue", report, StringComparison.Ordinal);
        Assert.Contains("vpp-report-settlement-evidence", report, StringComparison.Ordinal);
    }

    [Fact]
    public void M6_UserAdministration_UsesAtlasCopyAndNeverRendersSessionVersion()
    {
        var users = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_User.razor");
        var userCode = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_User.razor.cs");
        var inspector = ReadFrontendSource("Components/Pages/Lib/Component_RecordInspector.razor");

        Assert.Contains("Loc[\"UserSearchPlaceholder\"]", users, StringComparison.Ordinal);
        Assert.Contains("Loc[\"AllAccountStatuses\"]", users, StringComparison.Ordinal);
        Assert.Contains("Loc[\"PermissionGroup\"]", users, StringComparison.Ordinal);
        Assert.Contains("accountStatus=", userCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Reset mật khẩu", users, StringComparison.Ordinal);
        Assert.DoesNotContain("Membership updated", userCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Deactivate membership", userCode, StringComparison.Ordinal);
        Assert.Contains("IsSensitiveProperty(prop.Name)", inspector, StringComparison.Ordinal);
        Assert.Contains("name is \"SessionVersion\"", inspector, StringComparison.Ordinal);
        Assert.DoesNotContain("name is \"Id\" or \"RowVersion\" or \"SessionVersion\"", inspector, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductCatalog_LoadsItsFirstPageBeforeTheGridDependsOnItsOwnCount()
    {
        var catalog = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor.cs");

        Assert.Contains("await LoadProductsAsync(new LoadDataArgs", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("productGrid.Reload()", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void M0_UsesOneGlobalInteractiveServerTree()
    {
        var app = ReadFrontendSource("Components/App.razor");

        Assert.Contains("<HeadOutlet @rendermode=\"InteractiveServer\"", app, StringComparison.Ordinal);
        Assert.Contains("<Routes @rendermode=\"InteractiveServer\"", app, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/Pages/Authen/LoginPage.razor")]
    [InlineData("Components/Pages/Authen/Register.razor")]
    [InlineData("Components/Pages/Authen/ForgotPassword.razor")]
    [InlineData("Components/Pages/Authen/ResetPassword.razor")]
    [InlineData("Components/Pages/Authen/ChangePassword.razor")]
    [InlineData("Components/Pages/Authen/Logout.razor")]
    public void M1_AccountRoutesShareTheCanonicalAccountShell(string relativePath)
    {
        var source = ReadFrontendSource(relativePath);

        Assert.Contains("<VppAccountShell", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_CatalogKeepsFiltersInTheHeaderAndUsesServerPaging()
    {
        var catalog = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor");
        var catalogCode = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor.cs");

        var headerStart = catalog.IndexOf("<header class=\"vpp-catalog-card-header\">", StringComparison.Ordinal);
        var headerEnd = catalog.IndexOf("</header>", headerStart, StringComparison.Ordinal);
        var filterStart = catalog.IndexOf("vpp-catalog-filter-group", StringComparison.Ordinal);

        Assert.True(headerStart >= 0 && headerEnd > headerStart && filterStart > headerStart && filterStart < headerEnd);
        Assert.Contains("AllowPaging=\"true\"", catalog, StringComparison.Ordinal);
        Assert.Contains("Count=\"@ProductCount\"", catalog, StringComparison.Ordinal);
        Assert.Contains("LoadData=\"@LoadProductsAsync\"", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("DownloadCatalog", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowCatalogDownloadNotice", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"@Loc[\"Status\"]\"", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_OrderCreateUsesCenteredTwoStepHeaderAndAtlasFooterActions()
    {
        var page = ReadFrontendSource("Components/Pages/VPPRequest/Page_OrderCreate.razor");
        var selection = ReadFrontendSource("Components/Pages/VPPRequest/OrderCreateStep2.razor");

        Assert.Contains("vpp-order-flow-steps", page, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-flow-back", page, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-save-draft", page, StringComparison.Ordinal);
        Assert.Contains("BackRequested=\"@GoBack\"", page, StringComparison.Ordinal);
        Assert.Contains("SaveDraftRequested", page, StringComparison.Ordinal);
        Assert.Contains("vpp-order-action-back", selection, StringComparison.Ordinal);
        Assert.Contains("vpp-order-action-save", selection, StringComparison.Ordinal);
        Assert.Contains("vpp-order-action-note", selection, StringComparison.Ordinal);
        Assert.Contains("popovertarget=\"@GetItemNotePopoverId(item)\"", selection, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"true\"", selection, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"false\"", selection, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_OrderAndHistoryCollectionsKeepTheirDistinctDataContracts()
    {
        var orders = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_Orders.razor");
        // Sau C-7, hai grid của màn Lịch sử nằm trong hai component con thay vì Tab_History.razor.
        var historyOrders = ReadFrontendSource("Components/Pages/VPPRequest/Components/HistoryOrderList.razor");
        var historyDetail = ReadFrontendSource("Components/Pages/VPPRequest/Components/HistoryOrderDetailSheet.razor");
        var detail = ReadFrontendSource("Components/Pages/VPPRequest/Components/VppOrderWorkspacePanel.razor");

        Assert.Contains("CurrentOrderViewIndex", orders, StringComparison.Ordinal);
        Assert.Contains("SupplementOrderViewIndex", orders, StringComparison.Ordinal);
        Assert.Contains("PreviousOrderViewIndex", orders, StringComparison.Ordinal);
        Assert.DoesNotContain("export-pdf-coming-soon", orders, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"true\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"true\"", historyDetail, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"false\"", detail, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"true\"", detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Aspire_KeepsThePausedReactPreviewOptIn()
    {
        var appHost = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "MyAspire.AppHost", "AppHost.cs"));

        Assert.Contains("Frontend:EnableReactPreview", appHost, StringComparison.Ordinal);
        Assert.Contains("if (reactPreviewEnabled)", appHost, StringComparison.Ordinal);
        Assert.Contains("AddViteApp", appHost, StringComparison.Ordinal);
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var projectRoot = Path.Combine(FindRepositoryRoot(), "gtas_vpp_fe", "gtas_vpp_fe", "gtas_vpp_fe");
        return File.ReadAllText(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
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
