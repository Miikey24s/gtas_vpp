using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
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

        public List<VPP01_RequestHeaderResDTO> PendingOrders { get; set; } = new();
        private HashSet<Guid> LoadedDetailOrderIds { get; } = new();
        private HashSet<Guid> LoadingDetailOrderIds { get; } = new();

        public bool IsLoading { get; set; } = true;
        public bool IsGridLoading { get; set; }
        public bool IsActionLoading { get; set; }
        public int TotalCount { get; set; }
        public int TotalLines { get; set; }
        public int TotalQty { get; set; }
        public int PageSize { get; set; } = 20;
        public int CurrentSkip { get; set; }

        private bool _isFirstLoad = true;
        private bool CanView => claims.HasPermission(Permissions.RequestAdminApproval);

        protected override async Task OnInitializedAsync()
        {
            await LoadPendingOrdersAsync();
        }

        protected async Task OnLoadData(Radzen.LoadDataArgs args)
        {
            CurrentSkip = args.Skip ?? 0;
            if (args.Top.HasValue && args.Top.Value > 0) PageSize = args.Top.Value;
            await LoadPendingOrdersAsync();
        }

        private async Task LoadPendingOrdersAsync()
        {
            if (!CanView) return;

            if (_isFirstLoad)
            {
                IsLoading = true;
                glb.isBusyPage = true;
            }
            else
            {
                IsGridLoading = true;
            }

            try
            {
                var endpoint = $"/api/VPPRequest/additional-orders/pending?skip={CurrentSkip}&top={PageSize}";
                var (data, totalCount, totalLines, totalQty) = await _apiServices.GetFromApiWithStatsAsync<List<VPP01_RequestHeaderResDTO>>(endpoint);

                PendingOrders = data ?? new();
                TotalCount = totalCount;
                TotalLines = totalLines;
                TotalQty = totalQty;

                LoadedDetailOrderIds.Clear();
                LoadingDetailOrderIds.Clear();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Error", $"Failed to load pending orders: {ex.Message}");
            }
            finally
            {
                if (_isFirstLoad)
                {
                    glb.isBusyPage = false;
                    _isFirstLoad = false;
                }
                IsLoading = false;
                IsGridLoading = false;
                StateHasChanged();
            }
        }

        protected async Task OnRowExpandAsync(VPP01_RequestHeaderResDTO row)
        {
            if (row == null || row.Id == Guid.Empty || LoadedDetailOrderIds.Contains(row.Id) || LoadingDetailOrderIds.Contains(row.Id))
                return;

            LoadingDetailOrderIds.Add(row.Id);
            try
            {
                var detail = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>($"/api/VPPRequest/orders/{row.Id}");
                row.Items = detail?.Items ?? new List<VPP02_RequestDetailResDTO>();
                LoadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Admin Approval", $"Load details failed: {ex.Message}");
            }
            finally
            {
                LoadingDetailOrderIds.Remove(row.Id);
                StateHasChanged();
            }
        }

        protected bool IsRowDetailLoading(Guid orderId) => LoadingDetailOrderIds.Contains(orderId);

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
                _isFirstLoad = false;
                await LoadPendingOrdersAsync();
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
                _isFirstLoad = false;
                await LoadPendingOrdersAsync();
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
