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
        var correctionDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_SettlementCorrection.razor");
        var historyDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_SettlementHistory.razor");
        var periodActionDialog = ReadFrontend("Components/Pages/VPPRequest/Components/Dialog_OrderPeriodAction.razor");

        Assert.Contains("ReopenSubmissions", periodActionDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("CanReopenSubmissions", operationsCode, StringComparison.Ordinal);
        Assert.Contains("periodYear={period.Year}&periodMonth={period.Month}", operationsCode, StringComparison.Ordinal);
        Assert.Contains("periodYear", periodTab, StringComparison.Ordinal);
        Assert.Contains("TargetYear", workspace, StringComparison.Ordinal);
        Assert.Contains("AdjustSettlementResult", settlement, StringComparison.Ordinal);
        Assert.DoesNotContain("ReopenForResettlement", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementVersionText", settlement, StringComparison.Ordinal);
        Assert.Contains("SavedSettlementVersions", historyDialog, StringComparison.Ordinal);
        Assert.Contains("CreateCorrectionRevision", correctionDialog, StringComparison.Ordinal);
        Assert.DoesNotContain("SettleQuoteCoverage", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementNetAmount", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementVatAmount", settlement, StringComparison.Ordinal);
        Assert.Contains("SettlementGrossAmount", settlement, StringComparison.Ordinal);
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
        Assert.Contains("Disabled: !period.CanEditSchedule", operationsCode, StringComparison.Ordinal);
        Assert.Contains("AllowOpenWhenAllDisabled=\"true\"", operations, StringComparison.Ordinal);
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
