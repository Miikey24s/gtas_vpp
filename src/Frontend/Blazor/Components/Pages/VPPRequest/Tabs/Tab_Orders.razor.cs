using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components.Pages.VPPRequest.Components;
using gtas_vpp_fe.Features.Requests.Api;
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
        [Inject] public RequestsQueryClient Requests { get; set; } = default!;
        [Inject] public RequestsCommandClient Commands { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public RequestsExportClient Exports { get; set; } = default!;
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [SupplyParameterFromQuery(Name = "orderView")] public string? OrderViewQuery { get; set; }

        protected const int CurrentOrderViewIndex = 0;
        protected const int SupplementOrderViewIndex = 1;
        protected const int PreviousOrderViewIndex = 2;

        public List<VppRequestResDTO> ActiveOrders { get; set; } = new();
        public List<VppRequestResDTO> PreviousOrders { get; set; } = new();
        public List<VppRequestResDTO> AdditionalOrders { get; set; } = new();

        // Bật sẵn để lần render interactive đầu tiên hiển thị skeleton trong lúc
        // period-info + orders đang tải; nếu để false, story hiện thoáng qua ở
        // trạng thái rỗng (không đơn, không nút xuất) trước khi dữ liệu về.
        public bool IsLoading { get; set; } = true;
        public bool ViewerVisible { get; set; }
        public VppRequestResDTO? ViewingOrder { get; set; }
        public VppPeriodInfoResDTO? PeriodInfo { get; set; }
        private readonly HashSet<Guid> _cancellingOrderIds = new();
        private readonly HashSet<Guid> _restoringOrderIds = new();
        private Guid? _selectedSupplementOrderId;
        protected int OrderViewSelectedIndex { get; set; }

        // P1: Ngày của kỳ được suy ra từ PeriodInfo có thẩm quyền của BE, không dùng DateTime.Now.
        // Chỉ fallback về "tháng lịch hiện tại" trong lúc PeriodInfo đang tải;
        // không bao giờ dùng để điều khiển logic submit/edit vốn được BE kiểm tra.
        public DateTime CurrentOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.CurrentPeriodYear, p.CurrentPeriodMonth, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        public DateTime PreviousOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.PreviousPeriodYear, p.PreviousPeriodMonth, 1)
            : CurrentOrderPeriodDate.AddMonths(-1);

        public DateTime CurrentDeadlineDate => PeriodInfo?.DeadlineDate
            ?? new DateTime(CurrentOrderPeriodDate.Year, CurrentOrderPeriodDate.Month, 1).AddMonths(1).AddDays(4);

        public int RemainingDeadlineDays => Math.Max(0, (CurrentDeadlineDate.Date - DateTime.Today).Days);
        public string CurrentOrderPeriodText => DateFormatter.Format(CurrentOrderPeriodDate, DateFormatter.MonthYear);
        public string PreviousOrderPeriodText => DateFormatter.Format(PreviousOrderPeriodDate, DateFormatter.MonthYear);
        public string CurrentDeadlineText => DateFormatter.Format(CurrentDeadlineDate, DateFormatter.LongDate);
        public string RemainingDeadlineText => RemainingDeadlineDays == 0 ? Loc["DeadlineIsToday"].Value : string.Format(Loc["RemainingDeadlineDaysFormat"], RemainingDeadlineDays);
        public IReadOnlyList<VppRequestResDTO> CurrentPeriodAdditionalOrders => AdditionalOrders
            .Where(order => order.Year == CurrentOrderPeriodDate.Year && order.Month == CurrentOrderPeriodDate.Month)
            .ToArray();
        protected VppRequestResDTO? CurrentRegularOrder => ActiveOrders.FirstOrDefault();
        protected VppRequestResDTO? PreviousRegularOrder => PreviousOrders.FirstOrDefault();
        protected VppRequestResDTO? SelectedSupplementOrder => CurrentPeriodAdditionalOrders
            .FirstOrDefault(order => order.Id == _selectedSupplementOrderId)
            ?? CurrentPeriodAdditionalOrders.FirstOrDefault();
        public IEnumerable<VppRequestResDTO> CurrentPeriodOrders => ActiveOrders.Concat(CurrentPeriodAdditionalOrders);
        public int TotalOrders => CurrentPeriodOrders.Count();
        public int SupplementTotalLines => CurrentPeriodAdditionalOrders.Sum(order => order.Items?.Count ?? order.TotalLines);
        public int SupplementTotalQty => CurrentPeriodAdditionalOrders.Sum(order => order.Items?.Sum(item => item.Qty) ?? order.TotalQty);
        public string SupplementSectionDescription => CurrentPeriodAdditionalOrders.Count > 0
            ? string.Format(Loc["OrderItemsSummaryFormat"].Value, SupplementTotalLines, SupplementTotalQty)
            : CanCreateSupplement
                ? Loc["SupplementAvailableDescription"].Value
                : SupplementUnavailableDescription;

        protected IReadOnlyList<VppSegmentedOption<int>> OrderViewOptions =>
        [
            new(CurrentOrderViewIndex, Loc["CurrentRegularOrder"]),
            new(SupplementOrderViewIndex, Loc["AdditionalOrders"]),
            new(PreviousOrderViewIndex, Loc["PreviousOrderPeriod"])
        ];
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

                if (TotalOrders > 0)
                {
                    return string.Empty;
                }

                return RemainingDeadlineDays == 0
                    ? string.Format(Loc["OrdersStoryTodayDescriptionFormat"].Value, CurrentDeadlineText)
                    : string.Format(Loc["OrdersStoryOpenDescriptionFormat"].Value, RemainingDeadlineDays, CurrentDeadlineText);
            }
        }
        public string EmptyCurrentOrdersDescription => CanCreateRegular
            ? Loc["NoCurrentRegularOrdersNextStep"].Value
            : Loc["NoOrdersSubmittedCurrentPeriod"].Value;

        private bool CanView => PermissionState.HasPermission(Permissions.RequestViewOwn);
        private bool CanCreate => PermissionState.HasPermission(Permissions.RequestCreate);
        private bool CanCreateRegular => CanCreate && PeriodInfo?.CanCreateOrder == true;
        private bool CanCreateSupplement => CanCreate && PeriodInfo?.CanCreateAdditional == true;
        private bool CanCopyPrevious => CanCreate && PeriodInfo?.CanCopyPrevious == true;
        // Luôn cho người có quyền thấy CTA trong đúng tab; capability từ backend
        // quyết định enabled/disabled để người dùng hiểu vì sao chưa thể tạo.
        private bool ShowSupplementAction => CanCreate && PeriodInfo is not null;
        private string SupplementActionHint => CanCreateSupplement
            ? Loc["RequestAdditional"].Value
            : SupplementUnavailableDescription;
        private string SupplementUnavailableDescription
        {
            get
            {
                if (PeriodInfo is null)
                {
                    return Loc["SupplementUnavailable"].Value;
                }

                if (PeriodInfo.HasPendingAdditional)
                {
                    return Loc["SupplementPendingMustResolve"].Value;
                }

                if (PeriodInfo.RemainingApprovedSupplementQuota <= 0)
                {
                    return Loc["SupplementApprovedQuotaFull"].Value;
                }

                if (PeriodInfo.RemainingSupplementAttempts <= 0)
                {
                    return Loc["SupplementAttemptLimitFull"].Value;
                }

                if (!PeriodInfo.IsSubmissionOpen || PeriodInfo.IsDeadlinePassed)
                {
                    return Loc["SupplementSubmissionClosed"].Value;
                }

                return Loc["SupplementUnavailable"].Value;
            }
        }
        private string? CurrentEmptyActionText => CanCreateRegular
            ? Loc["CreateOrderThisCycle"].Value
            : CanCopyPrevious
                ? Loc["CopyPreviousOrder"].Value
                : null;
        private string? CurrentPrimaryActionText => CurrentRegularOrder is not null && CanCreateRegular
            ? Loc["CreateOrderThisCycle"].Value
            : null;
        private string CurrentPrimaryActionIcon => VppIcons.Add;
        private EventCallback CurrentPrimaryActionRequested => EventCallback.Factory.Create(this, CreateRegularOrderAsync);
        private EventCallback CurrentEmptyActionRequested => EventCallback.Factory.Create(
            this,
            CanCreateRegular ? CreateRegularOrderAsync : CopyPreviousAsync);
        private string? SupplementEmptyActionText => ShowSupplementAction
            ? Loc["RequestAdditional"].Value
            : null;
        private string? SupplementPrimaryActionText => SelectedSupplementOrder is not null && ShowSupplementAction
            ? Loc["RequestAdditional"].Value
            : null;
        private string? PreviousPrimaryActionText => PreviousRegularOrder is not null && CanCopyPrevious
            ? Loc["CopyPreviousOrder"].Value
            : null;

        protected override void OnParametersSet()
        {
            OrderViewSelectedIndex = OrderViewQuery?.Trim().ToLowerInvariant() switch
            {
                "supplement" => SupplementOrderViewIndex,
                "previous" => PreviousOrderViewIndex,
                _ => CurrentOrderViewIndex
            };
        }

        protected Task SelectOrderViewAsync(int index)
        {
            var normalizedIndex = index switch
            {
                SupplementOrderViewIndex => SupplementOrderViewIndex,
                PreviousOrderViewIndex => PreviousOrderViewIndex,
                _ => CurrentOrderViewIndex
            };
            var queryValue = normalizedIndex switch
            {
                SupplementOrderViewIndex => "supplement",
                PreviousOrderViewIndex => "previous",
                _ => "current"
            };

            OrderViewSelectedIndex = normalizedIndex;
            if (!string.Equals(OrderViewQuery, queryValue, StringComparison.OrdinalIgnoreCase))
            {
                var uri = NavigationManager.GetUriWithQueryParameter("orderView", queryValue);
                NavigationManager.NavigateTo(uri, replace: true);
            }

            return Task.CompletedTask;
        }

        protected Task SelectSupplementOrderAsync(Guid orderId)
        {
            if (CurrentPeriodAdditionalOrders.Any(order => order.Id == orderId))
            {
                _selectedSupplementOrderId = orderId;
            }

            return Task.CompletedTask;
        }

        private static string? GetBusinessNote(string? description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            var value = description.Trim();
            return value.StartsWith("Demo quantity inferred", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("Normalized from", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("QA-", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : value;
        }

        private static bool HasBusinessNotes(VppRequestResDTO order)
            => order.Items?.Any(item => GetBusinessNote(item.Description) is not null) == true;
        private bool CanUpdate(VppRequestResDTO row) =>
            row.CanEdit && PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanRestore(VppRequestResDTO row) =>
            row.CanRestore && PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanRecreate(VppRequestResDTO row) =>
            row.CanRecreate && PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanCancel(VppRequestResDTO row) =>
            row.CanCancel && PermissionState.HasPermission(Permissions.RequestCancelOwn);
        private bool IsRestoring(Guid orderId) => _restoringOrderIds.Contains(orderId);

        protected override async Task OnInitializedAsync()
        {
            // P1: Tải PeriodInfo TRƯỚC để ngày suy ra như CurrentOrderPeriodDate phản ánh
            // dữ liệu có thẩm quyền của BE trước khi badge/header render.
            await LoadPeriodInfoAsync();
            await LoadOrdersAsync();
        }

        private async Task LoadPeriodInfoAsync()
        {
            try
            {
                PeriodInfo = await Requests.GetPeriodInfoAsync();
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
            if (!CanView)
            {
                // Không giữ skeleton khi tab không được phép xem dữ liệu.
                IsLoading = false;
                return;
            }

            IsLoading = true;

            try
            {
                var data = await Requests.GetMyOrdersAsync(
                [
                    new OrderPeriod(CurrentOrderPeriodDate.Year, CurrentOrderPeriodDate.Month),
                    new OrderPeriod(PreviousOrderPeriodDate.Year, PreviousOrderPeriodDate.Month)
                ]);

                var allOrders = data.OrderByDescending(x => x.UpdatedAtUtc).ToList();

                // Tách đơn theo loại.
                ActiveOrders = allOrders.Where(x => !x.IsAdditionalOrder && x.Year == CurrentOrderPeriodDate.Year && x.Month == CurrentOrderPeriodDate.Month).ToList();
                PreviousOrders = allOrders.Where(x => !x.IsAdditionalOrder && x.Year == PreviousOrderPeriodDate.Year && x.Month == PreviousOrderPeriodDate.Month).ToList();
                AdditionalOrders = allOrders.Where(x => x.IsAdditionalOrder).OrderByDescending(x => x.SubmittedDate ?? x.UpdatedAtUtc).ToList();

                if (CurrentPeriodAdditionalOrders.All(order => order.Id != _selectedSupplementOrderId))
                {
                    _selectedSupplementOrderId = CurrentPeriodAdditionalOrders.FirstOrDefault()?.Id;
                }
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

        private Task CreateRegularOrderAsync() => GoToCreatePage(false);

        private Task CreateSupplementOrderAsync() => GoToCreatePage(true);

        protected async Task GoToRecreatePage(VppRequestResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?orderId={row.Id}&mode=recreate");
            await Task.CompletedTask;
        }

        protected VppFileExportFormat? ExportingOrderFormat { get; private set; }

        protected async Task ExportOrderAsync(VppRequestResDTO row, VppFileExportFormat format)
        {
            if (ExportingOrderFormat.HasValue) return;

            ExportingOrderFormat = format;
            try
            {
                var result = await Exports.ExportOrderAsync(row.Id, format);
                Toast.Success(Loc["Order"], Loc["ExportCompleted", result.FileName, FileSizeFormatter.Format(result.Size)]);
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc),
                    Duration = 6000
                });
            }
            finally
            {
                ExportingOrderFormat = null;
            }
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
                await Commands.CancelAsync(row.Id, request);
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["Order"],
                    Detail = Loc["OrderCancelledSuccess"],
                    Duration = 3000
                });
                await LoadPeriodInfoAsync();
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

        protected Task OpenHistoryAsync(VppRequestResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard?tab=1&orderId={row.Id:D}");
            return Task.CompletedTask;
        }

        protected async Task RestoreCancelledOrderAsync(VppRequestResDTO row)
        {
            if (!CanRestore(row) || !_restoringOrderIds.Add(row.Id)) return;

            var confirm = await DialogService.Confirm(
                string.Format(Loc["RestoreOrderConfirm"], row.VppCode),
                Loc["RestoreOrderTitle"],
                new ConfirmOptions
                {
                    OkButtonText = Loc["RestoreOrder"],
                    CancelButtonText = Loc["KeepCancelledOrder"]
                });
            if (confirm != true)
            {
                _restoringOrderIds.Remove(row.Id);
                return;
            }

            try
            {
                var request = new VppRequestRestoreReqDTO
                {
                    RowVersion = row.RowVersion,
                    IdempotencyKey = Guid.NewGuid().ToString("N")
                };
                await Commands.RestoreAsync(row.Id, request);
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["Order"],
                    Detail = Loc["OrderRestoredSuccess"],
                    Duration = 3000
                });
                await LoadPeriodInfoAsync();
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["Order"],
                    Detail = UiErrorMapper.GetMessage(ex, Loc, "RestoreFailed"),
                    Duration = 6000
                });
            }
            finally
            {
                _restoringOrderIds.Remove(row.Id);
            }
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

        // P4/F-16: Nhãn trạng thái và style badge lấy từ helper dùng chung
        // StatusDisplay trả semantic tone typed; route không tự map class hoặc Radzen badge style.

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
