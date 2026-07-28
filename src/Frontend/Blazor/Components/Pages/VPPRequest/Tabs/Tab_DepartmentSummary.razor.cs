using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

public partial class Tab_DepartmentSummary : HistoryOrderWorkspaceTabBase
{
    protected override string HistoryPermission => Permissions.RequestDepartmentSummary;
    protected override string HistoryErrorSummary => Loc["DepartmentSummary"];
    protected override string HistoryPageEndpoint => Config.VppApi.DepartmentOrderHistory;
    protected override string HistorySummaryEndpoint => Config.VppApi.DepartmentOrderHistorySummary;
    protected override string HistoryFilterScope => "scope=department";
}
