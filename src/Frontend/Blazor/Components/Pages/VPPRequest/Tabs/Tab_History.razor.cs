using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

public partial class Tab_History : HistoryOrderWorkspaceTabBase
{
    protected override string HistoryPermission => Permissions.RequestHistory;
    protected override string HistoryErrorSummary => Loc["History"];
    protected override string HistoryPageEndpoint => Config.VppApi.MyOrderHistory;
    protected override string HistorySummaryEndpoint => Config.VppApi.MyOrderHistorySummary;
    protected override string HistoryFilterScope => "scope=my-orders";
}
