using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components.Shared;
using gtas_vpp_fe.Components.Pages.VPPRequest.Components;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_Orders
    {
        public sealed class ProductOption
        {
            public Guid Id { get; set; }
            public string? VppCode { get; set; }
            public string? VppName { get; set; }
            public string Display => $"{VppCode} - {VppName}";
        }

        public sealed class EditOrderItem
        {
            public Guid VppId { get; set; }
            public int Qty { get; set; } = 1;
            public string? Description { get; set; }
        }

        public sealed class EditOrderModel
        {
            public Guid Id { get; set; }
            public int Year { get; set; }
            public int Month { get; set; }
            public string? Description { get; set; }
            public List<EditOrderItem> Items { get; set; } = new();
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        public List<VppRequestResDTO> ActiveOrders { get; set; } = new();
        public List<VppRequestResDTO> PreviousOrders { get; set; } = new();
        public List<VppRequestResDTO> AdditionalOrders { get; set; } = new();

        public bool IsLoading { get; set; }
        public bool ViewerVisible { get; set; }
        public VppRequestResDTO? ViewingOrder { get; set; }
        public VppPeriodInfoResDTO? PeriodInfo { get; set; }
        private readonly HashSet<Guid> _cancellingOrderIds = new();

        // P1: Period dates are derived from PeriodInfo (BE truth) — never DateTime.Now.
        // Fallback to "current calendar month" only while PeriodInfo is still loading,
        // never to drive submit/edit logic (which is BE-validated anyway).
        public DateTime CurrentOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.CurrentPeriodYear, p.CurrentPeriodMonth, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public DateTime PreviousOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.PreviousPeriodYear, p.PreviousPeriodMonth, 1)
            : CurrentOrderPeriodDate.AddMonths(-1);

        public DateTime CurrentDeadlineDate => PeriodInfo?.DeadlineDate
            ?? new DateTime(CurrentOrderPeriodDate.Year, CurrentOrderPeriodDate.Month, 1).AddMonths(1).AddDays(4);

        // Period end = day before the deadline. F-29: all date strings go through DateFormatter.
        public DateTime PeriodEndDate => CurrentDeadlineDate.AddDays(-1);
        public string PeriodEndText => DateFormatter.Format(PeriodEndDate, DateFormatter.ShortDate);
        public int RemainingDeadlineDays => Math.Max(0, (CurrentDeadlineDate.Date - DateTime.Today).Days);
        public string CurrentOrderPeriodText => DateFormatter.Format(CurrentOrderPeriodDate, DateFormatter.MonthYear);
        public string PreviousOrderPeriodText => DateFormatter.Format(PreviousOrderPeriodDate, DateFormatter.MonthYear);
        public string CurrentDeadlineText => DateFormatter.Format(CurrentDeadlineDate, DateFormatter.LongDate);
        public string RemainingDeadlineText => RemainingDeadlineDays == 0 ? Loc["DeadlineIsToday"].Value : string.Format(Loc["RemainingDeadlineDaysFormat"], RemainingDeadlineDays);
        public IReadOnlyList<VppRequestResDTO> CurrentPeriodAdditionalOrders => AdditionalOrders
            .Where(order => order.Year == CurrentOrderPeriodDate.Year && order.Month == CurrentOrderPeriodDate.Month)
            .ToArray();
        public IEnumerable<VppRequestResDTO> CurrentPeriodOrders => ActiveOrders.Concat(CurrentPeriodAdditionalOrders);
        public int TotalOrders => CurrentPeriodOrders.Count();
        public string TotalOrdersText => TotalOrders.ToString();
        public int TotalLines => CurrentPeriodOrders.Sum(order => order.Items?.Count ?? order.TotalLines);
        public string TotalLinesText => TotalLines.ToString();
        public int TotalQty => CurrentPeriodOrders.Sum(order => order.Items?.Sum(item => item.Qty) ?? order.TotalQty);
        public string TotalQtyText => TotalQty.ToString();
        public string OrdersStoryTitle => TotalOrders == 0
            ? Loc["OrdersStoryEmptyTitle"].Value
            : Loc["OrdersStoryActiveTitle"].Value;
        public string OrdersStoryDescription
        {
            get
            {
                if (PeriodInfo is null)
                {
                    return Loc["CouldNotDetermineCurrentPeriodPleaseReload"].Value;
                }

                if (!PeriodInfo.IsSubmissionOpen || PeriodInfo.IsDeadlinePassed)
                {
                    return Loc["OrdersStoryClosedDescription"].Value;
                }

                return RemainingDeadlineDays == 0
                    ? string.Format(Loc["OrdersStoryTodayDescriptionFormat"].Value, CurrentDeadlineText)
                    : string.Format(Loc["OrdersStoryOpenDescriptionFormat"].Value, RemainingDeadlineDays, CurrentDeadlineText);
            }
        }
        public string SubmissionStateText => PeriodInfo switch
        {
            null => Loc["StatusUnknown"].Value,
            { IsSubmissionOpen: true, IsDeadlinePassed: false } => Loc["OrderPeriodOpen"].Value,
            _ => Loc["OrderPeriodClosed"].Value
        };
        public string SubmissionStateCss => PeriodInfo switch
        {
            null => "is-unknown",
            { IsSubmissionOpen: true, IsDeadlinePassed: false } => "is-open",
            _ => "is-closed"
        };
        public string SubmissionStateIcon => PeriodInfo switch
        {
            null => VppIcons.Info,
            { IsSubmissionOpen: true, IsDeadlinePassed: false } => VppIcons.Approved,
            _ => VppIcons.Warning
        };
        public string EmptyCurrentOrdersDescription => CanCreateRegular
            ? Loc["NoCurrentRegularOrdersNextStep"].Value
            : Loc["NoOrdersSubmittedCurrentPeriod"].Value;

        private bool CanView => PermissionState.HasPermission(Permissions.RequestViewOwn);
        private bool CanCreate => PermissionState.HasPermission(Permissions.RequestCreate);
        private bool CanCreateRegular => CanCreate && PeriodInfo?.CanCreateOrder == true;
        private bool CanCreateSupplement => CanCreate && PeriodInfo?.CanCreateAdditional == true;
        private bool CanCopyPrevious => CanCreate && PeriodInfo?.CanCopyPrevious == true;
        private bool ShowSupplementAction => CanCreate && PeriodInfo is { HasCurrentPeriodOrder: true, MaxAdditionalOrders: > 0 };
        private bool HasAvailableStoryAction => CanCreateRegular || CanCopyPrevious || ShowSupplementAction;
        private string SupplementActionHint => CanCreateSupplement
            ? Loc["RequestAdditional"].Value
            : PeriodInfo?.CanCreateAdditionalReason ?? Loc["SupplementUnavailable"].Value;
        private bool CanUpdate(VppRequestResDTO row) =>
            row.CanEdit && PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanReplace(VppRequestResDTO row) =>
            row.CanReplace && PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanCancel(VppRequestResDTO row) =>
            row.CanCancel && PermissionState.HasPermission(Permissions.RequestCancelOwn);

        protected override async Task OnInitializedAsync()
        {
            // P1: Load PeriodInfo FIRST so derived dates (CurrentOrderPeriodDate etc.)
            // reflect BE truth before badges/headers render.
            await LoadPeriodInfoAsync();
            await LoadOrdersAsync();
        }

        private async Task LoadPeriodInfoAsync()
        {
            try
            {
                PeriodInfo = await _apiServices.GetFromApiAsync<VppPeriodInfoResDTO>($"{Config.VppApi.ApiVppBase}/period-info");
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Period"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc),
                    Duration = 5000
                });
            }
        }
        protected async Task LoadOrdersAsync()
        {
            if (!CanView) return;

            IsLoading = true;

            try
            {
                var endpoint = $"{Config.VppApi.MyOrders}?years={CurrentOrderPeriodDate.Year}&months={CurrentOrderPeriodDate.Month}&years={PreviousOrderPeriodDate.Year}&months={PreviousOrderPeriodDate.Month}";
                var data = await _apiServices.GetFromApiAsync<List<VppRequestResDTO>>(endpoint);

                var allOrders = (data ?? new()).OrderByDescending(x => x.UpdatedAtUtc).ToList();
                
                // Separate orders by type
                ActiveOrders = allOrders.Where(x => !x.IsAdditionalOrder && x.Year == CurrentOrderPeriodDate.Year && x.Month == CurrentOrderPeriodDate.Month).ToList();
                PreviousOrders = allOrders.Where(x => !x.IsAdditionalOrder && x.Year == PreviousOrderPeriodDate.Year && x.Month == PreviousOrderPeriodDate.Month).ToList();
                AdditionalOrders = allOrders.Where(x => x.IsAdditionalOrder).OrderByDescending(x => x.SubmittedDate ?? x.UpdatedAtUtc).ToList();
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Orders"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc, "LoadOrdersFailed"),
                    Duration = 6000
                });
            }
            finally
            {
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task ReloadAsync() => await LoadOrdersAsync();

        protected async Task GoToCreatePage(bool isAdditional = false)
        {
            if (isAdditional ? !CanCreateSupplement : !CanCreateRegular)
            {
                var reason = isAdditional
                    ? PeriodInfo?.CanCreateAdditionalReason
                    : PeriodInfo?.CanCreateOrderReason;
                Toast.Warning(Loc["Order"], reason ?? Loc["RequestActionUnavailable"]);
                return;
            }

            NavigationManager.NavigateTo($"/dashboard/order-create?isAdditional={isAdditional}");
            await Task.CompletedTask;
        }

        protected async Task CopyPreviousAsync()
        {
            if (!CanCreate || PeriodInfo?.CanCopyPrevious != true)
            {
                return;
            }

            NavigationManager.NavigateTo("/dashboard/order-create?copyFrom=previous");
            await Task.CompletedTask;
        }

        protected async Task GoToEditPage(VppRequestResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?orderId={row.Id}");
            await Task.CompletedTask;
        }

        protected async Task CancelOrderAsync(VppRequestResDTO row)
        {
            if (!CanCancel(row) || !_cancellingOrderIds.Add(row.Id)) return;

            var confirm = await DialogService.Confirm(
                string.Format(Loc["CancelOrderConfirm"], row.VppCode),
                Loc["CancelOrderTitle"],
                new ConfirmOptions { OkButtonText = Loc["ConfirmCancel"], CancelButtonText = Loc["KeepOrder"] });

            if (confirm != true)
            {
                _cancellingOrderIds.Remove(row.Id);
                return;
            }

            try
            {
                var request = new VppRequestCancelReqDTO
                {
                    RowVersion = row.RowVersion,
                    IdempotencyKey = Guid.NewGuid().ToString("N")
                };
                await _apiServices.PostFromApiAsync<object>($"{Config.VppApi.Orders}/{row.Id}/cancel", request);
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["Order"],
                    Detail = Loc["OrderCancelledSuccess"],
                    Duration = 3000
                });
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc, "CancelFailed"),
                    Duration = 6000
                });
            }
            finally
            {
                _cancellingOrderIds.Remove(row.Id);
                StateHasChanged();
            }
        }

        protected bool IsCancelling(Guid orderId) => _cancellingOrderIds.Contains(orderId);

        protected async Task OpenHistoryAsync(VppRequestResDTO row)
        {
            await DialogService.OpenAsync<Dialog_RequestHistory>(
                Loc["RequestLifecycle"],
                new Dictionary<string, object?> { [nameof(Dialog_RequestHistory.RequestId)] = row.Id },
                new DialogOptions { Width = "min(760px, 96vw)", Resizable = true, Draggable = true });
        }

        protected bool IsSubmitted(VppRequestResDTO row) => row.Status == 1;
        protected bool CanEditOrDelete(VppRequestResDTO row) => CanUpdate(row) || CanCancel(row);

        protected void ViewOrder(VppRequestResDTO row)
        {
            ViewingOrder = row;
            ViewerVisible = true;
            StateHasChanged();
        }

        protected void CloseViewer()
        {
            ViewerVisible = false;
            ViewingOrder = null;
        }

        // P4/F-16: Status label + badge style now come from the shared
        // StatusDisplay / StatusDisplayRadzen helpers. Local switch removed.

        public HashSet<Guid> ExpandedOrderIds { get; set; } = new();

        public void ToggleOrderCode(Guid orderId)
        {
            if (ExpandedOrderIds.Contains(orderId))
                ExpandedOrderIds.Remove(orderId);
            else
                ExpandedOrderIds.Add(orderId);
        }

        public string GetShortCode(VppRequestResDTO order)
        {
            var code = order.VppCode;
            if (string.IsNullOrEmpty(code)) return "";
            var parts = code.Split('-');
            if (parts.Length >= 2)
            {
                if (order.IsAdditionalOrder)
                {
                    return $"{parts[0]}-ADD-{parts[1]}";
                }
                return $"{parts[0]}-{parts[1]}";
            }
            return code.Length > 10 ? code.Substring(0, 10) : code;
        }

        public async Task CopyToClipboard(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;
            try
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
                var isVi = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = isVi ? "Đã sao chép" : "Copied",
                    Detail = isVi ? $"Đã sao chép mã đơn hàng: {text}!" : $"Copied order code: {text}!",
                    Duration = 4000
                });
            }
            catch (Exception) { }
        }
    }
}
