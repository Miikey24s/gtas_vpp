using gtas_vpp_fe.Components.Layout;
using gtas_vpp_shared.Constants;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class AtlasWave1ArchitectureTests
{
    [Fact]
    public void TypedAdministrativeCollections_RetireTheGenericReflectionGridAndInspector()
    {
        var libraryRoot = Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor", "Components", "Pages", "Lib");

        Assert.False(File.Exists(Path.Combine(libraryRoot, "Component_ShareGrid.razor")));
        Assert.False(File.Exists(Path.Combine(libraryRoot, "Component_ShareGrid.razor.cs")));
        Assert.False(File.Exists(Path.Combine(libraryRoot, "Component_RecordInspector.razor")));
        Assert.False(File.Exists(Path.Combine(
            FindRepositoryRoot(), "src", "Frontend", "Blazor", "Components", "Pages", "Permission", "Tabs", "Component_Loading.razor")));
        Assert.False(File.Exists(Path.Combine(
            FindRepositoryRoot(), "src", "Frontend", "Blazor", "Components", "Shared", "DialogProvider.razor")));

        foreach (var tab in new[]
                 {
                     "Tab_CategoryLibrary.razor",
                     "Tab_ItemLibrary.razor",
                     "Tab_SupplierLibrary.razor",
                     "Tab_DepartmentLibrary.razor"
                 })
        {
            var source = ReadFrontendSource($"Components/Pages/Lib/Tabs/{tab}");
            Assert.Contains("<VppCollectionWorkspace", source, StringComparison.Ordinal);
            Assert.Contains("VppDataSourceMode.ServerPaging", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void UserAdministrationUsesFullWidthCollectionAndPasswordlessInvitationDialogs()
    {
        var page = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_User.razor");
        var code = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_User.razor.cs");
        var invitation = ReadFrontendSource("Components/Pages/Permission/Dialogs/Dialog_UserInvitationEditor.razor");

        Assert.Contains("<VppCollectionWorkspace", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppListDetailWorkspace", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Component_RecordInspector", page, StringComparison.Ordinal);
        Assert.Contains("Loc[\"AddUser\"]", page, StringComparison.Ordinal);
        Assert.Contains("Config.ApiAccountAdminInviteEndpoint", code, StringComparison.Ordinal);
        Assert.Contains("Config.ApiAccountAdminSendPasswordResetLinkEndpoint", code, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateTemporaryPassword", code, StringComparison.Ordinal);
        Assert.Contains("<VppAdaptiveDialogShell", invitation, StringComparison.Ordinal);
        Assert.Contains("Loc[\"UserInvitationPasswordlessHint\"]", invitation, StringComparison.Ordinal);
        Assert.Contains("class=\"vpp-admin-inline-select\"", page, StringComparison.Ordinal);
        Assert.Contains("AllowFiltering=\"false\"", page, StringComparison.Ordinal);
        Assert.Contains(".vpp-data-grid .vpp-admin-inline-select.rz-dropdown", ReadFrontendSource("wwwroot/css/vpp-radzen-theme.css"), StringComparison.Ordinal);
        Assert.Contains(".vpp-data-grid .vpp-admin-inline-select.rz-dropdown:focus-visible", ReadFrontendSource("wwwroot/css/vpp-a11y.css"), StringComparison.Ordinal);
        Assert.Contains("OnGroupAssignmentChangedAsync", page, StringComparison.Ordinal);
        Assert.Contains("OnDepartmentAssignmentChangedAsync", page, StringComparison.Ordinal);
        Assert.Contains("ApproveAccountAsync", page, StringComparison.Ordinal);
        Assert.Contains("ToggleUserAccessAsync", page, StringComparison.Ordinal);
        Assert.Contains("<VppAdminActiveToggle", page, StringComparison.Ordinal);
        Assert.DoesNotContain("manage_accounts", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Icon=\"person_off\"", page, StringComparison.Ordinal);
        Assert.Contains("button,", ReadFrontendSource("wwwroot/css/vpp-radzen-theme.css"), StringComparison.Ordinal);
        Assert.Contains("box-shadow: none !important", ReadFrontendSource("wwwroot/css/vpp-radzen-theme.css"), StringComparison.Ordinal);
    }

    [Fact]
    public void PriceListAdminUsesFullWidthCollectionAndAdaptiveEditorContract()
    {
        var page = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor");
        var editor = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_PriceListEditor.razor");
        var priceEditor = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_PriceEditor.razor");
        var priceCode = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceLibrary.razor.cs");

        Assert.Contains("<VppCollectionWorkspace", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppListDetailWorkspace", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Component_RecordInspector", page, StringComparison.Ordinal);
        Assert.Contains("<VppFilterSelect TValue=\"string\"", page, StringComparison.Ordinal);
        Assert.Contains("<VppAdaptiveDialogShell", editor, StringComparison.Ordinal);
        Assert.Contains("<VppAdaptiveDialogShell", priceEditor, StringComparison.Ordinal);
        Assert.Contains("VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard", priceCode, StringComparison.Ordinal);
        Assert.Contains("data-vpp-admin-dialog-size=\"@SizeCssClass\"", ReadFrontendSource("Components/DesignSystem/Composites/VppAdaptiveDialogShell.razor"), StringComparison.Ordinal);
    }

    [Fact]
    public void TypedLibraryCollections_UseAdaptiveEditorsAndGuardedLookupHardDelete()
    {
        var classes = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor");
        var classesCode = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_LookupLibrary.razor.cs");
        var valueEditor = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_AddLookupValue.razor");
        var priceLists = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor");
        var priceListsCode = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor.cs");
        var categoryEditor = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_CategoryEditor.razor");
        var supplierEditor = ReadFrontendSource("Components/Pages/Lib/Tabs/Dialog/Dialog_SupplierEditor.razor");

        Assert.Contains("<VppAdaptiveDialogShell", categoryEditor, StringComparison.Ordinal);
        Assert.Contains("<VppAdaptiveDialogShell", supplierEditor, StringComparison.Ordinal);
        Assert.Contains("<VppAdminActiveToggle", classes, StringComparison.Ordinal);
        Assert.Contains("delete_forever", classes, StringComparison.Ordinal);
        Assert.Contains("!data.IsDeleted", classes, StringComparison.Ordinal);
        Assert.Contains("HardDeleteCategoryAsync", classesCode, StringComparison.Ordinal);
        Assert.Contains("HardDeleteValueAsync", classesCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.ExtraField1", valueEditor, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.ExtraField2", valueEditor, StringComparison.Ordinal);
        Assert.DoesNotContain("Model.ExtraField3", valueEditor, StringComparison.Ordinal);
        Assert.Contains("FieldExample", valueEditor, StringComparison.Ordinal);
        Assert.Contains("price-list-lifecycle-menu", priceLists, StringComparison.Ordinal);
        Assert.Contains("HardDeleteAsync", priceListsCode, StringComparison.Ordinal);
        Assert.Contains("delete_forever", priceListsCode, StringComparison.Ordinal);

        foreach (var tab in new[]
                 {
                     "Tab_CategoryLibrary.razor",
                     "Tab_ItemLibrary.razor",
                     "Tab_SupplierLibrary.razor",
                     "Tab_DepartmentLibrary.razor"
                 })
        {
            var source = ReadFrontendSource($"Components/Pages/Lib/Tabs/{tab}");
            Assert.Contains("<VppAdminActiveToggle", source, StringComparison.Ordinal);
            Assert.Contains("delete_forever", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PriceListStatusFilter_UsesLocalizedToolbarInsteadOfHeaderPopup()
    {
        var page = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor");
        var code = ReadFrontendSource("Components/Pages/Lib/Tabs/Tab_PriceListLibrary.razor.cs");

        Assert.Contains("PriceListStatusOptions", page, StringComparison.Ordinal);
        Assert.Contains("Filterable=\"false\"", page, StringComparison.Ordinal);
        Assert.Contains("SelectedStatusFilter", code, StringComparison.Ordinal);
        Assert.DoesNotContain("DataGridLoadColumnFilterDataEventArgs", code, StringComparison.Ordinal);
        Assert.Contains("PriceListStatusPublished", code, StringComparison.Ordinal);
    }

    [Fact]
    public void PeriodOperations_UsePreviewBeforeConfirmation()
    {
        var host = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor");
        var workspace = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodOperationsWorkspace.razor");
        var settlement = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor");
        var settlementCode = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor.cs");

        Assert.Contains("<PeriodOperationsWorkspace", host, StringComparison.Ordinal);
        Assert.Contains("<PeriodSettlementPanel", workspace, StringComparison.Ordinal);
        Assert.Contains("CurrentPeriodYear=\"@currentPeriodYear\"", workspace, StringComparison.Ordinal);
        Assert.Contains("PreviousPeriodYear=\"@previousPeriodYear\"", workspace, StringComparison.Ordinal);
        Assert.Contains("State.SetPeriod(previousPeriodYear, previousPeriodMonth)", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("DefaultYear", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppWorkflowStepper", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-period-workspace-header", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("<h1", workspace, StringComparison.Ordinal);
        Assert.Contains("<VppSegmentedSelector", settlement, StringComparison.Ordinal);
        Assert.Contains("VppFilterSelect TValue=\"Guid?\"", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementByItem", settlementCode, StringComparison.Ordinal);
        Assert.Contains("SettlementByDepartment", settlementCode, StringComparison.Ordinal);
        Assert.Contains("HasPeriodBlockers", settlement, StringComparison.Ordinal);
        Assert.Contains("Preview?.Blockers", settlementCode, StringComparison.Ordinal);
        Assert.Contains("State.Exceptions", settlementCode, StringComparison.Ordinal);
        Assert.Contains("InputHash = preview.InputHash", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Phương án chốt", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementViewPreview", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("<Footer>", settlement, StringComparison.Ordinal);
        Assert.Contains("Click=\"@SettleAsync\"", settlement, StringComparison.Ordinal);
    }

    [Fact]
    public void PeriodOperations_HostDelegatesLargeWorkspacesToFocusedComponents()
    {
        var host = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor");
        var hostCode = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor.cs");
        var approvals = ReadFrontendSource("Components/Pages/VPPRequest/Components/PendingApprovalWorkspace.razor");

        Assert.Contains("<PeriodOperationsWorkspace", host, StringComparison.Ordinal);
        Assert.Contains("<PendingApprovalWorkspace", host, StringComparison.Ordinal);
        Assert.Contains("RadzenDataGrid TItem=\"VppRequestResDTO\"", approvals, StringComparison.Ordinal);
        Assert.Contains("<VppCollectionHeader", approvals, StringComparison.Ordinal);
        Assert.Contains("<VppFilterSelect TValue=\"string\"", approvals, StringComparison.Ordinal);
        Assert.Contains("@bind-Value=\"selectedOrders\"", approvals, StringComparison.Ordinal);
        Assert.Contains("VppStatusTone.Warning", approvals, StringComparison.Ordinal);
        Assert.Contains("ApprovalDecisionHint", approvals, StringComparison.Ordinal);
        Assert.DoesNotContain("ShortCode", approvals, StringComparison.Ordinal);
        Assert.Contains("SynchronizePendingSelectionAsync", hostCode, StringComparison.Ordinal);
        Assert.Contains("FillAvailableSpace=\"true\"", approvals, StringComparison.Ordinal);
        Assert.DoesNotContain("RadzenDataGrid", host, StringComparison.Ordinal);
        Assert.True(host.Split('\n').Length < 100);
    }

    [Fact]
    public void UnifiedSettlement_UsesDemandAndPreviewForItemAndSupplierSelection()
    {
        var page = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor");
        var code = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor.cs");

        Assert.Contains("OnSupplierChangedAsync", page, StringComparison.Ordinal);
        Assert.Contains("OnPriceListChangedAsync", page, StringComparison.Ordinal);
        Assert.Contains("AggregatedVppItemResDTO", page, StringComparison.Ordinal);
        Assert.Contains("Config.VppApi.PeriodDemand", code, StringComparison.Ordinal);
        Assert.Contains("Config.RequestApi.PeriodSettlement.Preview", code, StringComparison.Ordinal);
        Assert.Contains("PrimarySupplierId = supplierId", code, StringComparison.Ordinal);
        Assert.Contains("PriceListId = priceListId", code, StringComparison.Ordinal);
        Assert.DoesNotContain("isSupplierDialogOpen", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Config.LibraryApi.VPPPrice_ItemPrices", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Reports_ShowSettlementEvidenceOnlyWhenTheApiProvidesIt()
    {
        var report = ReadFrontendSource("Components/Pages/Report.razor");
        var reportCode = ReadFrontendSource("Components/Pages/Report.razor.cs");
        var reportClient = ReadFrontendSource("Features/Reports/Api/ReportsApiClient.cs");

        Assert.Contains("Summary.SettlementId.HasValue", report, StringComparison.Ordinal);
        Assert.Contains("vpp-report-settlement-evidence", report, StringComparison.Ordinal);
        Assert.Contains("Loc[\"SettlementEvidence\"]", report, StringComparison.Ordinal);
        Assert.Contains("<VppFileExportActions", report, StringComparison.Ordinal);
        Assert.Contains("ReportExportFormats", report, StringComparison.Ordinal);
        Assert.Contains("ClearReportFiltersAsync", report, StringComparison.Ordinal);
        Assert.Contains("ValueProperty=\"TotalAmount\"", report, StringComparison.Ordinal);
        Assert.Contains("Smooth=\"@CanSmoothPeriodTrend\"", report, StringComparison.Ordinal);
        Assert.Contains("HasPeriodTrend", reportCode, StringComparison.Ordinal);
        Assert.Contains("HasStatusChartData", reportCode, StringComparison.Ordinal);
        Assert.Contains(".Where(item => item.OrderCount > 0)", reportCode, StringComparison.Ordinal);
        Assert.Contains("RadzenDataGrid TItem=\"ReportDepartmentPointResDTO\"", report, StringComparison.Ordinal);
        Assert.Contains("FilteredDepartmentBreakdown", reportCode, StringComparison.Ordinal);
        Assert.Contains("VppFileExportFormat.Pdf", reportCode, StringComparison.Ordinal);
        Assert.Contains("VppFileExportFormat.Excel", reportCode, StringComparison.Ordinal);
        Assert.Contains("VppFileExportFormat.Csv", reportCode, StringComparison.Ordinal);
        Assert.Contains("Reports.ExportAsync", reportCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", reportCode, StringComparison.Ordinal);
        Assert.DoesNotContain("api/reports", reportCode, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildEndpoint", reportCode, StringComparison.Ordinal);
        Assert.Contains("format.ApiSuffix()", reportClient, StringComparison.Ordinal);
        Assert.Contains("api/reports/{action}", reportClient, StringComparison.Ordinal);
        Assert.DoesNotContain("aria-label=\"Bằng chứng chốt kỳ\"", report, StringComparison.Ordinal);
    }

    [Fact]
    public void M6_UserAdministration_UsesAtlasCopyAndNeverRendersSessionVersion()
    {
        var users = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_User.razor");
        var userCode = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_User.razor.cs");
        var invitation = ReadFrontendSource("Components/Pages/Permission/Dialogs/Dialog_UserInvitationEditor.razor");
        var renderedUserSources = string.Join('\n', users, userCode, invitation);

        Assert.Contains("Loc[\"UserSearchPlaceholder\"]", users, StringComparison.Ordinal);
        Assert.Contains("Loc[\"AllAccountStatuses\"]", users, StringComparison.Ordinal);
        Assert.Contains("Loc[\"PermissionGroup\"]", users, StringComparison.Ordinal);
        Assert.Contains("Text=\"@Loc[\"Approve\"]\"", users, StringComparison.Ordinal);
        Assert.Contains("GetApprovalActionTitle(user)", users, StringComparison.Ordinal);
        Assert.Contains("vpp-admin-action-label", users, StringComparison.Ordinal);
        Assert.Contains("SelfMembershipChangeBlocked", userCode, StringComparison.Ordinal);
        Assert.Contains("accountStatus=", userCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Reset mật khẩu", users, StringComparison.Ordinal);
        Assert.DoesNotContain("TemporaryPassword", userCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Membership updated", userCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Deactivate membership", userCode, StringComparison.Ordinal);
        Assert.DoesNotContain("SessionVersion", renderedUserSources, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", renderedUserSources, StringComparison.Ordinal);
        Assert.DoesNotContain("SecurityStamp", renderedUserSources, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessToken", renderedUserSources, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", renderedUserSources, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConfirmationToken", renderedUserSources, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void M6_PermissionMatrix_IsCanonicalReadOnlyAndUsesBackendGroupCodes()
    {
        var page = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_PagePermission.razor");
        var code = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_PagePermission.razor.cs");

        Assert.Equal(18, CanonicalRbac.Actions.Count);
        Assert.Equal(3, CanonicalRbac.Personas.Count);
        Assert.All(CanonicalRbac.Actions, action =>
            Assert.True(CanonicalRbac.HasAction(CanonicalRbac.Dev.GroupId, action.PermissionCode)));
        Assert.Contains("CanonicalRbac.Actions", page, StringComparison.Ordinal);
        Assert.Contains("CanonicalRbac.Personas", page, StringComparison.Ordinal);
        Assert.Contains("CanonicalRbac.HasAction", page, StringComparison.Ordinal);
        Assert.Contains("PermissionGroupDto.GroupName", page, StringComparison.Ordinal);
        Assert.Contains("group.GroupCode", code, StringComparison.Ordinal);
        Assert.DoesNotContain("group.GroupName switch", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Permission updated", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Lưu ma trận quyền", page, StringComparison.Ordinal);
    }

    [Fact]
    public void M6_SecurityAudit_IsTypedReadOnlyAndUsesAdaptiveDetail()
    {
        var page = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_SecurityAudit.razor");
        var code = ReadFrontendSource("Components/Pages/Permission/Tabs/Tab_SecurityAudit.razor.cs");
        var dialog = ReadFrontendSource("Components/Pages/Permission/Dialogs/Dialog_SecurityAuditDetail.razor");
        var sidebar = ReadFrontendSource("Components/Layout/LeftSidebar.razor");
        var sidebarCode = ReadFrontendSource("Components/Layout/LeftSidebar.razor.cs");
        var routeCatalog = ReadFrontendSource("Helpers/RouteCatalog.cs");

        Assert.Contains("security-audit-data-surface", page, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ServerPaging", page, StringComparison.Ordinal);
        Assert.Contains("SecurityAuditResDTO", page, StringComparison.Ordinal);
        Assert.Contains("/api/Permission/security-audits", code, StringComparison.Ordinal);
        Assert.Contains("Dialog_SecurityAuditDetail", code, StringComparison.Ordinal);
        Assert.Contains("VppAdaptiveDialogShell", dialog, StringComparison.Ordinal);
        Assert.Equal("/permission?tab=2", ShellNavigationCatalog.SecurityAudit.Path);
        Assert.Equal(Permissions.PermissionManage, ShellNavigationCatalog.SecurityAudit.Permission);
        Assert.Contains("ShellNavigationCatalog.SecurityAudit.Path", sidebar, StringComparison.Ordinal);
        Assert.Contains("CanViewShellItem(ShellNavigationCatalog.SecurityAudit)", sidebarCode, StringComparison.Ordinal);
        Assert.Contains("permission.security-audit", routeCatalog, StringComparison.Ordinal);
        Assert.DoesNotContain("PostFromApi", code, StringComparison.Ordinal);
        Assert.DoesNotContain("PatchFromApi", code, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteFromApi", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Password", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Token", page, StringComparison.OrdinalIgnoreCase);
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
    public void M1_AccountRoutesUseTheCanonicalAccountWorkspaceDirectly(string relativePath)
    {
        var source = ReadFrontendSource(relativePath);

        Assert.Contains("<VppAccountWorkspace", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_CatalogUsesFlatSharedFiltersAndVisibleServerPaging()
    {
        var catalog = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor");
        var catalogCode = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_ProductCatalog.razor.cs");

        Assert.DoesNotContain("vpp-catalog-card-header", catalog, StringComparison.Ordinal);
        Assert.Contains("<VppFilterSearch", catalog, StringComparison.Ordinal);
        Assert.Equal(2, catalog.Split("<VppFilterSelect", StringSplitOptions.None).Length - 1);
        Assert.Contains("AllowPaging=\"true\"", catalog, StringComparison.Ordinal);
        Assert.Contains("PagerAlwaysVisible=\"true\"", catalog, StringComparison.Ordinal);
        Assert.Contains("Count=\"@ProductCount\"", catalog, StringComparison.Ordinal);
        Assert.Contains("LoadData=\"@LoadProductsAsync\"", catalog, StringComparison.Ordinal);
        Assert.Contains("Title=\"#\"", catalog, StringComparison.Ordinal);
        Assert.Contains("CurrentSkip = args.Skip ?? 0;", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("DownloadCatalog", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowCatalogDownloadNotice", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"@Loc[\"Status\"]\"", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_OrderCreateUsesCompactTwoStepNavigationAndAtlasFooterActions()
    {
        var page = ReadFrontendSource("Components/Pages/VPPRequest/Page_OrderCreate.razor");
        var selection = ReadFrontendSource("Components/Pages/VPPRequest/OrderCreateStep2.razor");

        Assert.Contains("<VppWorkflowStepper", page, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-flow-header", page, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-flow-heading", page, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-flow-back", page, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-order-save-draft", page, StringComparison.Ordinal);
        Assert.Contains("BackRequested=\"@GoBack\"", page, StringComparison.Ordinal);
        Assert.Contains("SaveDraftRequested", page, StringComparison.Ordinal);
        Assert.Contains("vpp-order-action-back", selection, StringComparison.Ordinal);
        Assert.Contains("vpp-order-action-save", selection, StringComparison.Ordinal);
        Assert.Contains("vpp-order-action-note", selection, StringComparison.Ordinal);
        Assert.Contains("popovertarget=\"@GetItemNotePopoverId(item)\"", selection, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ClientSnapshotPaged", selection, StringComparison.Ordinal);
        Assert.Contains("SnapshotBatchSize = 100", selection, StringComparison.Ordinal);
        Assert.Contains("<RadzenPager", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("LoadData=", selection, StringComparison.Ordinal);
        Assert.Contains("VppPagingProfiles.LargeWorkingSet", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("<RadzenDataGrid", selection, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_OrderAndHistoryCollectionsKeepTheirDistinctDataContracts()
    {
        var orders = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_Orders.razor");
        var ordersCode = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_Orders.razor.cs");
        // Sau C-7, hai grid của màn Lịch sử nằm trong hai component con thay vì Tab_History.razor.
        var historyOrders = ReadFrontendSource("Components/Pages/VPPRequest/Components/HistoryOrderList.razor");
        var historyDetail = ReadFrontendSource("Components/Pages/VPPRequest/Components/HistoryOrderDetailSheet.razor");
        var detail = ReadFrontendSource("Components/Pages/VPPRequest/Components/VppOrderWorkspacePanel.razor");
        var orderItemsSurface = ReadFrontendSource("Components/DesignSystem/Composites/VppOrderItemsSurface.razor");

        Assert.Contains("CurrentOrderViewIndex", ordersCode, StringComparison.Ordinal);
        Assert.Contains("SupplementOrderViewIndex", ordersCode, StringComparison.Ordinal);
        Assert.Contains("PreviousOrderViewIndex", ordersCode, StringComparison.Ordinal);
        Assert.DoesNotContain("export-pdf-coming-soon", orders, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"true\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("VppOrderItemsSurfaceVariant.HistoryDrawer", historyDetail, StringComparison.Ordinal);
        Assert.Contains("VppOrderItemsSurfaceVariant.Workspace", detail, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"@UsePaging\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"false\"", orderItemsSurface, StringComparison.Ordinal);
    }

    [Fact]
    public void Aspire_DoesNotRegisterTheRetiredReactPreview()
    {
        var appHost = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Hosting",
            "AppHost",
            "AppHost.cs"));

        Assert.DoesNotContain("Frontend:EnableReactPreview", appHost, StringComparison.Ordinal);
        Assert.DoesNotContain("AddViteApp", appHost, StringComparison.Ordinal);
    }

    private static string ReadFrontendSource(string relativePath)
    {
        var projectRoot = Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");
        return File.ReadAllText(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
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
