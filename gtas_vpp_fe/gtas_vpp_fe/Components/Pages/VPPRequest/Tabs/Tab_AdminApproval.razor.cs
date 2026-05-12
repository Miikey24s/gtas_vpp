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

        protected override bool CanView => claims.HasPermission(Permissions.RequestAdminApproval);
        protected override string ErrorSummary => "Admin Approval";

        protected override string BuildEndpoint()
            => $"/api/VPPRequest/additional-orders/pending?skip={CurrentSkip}&top={PageSize}";

        private async Task HandleApproveClick(VPP01_RequestHeaderResDTO order)
        {
            if (order == null) return;

            var confirm = await DialogService.Confirm(
                "Are you sure you want to approve this additional order?",
                "Approve Order",
                new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });

            if (confirm != true) return;

            IsActionLoading = true;
            StateHasChanged();
            try
            {
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/approve", null);
                NotificationService.Notify(NotificationSeverity.Success, "Success", "Order approved successfully.");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to approve order: {ex.Message}");
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
                $"Are you sure you want to reject order {order.VPPCode}?",
                "Reject Order",
                new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });

            if (confirm != true) return;

            IsActionLoading = true;
            StateHasChanged();
            try
            {
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/reject", new { Reason = "" });
                NotificationService.Notify(NotificationSeverity.Success, "Success", "Order rejected successfully.");
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to reject order: {ex.Message}");
            }
            finally
            {
                IsActionLoading = false;
                StateHasChanged();
            }
        }
    }
}
