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
        var apiClient = ReadFrontendSource("Features/IdentityAccess/Api/UserAdministrationApiClient.cs");
        var invitation = ReadFrontendSource("Components/Pages/Permission/Dialogs/Dialog_UserInvitationEditor.razor");

        Assert.Contains("<VppCollectionWorkspace", page, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppListDetailWorkspace", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Component_RecordInspector", page, StringComparison.Ordinal);
        Assert.Contains("Loc[\"AddUser\"]", page, StringComparison.Ordinal);
        Assert.Contains("UserAdministrationApiClient", code, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", code, StringComparison.Ordinal);
        Assert.DoesNotContain("AuthenticationStateProvider", page, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigationManager", page, StringComparison.Ordinal);
        Assert.DoesNotContain("/api/", code, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildUsersEndpoint", code, StringComparison.Ordinal);
        Assert.Contains("/invite", apiClient, StringComparison.Ordinal);
        Assert.Contains("/send-password-reset-link", apiClient, StringComparison.Ordinal);
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
        var client = ReadFrontendSource("Features/CatalogPricing/Api/PricingApiClient.cs");

        Assert.Contains("PriceListStatusOptions", page, StringComparison.Ordinal);
        Assert.Contains("Filterable=\"false\"", page, StringComparison.Ordinal);
        Assert.Contains("PricingApi.GetPriceListsAsync", code, StringComparison.Ordinal);
        Assert.Contains("string.IsNullOrWhiteSpace(selectedStatus) ? null : selectedStatus", code, StringComparison.Ordinal);
        Assert.Contains("Status ==", client, StringComparison.Ordinal);
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
        var settlementClient = ReadFrontendSource("Features/Settlement/Api/SettlementApiClient.cs");
        var settlementRequestFactory = ReadFrontendSource("Features/Settlement/Submission/SettlementRequestFactory.cs");

        Assert.Contains("<PeriodOperationsWorkspace", host, StringComparison.Ordinal);
        Assert.Contains("<PeriodSettlementPanel", workspace, StringComparison.Ordinal);
        Assert.Contains("CurrentPeriodYear=\"@currentPeriodYear\"", workspace, StringComparison.Ordinal);
        Assert.Contains("PreviousPeriodYear=\"@previousPeriodYear\"", workspace, StringComparison.Ordinal);
        Assert.Contains("State.SetPeriod(previousPeriodYear, previousPeriodMonth)", workspace, StringComparison.Ordinal);
        Assert.Contains("RequestsQueryClient", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", workspace, StringComparison.Ordinal);
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
        Assert.Contains("SettlementRequestFactory.BuildConfirm", settlementCode, StringComparison.Ordinal);
        Assert.Contains("InputHash = preview.InputHash", settlementRequestFactory, StringComparison.Ordinal);
        Assert.Contains("SettlementApiClient", settlementCode, StringComparison.Ordinal);
        Assert.Contains("CatalogApiClient", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Config.RequestApi.PeriodSettlement", settlementCode, StringComparison.Ordinal);
        Assert.Contains("/api/periodsettlement", settlementClient, StringComparison.Ordinal);
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
        var filterBuilder = ReadFrontendSource("Features/Requests/Approval/PendingApprovalFilterBuilder.cs");
        var decisionFactory = ReadFrontendSource("Features/Requests/Approval/SupplementDecisionRequestFactory.cs");

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
        Assert.Contains("RequestsCommandClient", hostCode, StringComparison.Ordinal);
        Assert.Contains("PendingApprovalFilterBuilder.Build", hostCode, StringComparison.Ordinal);
        Assert.Contains("SupplementDecisionRequestFactory", hostCode, StringComparison.Ordinal);
        Assert.Contains("_decisionRequests.BuildApprove", hostCode, StringComparison.Ordinal);
        Assert.Contains("_decisionRequests.BuildReject", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new ApproveOrderReqDTO", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new RejectOrderReqDTO", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("EscapeFilterValue", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("_decisionIdempotencyKeys", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("VppCode.ToLower().Contains", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("DepartmentCode.ToLower() ==", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid().ToString(\"N\")", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", hostCode, StringComparison.Ordinal);
        Assert.DoesNotContain("PostFromApi", hostCode, StringComparison.Ordinal);
        Assert.Contains("FillAvailableSpace=\"true\"", approvals, StringComparison.Ordinal);
        Assert.DoesNotContain("RadzenDataGrid", host, StringComparison.Ordinal);
        Assert.True(host.Split('\n').Length < 100);
        Assert.Contains("VppCode.ToLower().Contains", filterBuilder, StringComparison.Ordinal);
        Assert.Contains("DepartmentCode.ToLower() ==", filterBuilder, StringComparison.Ordinal);
        Assert.Contains("ApproveOrderReqDTO", decisionFactory, StringComparison.Ordinal);
        Assert.Contains("RejectOrderReqDTO", decisionFactory, StringComparison.Ordinal);
    }

    [Fact]
    public void UnifiedSettlement_UsesDemandAndPreviewForItemAndSupplierSelection()
    {
        var page = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor");
        var code = ReadFrontendSource("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor.cs");
        var client = ReadFrontendSource("Features/Settlement/Api/SettlementApiClient.cs");
        var projection = ReadFrontendSource("Features/Settlement/Projection/SettlementWorkspaceProjection.cs");
        var requestFactory = ReadFrontendSource("Features/Settlement/Submission/SettlementRequestFactory.cs");
        var state = ReadFrontendSource("Features/Settlement/State/PeriodSettlementState.cs");
        var registrations = ReadFrontendSource(
            "Platform/Composition/FrontendServiceCollectionExtensions.cs");

        Assert.Contains("OnSupplierChangedAsync", page, StringComparison.Ordinal);
        Assert.Contains("OnPriceListChangedAsync", page, StringComparison.Ordinal);
        Assert.Contains("AggregatedVppItemResDTO", page, StringComparison.Ordinal);
        Assert.Contains("Settlement.GetDemandAsync", code, StringComparison.Ordinal);
        Assert.Contains("Settlement.PreviewAsync", code, StringComparison.Ordinal);
        Assert.Contains("SettlementWorkspaceProjection.BuildDepartmentRows", code, StringComparison.Ordinal);
        Assert.Contains("SettlementWorkspaceProjection.FilterItems", code, StringComparison.Ordinal);
        Assert.Contains("SettlementRequestFactory.BuildPreview", code, StringComparison.Ordinal);
        Assert.Contains("SettlementRequestFactory.BuildConfirm", code, StringComparison.Ordinal);
        Assert.Contains("SettlementRequestFactory.BuildCorrection", code, StringComparison.Ordinal);
        Assert.Contains("private bool CanSubmitCurrentPreview", code, StringComparison.Ordinal);
        Assert.Contains("hasCorrectionTarget = status is", code, StringComparison.Ordinal);
        Assert.Contains("&& !isPreviewLoading", code, StringComparison.Ordinal);
        Assert.Contains("RequireFreshPreviewForNextSubmission", code, StringComparison.Ordinal);
        Assert.Equal(
            2,
            System.Text.RegularExpressions.Regex.Matches(
                code,
                @"await LoadStatusAsync\(\);\s+State\.RequireFreshPreviewForNextSubmission\(\);")
            .Count);
        Assert.Contains("reason.Length is < 5 or > 500", code, StringComparison.Ordinal);
        Assert.DoesNotContain("MatchesClientFilters", code, StringComparison.Ordinal);
        Assert.DoesNotContain("MatchesItemFilters", code, StringComparison.Ordinal);
        Assert.DoesNotContain("new SettlementPreviewReqDTO", code, StringComparison.Ordinal);
        Assert.DoesNotContain("new SettlementConfirmReqDTO", code, StringComparison.Ordinal);
        Assert.DoesNotContain("new SettlementCorrectionReqDTO", code, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildConfirmRequest", code, StringComparison.Ordinal);
        Assert.DoesNotContain("CloneException", code, StringComparison.Ordinal);
        Assert.DoesNotContain("isSupplierDialogOpen", code, StringComparison.Ordinal);
        Assert.DoesNotContain("Config.LibraryApi.VPPPrice_ItemPrices", code, StringComparison.Ordinal);
        Assert.Contains("/period-demand?year={year}&month={month}", client, StringComparison.Ordinal);
        Assert.Contains("/all-orders?year={year}&month={month}", client, StringComparison.Ordinal);
        Assert.Contains("SettlementDepartmentStatus.Pending", projection, StringComparison.Ordinal);
        Assert.Contains("PrimarySupplierId = primarySupplierId", requestFactory, StringComparison.Ordinal);
        Assert.Contains("PriceListId = priceListId", requestFactory, StringComparison.Ordinal);
        Assert.Contains("SettlementCorrectionReqDTO", requestFactory, StringComparison.Ordinal);
        Assert.Contains("correctionReason.Trim()", requestFactory, StringComparison.Ordinal);
        Assert.Contains("namespace gtas_vpp_fe.Features.Settlement.State;", state, StringComparison.Ordinal);
        Assert.Contains("AddScoped<PeriodSettlementState>", registrations, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Components.Pages.VPPRequest.Components.PeriodSettlementState",
            registrations,
            StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Components",
            "Pages",
            "VPPRequest",
            "Components",
            "PeriodSettlementState.cs")));
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
        var apiClient = ReadFrontendSource("Features/IdentityAccess/Api/UserAdministrationApiClient.cs");
        var invitation = ReadFrontendSource("Components/Pages/Permission/Dialogs/Dialog_UserInvitationEditor.razor");
        var renderedUserSources = string.Join('\n', users, userCode, invitation);

        Assert.Contains("Loc[\"UserSearchPlaceholder\"]", users, StringComparison.Ordinal);
        Assert.Contains("Loc[\"AllAccountStatuses\"]", users, StringComparison.Ordinal);
        Assert.Contains("Loc[\"PermissionGroup\"]", users, StringComparison.Ordinal);
        Assert.Contains("Text=\"@Loc[\"Approve\"]\"", users, StringComparison.Ordinal);
        Assert.Contains("GetApprovalActionTitle(user)", users, StringComparison.Ordinal);
        Assert.Contains("vpp-admin-action-label", users, StringComparison.Ordinal);
        Assert.Contains("SelfMembershipChangeBlocked", userCode, StringComparison.Ordinal);
        Assert.Contains("accountStatus", apiClient, StringComparison.Ordinal);
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
        var apiClient = ReadFrontendSource("Features/IdentityAccess/Api/PermissionAdministrationApiClient.cs");
        var dialog = ReadFrontendSource("Components/Pages/Permission/Dialogs/Dialog_SecurityAuditDetail.razor");
        var sidebar = ReadFrontendSource("Components/Layout/LeftSidebar.razor");
        var sidebarCode = ReadFrontendSource("Components/Layout/LeftSidebar.razor.cs");
        var routeCatalog = ReadFrontendSource("Helpers/RouteCatalog.cs");

        Assert.Contains("security-audit-data-surface", page, StringComparison.Ordinal);
        Assert.Contains("VppDataSourceMode.ServerPaging", page, StringComparison.Ordinal);
        Assert.Contains("SecurityAuditResDTO", page, StringComparison.Ordinal);
        Assert.Contains("PermissionAdministrationApiClient", code, StringComparison.Ordinal);
        Assert.Contains("/security-audits", apiClient, StringComparison.Ordinal);
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
        var requestsClient = ReadFrontendSource("Features/Requests/Api/RequestsQueryClient.cs");

        Assert.DoesNotContain("vpp-catalog-card-header", catalog, StringComparison.Ordinal);
        Assert.Contains("<VppFilterSearch", catalog, StringComparison.Ordinal);
        Assert.Equal(2, catalog.Split("<VppFilterSelect", StringSplitOptions.None).Length - 1);
        Assert.Contains("AllowPaging=\"true\"", catalog, StringComparison.Ordinal);
        Assert.Contains("PagerAlwaysVisible=\"true\"", catalog, StringComparison.Ordinal);
        Assert.Contains("Count=\"@ProductCount\"", catalog, StringComparison.Ordinal);
        Assert.Contains("LoadData=\"@LoadProductsAsync\"", catalog, StringComparison.Ordinal);
        Assert.Contains("Title=\"#\"", catalog, StringComparison.Ordinal);
        Assert.Contains("CurrentSkip = args.Skip ?? 0;", catalogCode, StringComparison.Ordinal);
        Assert.Contains("VppItemResDTO", catalog, StringComparison.Ordinal);
        Assert.Contains("RequestsQueryClient", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("class ProductItem", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("class CategoryItem", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildProductsEndpoint", catalogCode, StringComparison.Ordinal);
        Assert.Contains("RequestsBase = \"/api/VPPRequest\"", requestsClient, StringComparison.Ordinal);
        Assert.Contains("/products?", requestsClient, StringComparison.Ordinal);
        Assert.DoesNotContain("DownloadCatalog", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowCatalogDownloadNotice", catalogCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Title=\"@Loc[\"Status\"]\"", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void M2_OrderCreateUsesCompactTwoStepNavigationAndAtlasFooterActions()
    {
        var page = ReadFrontendSource("Components/Pages/VPPRequest/Page_OrderCreate.razor");
        var pageCode = ReadFrontendSource("Components/Pages/VPPRequest/Page_OrderCreate.razor.cs");
        var selection = ReadFrontendSource("Components/Pages/VPPRequest/OrderCreateStep2.razor");
        var review = ReadFrontendSource("Components/Pages/VPPRequest/OrderCreateStep3.razor");
        var commandClient = ReadFrontendSource("Features/Requests/Api/RequestsCommandClient.cs");
        var draftStore = ReadFrontendSource("Features/Requests/Drafts/OrderDraftStore.cs");
        var draftPolicy = ReadFrontendSource("Features/Requests/Drafts/OrderDraftStoragePolicy.cs");
        var editorSession = ReadFrontendSource("Features/Requests/Editor/OrderEditorSession.cs");
        var submissionCoordinator = ReadFrontendSource("Features/Requests/Submission/OrderSubmissionCoordinator.cs");
        var submissionFactory = ReadFrontendSource("Features/Requests/Submission/OrderSubmissionRequestFactory.cs");
        var registrations = ReadFrontendSource(
            "Platform/Composition/FrontendServiceCollectionExtensions.cs");

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
        Assert.Contains("RequestsQueryClient", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestsCommandClient", pageCode, StringComparison.Ordinal);
        Assert.Contains("OrderDraftStore", pageCode, StringComparison.Ordinal);
        Assert.Contains("OrderEditorSession", pageCode, StringComparison.Ordinal);
        Assert.Contains("OrderSubmissionCoordinator", pageCode, StringComparison.Ordinal);
        Assert.Contains("BuildSubmissionOperation", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderSubmissionRequestFactory", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("currentStep", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderCreateContext", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new VppRequestCreateReqDTO", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new VppRequestUpdateReqDTO", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("new VppRequestRecreateReqDTO", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonSerializer", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage.setItem", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage.getItem", pageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("localStorage.removeItem", pageCode, StringComparison.Ordinal);
        Assert.Contains("localStorage.setItem", draftStore, StringComparison.Ordinal);
        Assert.Contains("localStorage.getItem", draftStore, StringComparison.Ordinal);
        Assert.Contains("OrderDraftStoragePolicy.CanRestore", draftStore, StringComparison.Ordinal);
        Assert.Contains("namespace gtas_vpp_fe.Features.Requests.Drafts;", draftPolicy, StringComparison.Ordinal);
        Assert.Contains("AddScoped<OrderDraftStore>", registrations, StringComparison.Ordinal);
        Assert.Contains("AddScoped<OrderSubmissionCoordinator>", registrations, StringComparison.Ordinal);
        Assert.Contains("OrderSubmissionOperation.Create", submissionCoordinator, StringComparison.Ordinal);
        Assert.Contains("OrderSubmissionOperation.Update", submissionCoordinator, StringComparison.Ordinal);
        Assert.Contains("OrderSubmissionOperation.Recreate", submissionCoordinator, StringComparison.Ordinal);
        Assert.Contains("OrderSubmissionRequestFactory", submissionCoordinator, StringComparison.Ordinal);
        Assert.Contains("BuildCreateRequest", submissionFactory, StringComparison.Ordinal);
        Assert.Contains("BuildUpdateRequest", submissionFactory, StringComparison.Ordinal);
        Assert.Contains("BuildRecreateRequest", submissionFactory, StringComparison.Ordinal);
        Assert.Contains("OrderEditorStep", editorSession, StringComparison.Ordinal);
        Assert.Contains("ValidateForSubmission", editorSession, StringComparison.Ordinal);
        Assert.Contains("OrderEditorSession", selection, StringComparison.Ordinal);
        Assert.Contains("OrderEditorSession", review, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderCreateContext", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderCreateContext", review, StringComparison.Ordinal);
        Assert.Contains("RequestsQueryClient", selection, StringComparison.Ordinal);
        Assert.Contains("VppItemResDTO", selection, StringComparison.Ordinal);
        Assert.Contains("@implements IDisposable", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("class ProductOption", selection, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", selection, StringComparison.Ordinal);
        Assert.Contains("/orders/{orderId}/recreate", commandClient, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Components",
            "Pages",
            "VPPRequest",
            "OrderCreateContext.cs")));
        Assert.False(File.Exists(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Helpers",
            "OrderDraftStoragePolicy.cs")));
    }

    [Fact]
    public void M2_OrderAndHistoryCollectionsKeepTheirDistinctDataContracts()
    {
        var orders = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_Orders.razor");
        var ordersCode = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/Tab_Orders.razor.cs");
        var baseOrderTab = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/BaseOrderTab.cs");
        var historyCode = ReadFrontendSource("Components/Pages/VPPRequest/Tabs/HistoryOrderWorkspaceTabBase.cs");
        var historyDialog = ReadFrontendSource("Components/Pages/VPPRequest/Components/Dialog_RequestHistory.razor");
        var requestsClient = ReadFrontendSource("Features/Requests/Api/RequestsQueryClient.cs");
        var exportClient = ReadFrontendSource("Features/Requests/Api/RequestsExportClient.cs");
        var registrations = ReadFrontendSource(
            "Platform/Composition/FrontendServiceCollectionExtensions.cs");
        // Sau C-7, hai grid của màn Lịch sử nằm trong hai component con thay vì Tab_History.razor.
        var historyOrders = ReadFrontendSource("Components/Pages/VPPRequest/Components/HistoryOrderList.razor");
        var historyDetail = ReadFrontendSource("Components/Pages/VPPRequest/Components/HistoryOrderDetailSheet.razor");
        var detail = ReadFrontendSource("Components/Pages/VPPRequest/Components/VppOrderWorkspacePanel.razor");
        var orderItemsSurface = ReadFrontendSource("Components/DesignSystem/Composites/VppOrderItemsSurface.razor");

        Assert.Contains("CurrentOrderViewIndex", ordersCode, StringComparison.Ordinal);
        Assert.Contains("SupplementOrderViewIndex", ordersCode, StringComparison.Ordinal);
        Assert.Contains("PreviousOrderViewIndex", ordersCode, StringComparison.Ordinal);
        Assert.Contains("RequestsQueryClient", ordersCode, StringComparison.Ordinal);
        Assert.Contains("RequestsCommandClient", ordersCode, StringComparison.Ordinal);
        Assert.Contains("RequestsExportClient", ordersCode, StringComparison.Ordinal);
        Assert.Contains("RequestsExportClient", baseOrderTab, StringComparison.Ordinal);
        Assert.DoesNotContain("IBrowserFileDownloadService", ordersCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IBrowserFileDownloadService", baseOrderTab, StringComparison.Ordinal);
        Assert.DoesNotContain("Config.VppApi.Orders", ordersCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Config.VppApi.Orders", baseOrderTab, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFromApi", ordersCode, StringComparison.Ordinal);
        Assert.DoesNotContain("PostFromApi", ordersCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", ordersCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", baseOrderTab, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildEndpoint", baseOrderTab, StringComparison.Ordinal);
        Assert.DoesNotContain("_apiServices", historyCode, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", historyDialog, StringComparison.Ordinal);
        Assert.Contains("GetHistoryOrdersAsync", requestsClient, StringComparison.Ordinal);
        Assert.Contains("GetOrderFilterValuesAsync", requestsClient, StringComparison.Ordinal);
        Assert.Contains("/api/VPPRequest/orders", exportClient, StringComparison.Ordinal);
        Assert.Contains("VppFileExportFormat.Pdf => \"export.pdf\"", exportClient, StringComparison.Ordinal);
        Assert.Contains("VppFileExportFormat.Excel => \"export.xlsx\"", exportClient, StringComparison.Ordinal);
        Assert.Contains("ArgumentOutOfRangeException", exportClient, StringComparison.Ordinal);
        Assert.Contains("AddScoped<RequestsExportClient>", registrations, StringComparison.Ordinal);
        Assert.DoesNotContain("export-pdf-coming-soon", orders, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"true\"", historyOrders, StringComparison.Ordinal);
        Assert.Contains("VppOrderItemsSurfaceVariant.HistoryDrawer", historyDetail, StringComparison.Ordinal);
        Assert.Contains("VppOrderItemsSurfaceVariant.Workspace", detail, StringComparison.Ordinal);
        Assert.Contains("AllowPaging=\"@UsePaging\"", orderItemsSurface, StringComparison.Ordinal);
        Assert.Contains("AllowVirtualization=\"false\"", orderItemsSurface, StringComparison.Ordinal);
    }

    [Fact]
    public void VppRequestPages_DoNotOwnTransportOrApiEndpointLiterals()
    {
        var pagesRoot = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Frontend",
            "Blazor",
            "Components",
            "Pages",
            "VPPRequest");
        var forbidden = new[]
        {
            "IAPIServices",
            "IHttpClientFactory",
            "IBrowserFileDownloadService",
            "/api/",
            "Config.VppApi",
            "Config.RequestApi",
            "Config.LibraryApi"
        };
        var violations = Directory
            .EnumerateFiles(pagesRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase))
            .Select(path => new
            {
                Path = Path.GetRelativePath(pagesRoot, path),
                Source = File.ReadAllText(path)
            })
            .SelectMany(file => forbidden
                .Where(token => file.Source.Contains(token, StringComparison.Ordinal))
                .Select(token => $"{file.Path}: {token}"))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"VPP request pages must use feature-owned clients: {string.Join(", ", violations)}");
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
