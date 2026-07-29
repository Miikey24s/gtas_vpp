using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
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
        protected int CurrentRegularLineCount => GetLineCount(CurrentRegularOrder);
        protected int SelectedSupplementLineCount => GetLineCount(SelectedSupplementOrder);
        protected int PreviousOrderLineCount => GetLineCount(PreviousRegularOrder);
        public IEnumerable<VppRequestResDTO> CurrentPeriodOrders => ActiveOrders.Concat(CurrentPeriodAdditionalOrders);
        public int TotalOrders => CurrentPeriodOrders.Count();
        public int SupplementTotalLines => CurrentPeriodAdditionalOrders.Sum(order => order.Items?.Count ?? order.TotalLines);
        public int SupplementTotalQty => CurrentPeriodAdditionalOrders.Sum(order => order.Items?.Sum(item => item.Qty) ?? order.TotalQty);
        public string SupplementSectionDescription => CurrentPeriodAdditionalOrders.Count > 0
            ? string.Format(Loc["OrderItemsSummaryFormat"].Value, SupplementTotalLines, SupplementTotalQty)
            : CanCreateSupplement
                ? Loc["SupplementAvailableDescription"].Value
                : Loc["SupplementUnavailable"].Value;

        protected IReadOnlyList<VppSegmentedOption<int>> OrderViewOptions =>
        [
            new(CurrentOrderViewIndex, Loc["CurrentRegularOrder"], Badge: CurrentRegularLineCount.ToString()),
            new(SupplementOrderViewIndex, Loc["AdditionalOrders"], Badge: SelectedSupplementLineCount.ToString()),
            new(PreviousOrderViewIndex, Loc["PreviousOrderPeriod"], Badge: PreviousOrderLineCount.ToString())
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
        private bool ShowSupplementAction => CanCreate && PeriodInfo is { HasCurrentPeriodOrder: true, MaxAdditionalOrders: > 0 };
        private string SupplementActionHint => CanCreateSupplement
            ? Loc["RequestAdditional"].Value
            : Loc["SupplementUnavailable"].Value;

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

        private static int GetLineCount(VppRequestResDTO? order)
            => order?.Items?.Count ?? order?.TotalLines ?? 0;

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
        private bool CanReplace(VppRequestResDTO row) =>
            row.CanReplace && PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanCancel(VppRequestResDTO row) =>
            row.CanCancel && PermissionState.HasPermission(Permissions.RequestCancelOwn);

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
            if (!CanView)
            {
                // Không giữ skeleton khi tab không được phép xem dữ liệu.
                IsLoading = false;
                return;
            }

            IsLoading = true;

            try
            {
                var endpoint = $"{Config.VppApi.MyOrders}?years={CurrentOrderPeriodDate.Year}&months={CurrentOrderPeriodDate.Month}&years={PreviousOrderPeriodDate.Year}&months={PreviousOrderPeriodDate.Month}";
                var data = await _apiServices.GetFromApiAsync<List<VppRequestResDTO>>(endpoint);

                var allOrders = (data ?? new()).OrderByDescending(x => x.UpdatedAtUtc).ToList();

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

        protected bool IsExportingOrder { get; private set; }

        protected Task ExportOrderPdfAsync(VppRequestResDTO row) => ExportOrderAsync(row, "export.pdf");

        protected Task ExportOrderExcelAsync(VppRequestResDTO row) => ExportOrderAsync(row, "export.xlsx");

        private async Task ExportOrderAsync(VppRequestResDTO row, string format)
        {
            if (IsExportingOrder) return;

            IsExportingOrder = true;
            try
            {
                var file = await _apiServices.GetFileFromApiAsync(
                    $"{Config.VppApi.Orders}/{row.Id}/{format}");
                await using var stream = new MemoryStream(file.Content, writable: false);
                using var streamReference = new DotNetStreamReference(stream);
                await JSRuntime.InvokeVoidAsync("vppDownload.fromStream", file.FileName, streamReference);
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = Loc["Order"],
                    Detail = Loc["OrderExported"],
                    Duration = 3000
                });
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
                IsExportingOrder = false;
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

        // P4/F-16: Nhãn trạng thái và style badge lấy từ helper dùng chung
        // StatusDisplay/StatusDisplayRadzen; switch cục bộ đã được gỡ.

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
