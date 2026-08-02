using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

public partial class Tab_History : HistoryOrderWorkspaceTabBase
{
    protected override string HistoryPermission => Permissions.RequestHistory;
    protected override string HistoryErrorSummary => Loc["History"];
    protected override OrderHistoryScope HistoryScope => OrderHistoryScope.Own;
}
