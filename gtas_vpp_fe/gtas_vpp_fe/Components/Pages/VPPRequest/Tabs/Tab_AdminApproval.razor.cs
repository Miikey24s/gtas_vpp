using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_AdminApproval
    {
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] public NotificationService NotificationService { get; set; } = default!;

        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }

        public List<VPP01_RequestHeaderResDTO> PendingOrders { get; set; } = new();
        public bool IsLoading { get; set; }
        private HashSet<Guid> LoadedDetailOrderIds { get; } = new();
        private HashSet<Guid> LoadingDetailOrderIds { get; } = new();

        private bool CanView =>
            sp_Authentication_GetPermissionSinglePage?.List_Component?.Any(x =>
                (x.ComponentCode == Config.Page_ComponentCode.ComponentCode.RequestApproval && x.IsVisible)) == true;

        protected override async Task OnInitializedAsync()
        {
            await LoadPendingOrdersAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
        }

        private async Task HandleApproveClick(VPP01_RequestHeaderResDTO order)
        {
            await ApproveOrderAsync(order);
        }

        private async Task HandleRejectClick(VPP01_RequestHeaderResDTO order)
        {
            await ShowRejectDialogAsync(order);
        }

        private async Task LoadPendingOrdersAsync()
        {
            if (!CanView) return;

            IsLoading = true;
            try
            {
                var data = await _apiServices.GetFromApiAsync<List<VPP01_RequestHeaderResDTO>>("/api/VPPRequest/additional-orders/pending");
                PendingOrders = data ?? new();
                LoadedDetailOrderIds.Clear();
                LoadingDetailOrderIds.Clear();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to load pending orders: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task OnRowExpandAsync(VPP01_RequestHeaderResDTO row)
        {
            if (row == null || row.Id == Guid.Empty || LoadedDetailOrderIds.Contains(row.Id) || LoadingDetailOrderIds.Contains(row.Id))
            {
                return;
            }

            LoadingDetailOrderIds.Add(row.Id);
            try
            {
                var detail = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>($"/api/VPPRequest/orders/{row.Id}");
                row.Items = detail?.Items ?? new List<VPP02_RequestDetailResDTO>();
                LoadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Admin Approval",
                    Detail = $"Load details failed: {ex.Message}",
                    Duration = 6000
                });
            }
            finally
            {
                LoadingDetailOrderIds.Remove(row.Id);
                StateHasChanged();
            }
        }

        protected bool IsRowDetailLoading(Guid orderId) => LoadingDetailOrderIds.Contains(orderId);

        private async Task ApproveOrderAsync(VPP01_RequestHeaderResDTO order)
        {
            try
            {
                if (order == null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Error", "Order data is invalid.");
                    return;
                }

                var confirm = await DialogService.Confirm(
                    "Are you sure you want to approve this additional order?", 
                    "Approve Order", 
                    new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });
                
                if (confirm != true)
                {
                    return;
                }

                IsLoading = true;
                StateHasChanged();
                
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/approve", null);
                
                NotificationService.Notify(NotificationSeverity.Success, "Success", "Order approved successfully.");
                await LoadPendingOrdersAsync();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to approve order: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        private async Task ShowRejectDialogAsync(VPP01_RequestHeaderResDTO order)
        {
            try
            {
                if (order == null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Error", "Order data is invalid.");
                    return;
                }

                string rejectReason = "";
                
                var confirm = await DialogService.Confirm(
                    $"Are you sure you want to reject order {order.VPPCode}? You can optionally provide a reason.",
                    "Reject Order",
                    new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" });

                if (confirm == true)
                {
                    IsLoading = true;
                    StateHasChanged();
                    
                    await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/reject", new { Reason = rejectReason });
                    
                    NotificationService.Notify(NotificationSeverity.Success, "Success", "Order rejected successfully.");
                    await LoadPendingOrdersAsync();
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to reject order: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
    }
}
