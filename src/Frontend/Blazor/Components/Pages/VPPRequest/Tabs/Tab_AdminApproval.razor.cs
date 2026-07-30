using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components.DesignSystem.Composites;
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
        private const string PeriodDemandTab = "demand";
        private const string PeriodSupplyTab = "supply";
        private const string PeriodSettleTab = "settle";
        private const string PendingApprovalsTab = "pending";

        // Dialog service chỉ dành cho tab này, base không cần.
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private readonly HashSet<Guid> _processingOrderIds = new();
        private readonly Dictionary<(Guid OrderId, string Action), string> _decisionIdempotencyKeys = new();
        private string ActivePeriodTab { get; set; } = PeriodReviewTab;
        private bool _pendingOrdersLoaded;
        private bool _initialized;
        private VppRequestResDTO? SelectedPendingOrder { get; set; }
        private string PendingSearchText { get; set; } = string.Empty;
        private bool PendingHasFilters => !string.IsNullOrWhiteSpace(PendingSearchText);

        // Alias tiện ích để template Razor giữ tên PendingOrders hiện có.
        public List<VppRequestResDTO> PendingOrders => Orders;

        protected override bool CanView => HasDashboardPermission(Permissions.RequestAdminApproval)
            || HasDashboardPermission(Permissions.PeriodSettle);
        protected override string ErrorSummary => Loc["PeriodOperations"];

        private bool CanShowSettlement => PermissionState.HasPermission(Permissions.PeriodSettle);

        private bool CanShowApprovals => CanApprove || CanReject;
        private bool CanApprove => PermissionState.HasPermission(Permissions.RequestApprove);
        private bool CanReject => PermissionState.HasPermission(Permissions.RequestReject);

        private bool ShowSettlementWorkspace => CanShowSettlement && ActivePeriodTab != PendingApprovalsTab;

        private bool ShowApprovalsContent => CanShowApprovals && ActivePeriodTab == PendingApprovalsTab;

        private bool ShowPeriodNavigation => PeriodHeaderTabs.Count > 0;

        private string DefaultPeriodPath => CanShowSettlement
            ? "/dashboard?tab=5&periodTab=review"
            : "/dashboard?tab=5&periodTab=pending";

        private IReadOnlyList<VppHeaderSubTab> PeriodHeaderTabs
        {
            get
            {
                var tabs = new List<VppHeaderSubTab>();

                if (CanShowSettlement)
                {
                    tabs.Add(new(
                        Loc["PeriodSettleStep"],
                        "/dashboard?tab=5&periodTab=review",
                        ActivePeriodTab != PendingApprovalsTab));
                }

                if (CanShowApprovals)
                {
                    tabs.Add(new(
                        Loc["AdminApproval"],
                        "/dashboard?tab=5&periodTab=pending",
                        ActivePeriodTab == PendingApprovalsTab));
                }

                return tabs;
            }
        }

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

            if (CanShowSettlement && new[] { PeriodReviewTab, PeriodDemandTab, PeriodSupplyTab, PeriodSettleTab }
                .Contains(requested, StringComparer.OrdinalIgnoreCase))
            {
                // URL cũ vẫn hoạt động nhưng toàn bộ workflow đã hợp nhất vào màn Chốt kỳ.
                ActivePeriodTab = PeriodReviewTab;
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
            // Hàng chờ duyệt xếp đơn chờ lâu nhất lên đầu (Atlas supplement-approval);
            // người duyệt vẫn đổi được thứ tự bằng sort trên cột.
            var orderBy = string.IsNullOrWhiteSpace(CurrentOrderByExpression)
                ? "SubmittedDate asc"
                : CurrentOrderByExpression;
            query.Add($"orderby={Uri.EscapeDataString(orderBy)}");

            return $"/api/VPPRequest/additional-orders/pending?{string.Join("&", query)}";
        }

        protected override void AppendFilterScopeQuery(List<string> query)
        {
            query.Add("scope=pending");
        }

        private void NavigateToPeriodTab(string periodTab)
        {
            NavigationManager.NavigateTo($"/dashboard?tab=5&periodTab={periodTab}");
        }

        private async Task OnSettledRefresh()
        {
            if (CanShowApprovals)
            {
                await ReloadAsync();
            }
        }

        private async Task HandleApproveClick(VppRequestResDTO order)
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

        private async Task HandleRejectClick(VppRequestResDTO order)
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

        private async Task SelectPendingOrderAsync(VppRequestResDTO order)
        {
            SelectedPendingOrder = order;
            await OnRowExpandAsync(order);
        }

        private Task OnPendingSearchChangedAsync(ChangeEventArgs args)
        {
            PendingSearchText = args.Value?.ToString() ?? string.Empty;
            return ApplyManualFilterAsync(BuildPendingSearchFilter(PendingSearchText));
        }

        private Task ClearPendingFiltersAsync()
        {
            PendingSearchText = string.Empty;
            return ApplyManualFilterAsync(null, debounceMilliseconds: 0);
        }

        private static string? BuildPendingSearchFilter(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var escaped = value.Trim().ToLowerInvariant()
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            return $"((VppCode != null && VppCode.ToLower().Contains(\"{escaped}\")) || "
                + $"(RequesterName != null && RequesterName.ToLower().Contains(\"{escaped}\")) || "
                + $"(DepartmentCode != null && DepartmentCode.ToLower().Contains(\"{escaped}\")) || "
                + $"(Description != null && Description.ToLower().Contains(\"{escaped}\")))";
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
