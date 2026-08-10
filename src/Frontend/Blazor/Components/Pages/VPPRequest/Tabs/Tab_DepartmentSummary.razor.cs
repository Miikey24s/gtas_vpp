// PAGE LOGIC: VPPRequest/Tabs/Tab_DepartmentSummary.razor.cs
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

public partial class Tab_DepartmentSummary : HistoryOrderWorkspaceTabBase
{
    protected override string HistoryPermission => Permissions.RequestDepartmentSummary;
    protected override string HistoryErrorSummary => Loc["DepartmentSummary"];
    protected override OrderHistoryScope HistoryScope => OrderHistoryScope.Department;
}
