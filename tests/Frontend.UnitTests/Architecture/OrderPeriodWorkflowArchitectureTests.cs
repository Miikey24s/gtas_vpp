using gtas_vpp_fe.Components.Layout;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class OrderPeriodWorkflowArchitectureTests
{
    [Fact]
    public void SystemAdministration_OwnsVersionedOrderingDefaults()
    {
        var route = RouteCatalog.GetRequired("permission.order-period-settings");
        var permissionTabs = ReadFrontend("Components/Pages/Permission/Component_Permission.razor");
        var settings = ReadFrontend("Components/Pages/Permission/Tabs/Tab_OrderPeriodSettings.razor");
        var operations = ReadFrontend("Components/Pages/VPPRequest/Components/OrderPeriodManagementWorkspace.razor");
        var operationsCode = ReadFrontend("Components/Pages/VPPRequest/Components/OrderPeriodManagementWorkspace.razor.cs");
        var createPeriodDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_OrderPeriodCreate.razor");

        Assert.Equal("/permission?tab=3", route.Path);
        Assert.Contains(Permissions.PeriodSettingsManage, route.AnyOfPermissions);
        Assert.Equal("SystemAdministration", ShellNavigationCatalog.Permission.LabelKey);
        Assert.Contains("<Tab_OrderPeriodSettings", permissionTabs, StringComparison.Ordinal);
        Assert.Contains("RadzenTemplateForm", settings, StringComparison.Ordinal);
        Assert.Contains("PostCloseAdjustmentDays", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("SettlementReopenWindowDays", settings, StringComparison.Ordinal);
        Assert.Contains("ListSettingsHistoryAsync", ReadFrontend("Components/Pages/Permission/Tabs/Tab_OrderPeriodSettings.razor.cs"), StringComparison.Ordinal);
        Assert.DoesNotContain("period-settings-panel", operations, StringComparison.Ordinal);
        Assert.DoesNotContain("Cấu hình đang áp dụng", operations, StringComparison.Ordinal);
        Assert.DoesNotContain("Mở đủ số kỳ", operations, StringComparison.Ordinal);
        Assert.DoesNotContain("Mở kỳ rời rạc", operations, StringComparison.Ordinal);
        Assert.DoesNotContain("manual-period-panel", operations, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveSettingsAsync", operationsCode, StringComparison.Ordinal);
        Assert.Contains("GetCurrentSettingsAsync", operationsCode, StringComparison.Ordinal);
        Assert.Contains("AddText=\"Thêm kỳ\"", operations, StringComparison.Ordinal);
        Assert.Contains("CreateManualAsync", operationsCode, StringComparison.Ordinal);
        Assert.Contains("Settings.SupplementApprovalGraceDays", createPeriodDialog, StringComparison.Ordinal);
        Assert.Contains("Settings.PostCloseAdjustmentDays", createPeriodDialog, StringComparison.Ordinal);
        Assert.Contains("Bạn có thể đổi riêng cho kỳ này", createPeriodDialog, StringComparison.Ordinal);
        Assert.Contains("AvailableMonthOptions", createPeriodDialog, StringComparison.Ordinal);
        Assert.Contains("Không thể tạo kỳ đặt hàng trong quá khứ.", createPeriodDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("targetYear - 1", createPeriodDialog, StringComparison.Ordinal);
    }

    [Fact]
    public void PeriodOperations_UseVersionedAdjustmentsAndDeepLinkTheExactSettlementPeriod()
    {
        var operations = ReadFrontend("Components/Pages/VPPRequest/Components/OrderPeriodManagementWorkspace.razor");
        var operationsCode = ReadFrontend("Components/Pages/VPPRequest/Components/OrderPeriodManagementWorkspace.razor.cs");
        var periodTab = ReadFrontend("Components/Pages/VPPRequest/Tabs/Tab_AdminApproval.razor.cs");
        var workspace = ReadFrontend("Components/Pages/VPPRequest/Components/PeriodOperationsWorkspace.razor");
        var settlement = ReadFrontend("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor");
        var settlementCode = ReadFrontend("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.razor.cs");
        var settlementDetails = ReadFrontend("Components/Pages/VPPRequest/Components/PeriodSettlementPanel.OrderDetails.cs");
        var settlementPreviewDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_SettlementPreview.razor");
        var historyDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_SettlementHistory.razor");
        var periodActionDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_OrderPeriodAction.razor");

        Assert.DoesNotContain("ReopenSubmissions", periodActionDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("CanReopenSubmissions", operationsCode, StringComparison.Ordinal);
        Assert.Contains("periodYear={period.Year}&periodMonth={period.Month}", operationsCode, StringComparison.Ordinal);
        Assert.Contains("periodYear", periodTab, StringComparison.Ordinal);
        Assert.Contains("TargetYear", workspace, StringComparison.Ordinal);
        Assert.DoesNotContain("Loc[\"SettlementCorrectionAction\"]", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementHistoryAction", settlement, StringComparison.Ordinal);
        Assert.Contains("Loc[\"SettlementResettleAction\"]", settlement, StringComparison.Ordinal);
        Assert.Contains("OpenSettlementPreviewDialogAsync", settlement, StringComparison.Ordinal);
        Assert.Contains("Dialog_SettlementPreview", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenSettlementCorrectionDialogAsync", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("<VppFileExportActions", settlement, StringComparison.Ordinal);
        Assert.Contains("vpp-settlement-export-card", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("vpp-settlement-decision-action", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("Property=\"RegularOrderCount\"", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("Property=\"AdditionalOrderCount\"", settlement, StringComparison.Ordinal);
        Assert.Contains("VppCategoryChip Text=\"@OrderCountLabel", settlement, StringComparison.Ordinal);
        Assert.Contains("Title=\"@Loc[\"DemandLines\"]\"", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("ReopenForResettlement", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementVersionText", settlement, StringComparison.Ordinal);
        Assert.Contains("SavedSettlementVersions", historyDialog, StringComparison.Ordinal);
        Assert.Contains("Settlement.ListVersionsAsync(Year, Month)", historyDialog, StringComparison.Ordinal);
        Assert.Contains("nameof(Dialog_SettlementHistory.Year)", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("var versions = await Settlement.ListVersionsAsync", settlementCode, StringComparison.Ordinal);
        Assert.Contains("SettlementChangesSinceLastVersion", settlementPreviewDialog, StringComparison.Ordinal);
        Assert.Contains("InitialOrderRenderCount", settlementPreviewDialog, StringComparison.Ordinal);
        Assert.Contains("PendingCorrectionCount", settlementPreviewDialog, StringComparison.Ordinal);
        Assert.Contains("PreviewSelectionChanged", settlementPreviewDialog, StringComparison.Ordinal);
        Assert.Contains("OrderAdjustmentRequested", settlementPreviewDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("SettleQuoteCoverage", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementNetAmount", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementVatAmount", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementGrossAmount", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementTopItem", settlement, StringComparison.Ordinal);
        Assert.Contains("vpp-settlement-filter-department", settlement, StringComparison.Ordinal);
        Assert.Contains("vpp-settlement-filter-order-type", settlement, StringComparison.Ordinal);
        Assert.Contains("vpp-settlement-filter-status", settlement, StringComparison.Ordinal);
        Assert.Contains("Property=\"DepartmentSummary\"", settlement, StringComparison.Ordinal);
        Assert.Contains("StatusCounts", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementAllOrdersInGroup", settlementDetails, StringComparison.Ordinal);
        Assert.Contains("Guid.Empty", settlementDetails, StringComparison.Ordinal);
        Assert.Contains("Task.WhenAll", settlementCode, StringComparison.Ordinal);
        Assert.Contains("LoadDeferredSettlementAdministrationAsync", settlementCode, StringComparison.Ordinal);
        Assert.Contains("hasLoadedDepartmentDirectory", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("await LoadStatusAsync();\r\n            await LoadPreviewAsync();", settlementCode, StringComparison.Ordinal);
        Assert.DoesNotContain("period-action-panel", operations, StringComparison.Ordinal);
        Assert.DoesNotContain("Đóng nhận đơn sớm", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("CloseAction", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Mở lại nhận đơn", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ReopenSubmissionsAsync", operationsCode, StringComparison.Ordinal);
        Assert.Contains("Text=\"Xem\"", operations, StringComparison.Ordinal);
        Assert.Contains("Text=\"Chốt kỳ\"", operations, StringComparison.Ordinal);
        Assert.Contains("Click=\"@(() => NavigateToSettlement(row))\"", operations, StringComparison.Ordinal);
        Assert.Contains("Disabled=\"@(IsBusy || !CanSettlePeriod(row))\"", operations, StringComparison.Ordinal);
        Assert.Contains("Disabled: !period.CanExtendDeadline", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Disabled: !period.CanEditSchedule", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Sửa lịch", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("EditScheduleAsync", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("VppOrderPeriodUpdateReqDTO", periodActionDialog, StringComparison.Ordinal);
        Assert.Contains("VppOrderPeriodExtendDeadlineReqDTO", periodActionDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowOpenWhenAllDisabled=\"true\"", operations, StringComparison.Ordinal);
        Assert.Contains("Vô hiệu hóa", operationsCode, StringComparison.Ordinal);
        Assert.Contains("Khôi phục", operationsCode, StringComparison.Ordinal);
        Assert.Contains("Xóa kỳ", operationsCode, StringComparison.Ordinal);
        Assert.Contains("Disabled: period.IsDeleted ? !period.CanRestore : !period.CanDeactivate", operationsCode, StringComparison.Ordinal);
        Assert.Contains("Disabled: !period.CanHardDelete", operationsCode, StringComparison.Ordinal);
        Assert.Contains("DeactivatePeriodAsync", operationsCode, StringComparison.Ordinal);
        Assert.Contains("HardDeletePeriodAsync", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowPeriodDetailsAsync", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("Dialog_OrderPeriodDetails", operationsCode, StringComparison.Ordinal);
        Assert.DoesNotContain("if (period.CanCloseSubmissions || period.CanEditSchedule)", operationsCode, StringComparison.Ordinal);
        Assert.Contains("<VppDataToolbar", operations, StringComparison.Ordinal);
        Assert.Contains("Tìm kỳ hoặc thay đổi gần nhất", operations, StringComparison.Ordinal);
        Assert.Contains("Tất cả trạng thái", operations, StringComparison.Ordinal);
        Assert.Contains("Tất cả năm", operations, StringComparison.Ordinal);
        Assert.Contains("AdjustAfterCloseAsync", settlementDetails, StringComparison.Ordinal);
        Assert.Contains("CanAdjustOrders", settlementDetails, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(
            FindRepositoryRoot(),
            "src", "Frontend", "Blazor", "Components", "Pages", "VPPRequest", "Components",
            "Dialog_OrderPeriodDetails.razor")));
    }

    private static string ReadFrontend(string relativePath)
    {
        var root = FindRepositoryRoot();
        return File.ReadAllText(Path.Combine(root, "src", "Frontend", "Blazor", relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
