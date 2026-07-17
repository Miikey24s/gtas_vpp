using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components.Pages.VPPRequest.Components;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_AdminApproval : BaseOrderTab
    {
        private const string PeriodTabQueryName = "periodTab";
        private const string PeriodReviewTab = "review";
        private const string PendingApprovalsTab = "pending";

        // Dialog service is tab-specific (base doesn't need it).
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private readonly HashSet<Guid> _processingOrderIds = new();
        private readonly Dictionary<(Guid OrderId, string Action), string> _decisionIdempotencyKeys = new();
        private string ActivePeriodTab { get; set; } = PeriodReviewTab;
        private bool _pendingOrdersLoaded;
        private bool _initialized;

        // Convenience alias so the razor template keeps its existing PendingOrders name.
        public List<VPP01_RequestHeaderResDTO> PendingOrders => Orders;

        protected override bool CanView => HasDashboardPermission(Permissions.RequestAdminApproval)
            || HasDashboardPermission(Permissions.PeriodSettle);
        protected override string ErrorSummary => Loc["PeriodOperations"];

        private bool CanShowSettlement => PermissionState.HasPermission(Permissions.PeriodSettle);

        private bool CanShowApprovals => CanApprove || CanReject;
        private bool CanApprove => PermissionState.HasPermission(Permissions.RequestApprove);
        private bool CanReject => PermissionState.HasPermission(Permissions.RequestReject);

        private bool ShowSettlementContent => CanShowSettlement && ActivePeriodTab == PeriodReviewTab;

        private bool ShowApprovalsContent => CanShowApprovals && ActivePeriodTab == PendingApprovalsTab;

        protected override async Task OnInitializedAsync()
        {
            SetActivePeriodTabFromUri();
            _initialized = true;
            await EnsurePendingOrdersLoadedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!_initialized)
            {
                return;
            }

            var previousTab = ActivePeriodTab;
            SetActivePeriodTabFromUri();

            if (!string.Equals(previousTab, ActivePeriodTab, StringComparison.Ordinal))
            {
                StateHasChanged();
            }

            await EnsurePendingOrdersLoadedAsync();
        }

        private async Task EnsurePendingOrdersLoadedAsync()
        {
            if (ShowApprovalsContent)
            {
                if (!_pendingOrdersLoaded)
                {
                    _pendingOrdersLoaded = true;
                    await LoadAsync();
                }

                return;
            }

            IsLoading = false;
            IsGridLoading = false;
        }

        private void SetActivePeriodTabFromUri()
        {
            var requested = QueryHelpers
                .ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query)
                .TryGetValue(PeriodTabQueryName, out var values)
                    ? values.FirstOrDefault()
                    : null;

            if (string.Equals(requested, PendingApprovalsTab, StringComparison.OrdinalIgnoreCase) && CanShowApprovals)
            {
                ActivePeriodTab = PendingApprovalsTab;
                return;
            }

            if (CanShowSettlement)
            {
                ActivePeriodTab = PeriodReviewTab;
                return;
            }

            ActivePeriodTab = CanShowApprovals ? PendingApprovalsTab : PeriodReviewTab;
        }

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
            if (CanShowApprovals)
            {
                await ReloadAsync();
            }
        }

        private async Task HandleApproveClick(VPP01_RequestHeaderResDTO order)
        {
            if (order == null || !CanApprove) return;

            var confirm = await DialogService.Confirm(
                Loc["ApproveOrderConfirm"],
                Loc["ApproveOrderTitle"],
                new ConfirmOptions() { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            if (!_processingOrderIds.Add(order.Id)) return;
            StateHasChanged();
            try
            {
                var request = new ApproveOrderReqDTO
                {
                    RowVersion = order.RowVersion,
                    IdempotencyKey = GetDecisionIdempotencyKey(order.Id, "approve")
                };
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/approve", request);
                Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["OrderApprovedSuccess"]);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                Toast.Notify(NotificationSeverity.Error, Loc["Error"], UiErrorMapper.GetMessage(ex, Loc, "ApproveOrderFailed"));
            }
            finally
            {
                _processingOrderIds.Remove(order.Id);
                StateHasChanged();
            }
        }

        private async Task HandleRejectClick(VPP01_RequestHeaderResDTO order)
        {
            if (order == null || !CanReject) return;

            var reason = await DialogService.OpenAsync<Dialog_RejectSupplement>(
                Loc["RejectOrderTitle"],
                options: new DialogOptions { Width = "min(560px, 96vw)", Resizable = false, Draggable = true });

            if (reason is not string rejectionReason || string.IsNullOrWhiteSpace(rejectionReason)) return;

            if (!_processingOrderIds.Add(order.Id)) return;
            StateHasChanged();
            try
            {
                var request = new RejectOrderReqDTO
                {
                    Reason = rejectionReason,
                    RowVersion = order.RowVersion,
                    IdempotencyKey = GetDecisionIdempotencyKey(order.Id, "reject")
                };
                await _apiServices.PostFromApiAsync<object>($"/api/VPPRequest/additional-orders/{order.Id}/reject", request);
                Toast.Notify(NotificationSeverity.Success, Loc["Success"], Loc["OrderRejectedSuccess"]);
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                Toast.Notify(NotificationSeverity.Error, Loc["Error"], UiErrorMapper.GetMessage(ex, Loc, "RejectOrderFailed"));
            }
            finally
            {
                _processingOrderIds.Remove(order.Id);
                StateHasChanged();
            }
        }

        private bool IsProcessing(Guid orderId) => _processingOrderIds.Contains(orderId);

        private string GetDecisionIdempotencyKey(Guid orderId, string action)
        {
            var key = (orderId, action);
            if (_decisionIdempotencyKeys.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var created = Guid.NewGuid().ToString("N");
            _decisionIdempotencyKeys[key] = created;
            return created;
        }
    }
}
