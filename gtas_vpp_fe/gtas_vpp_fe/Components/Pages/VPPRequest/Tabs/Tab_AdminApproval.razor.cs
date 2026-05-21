using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_AdminApproval : BaseOrderTab
    {
        // Dialog service is tab-specific (base doesn't need it).
        [Inject] public DialogService DialogService { get; set; } = default!;

        public bool IsActionLoading { get; set; }

        // Convenience alias so the razor template keeps its existing PendingOrders name.
        public List<VPP01_RequestHeaderResDTO> PendingOrders => Orders;

        protected override bool CanView => HasDashboardPermission(Permissions.RequestAdminApproval);
        protected override string ErrorSummary => Loc["PeriodOperations"];

        private bool CanShowSettlement => PermissionState
            .GetPagePermission(Config.Page_ComponentCode.PageCode.Dashboard)
            .List_Component
            .Any(c => c.ComponentCode == Permissions.PeriodSettle && c.IsVisible);

        protected override string BuildEndpoint()
        {
            var query = new List<string>
            {
                $"skip={CurrentSkip}",
                $"top={PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(CurrentFilterExpression)) query.Add($"filter={Uri.EscapeDataString(CurrentFilterExpression)}");
            if (!string.IsNullOrWhiteSpace(CurrentOrderByExpression)) query.Add($"orderby={Uri.EscapeDataString(CurrentOrderByExpression)}");

            return $"/api/VPPRequest/additional-orders/pending?{string.Join("&", query)}";
        }

        protected override void AppendFilterScopeQuery(List<string> query)
        {
            query.Add("scope=pending");
        }

        private async Task OnSettledRefresh()
        {
            await ReloadAsync();
        }

        private async Task HandleApproveClick(VPP01_RequestHeaderResDTO order)
        {
            if (order == null) return;

            var confirm = await DialogService.Confirm(
                Loc["ApproveOrderConfirm"],
                Loc["ApproveOrderTitle"],
                new ConfirmOptions() { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            IsActionLoading = true;
            StateHasChanged();
            try
            {
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/approve", null);
                NotificationService.Notify(NotificationSeverity.Success, Loc["Success"], Loc["OrderApprovedSuccess"]);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, Loc["Error"], string.Format(Loc["ApproveOrderFailedFormat"], ex.Message));
            }
            finally
            {
                IsActionLoading = false;
                StateHasChanged();
            }
        }

        private async Task HandleRejectClick(VPP01_RequestHeaderResDTO order)
        {
            if (order == null) return;

            var confirm = await DialogService.Confirm(
                string.Format(Loc["RejectOrderConfirm"], order.VPPCode),
                Loc["RejectOrderTitle"],
                new ConfirmOptions() { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            IsActionLoading = true;
            StateHasChanged();
            try
            {
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/reject", new { Reason = "" });
                NotificationService.Notify(NotificationSeverity.Success, Loc["Success"], Loc["OrderRejectedSuccess"]);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, Loc["Error"], string.Format(Loc["RejectOrderFailedFormat"], ex.Message));
            }
            finally
            {
                IsActionLoading = false;
                StateHasChanged();
            }
        }
    }
}
