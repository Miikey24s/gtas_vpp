// PAGE LOGIC: VPPRequest/Tabs/Tab_Orders.razor.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components.Pages.VPPRequest.Components;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Features.Requests.State;
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
        // DEPENDENCIES: API, export, navigation và permission state của danh sách đơn.
        [Inject] public RequestsQueryClient Requests { get; set; } = default!;
        [Inject] public RequestsCommandClient Commands { get; set; } = default!;
        [Inject] public NavigationManager NavigationManager { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public RequestsExportClient Exports { get; set; } = default!;
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [SupplyParameterFromQuery(Name = "orderView")] public string? OrderViewQuery { get; set; }
        // periodId chỉ giữ tương thích cho liên kết cũ. Hai loại đơn dùng query riêng
        // để đổi tab không làm mất kỳ người dùng đã chọn ở tab còn lại.
        [SupplyParameterFromQuery(Name = "periodId")] public Guid? PeriodIdQuery { get; set; }
        [SupplyParameterFromQuery(Name = "regularPeriodId")] public Guid? RegularPeriodIdQuery { get; set; }
        [SupplyParameterFromQuery(Name = "supplementPeriodId")] public Guid? SupplementPeriodIdQuery { get; set; }

        // ORDER VIEWS: Kỳ được chọn riêng; selector ngang chỉ còn phân loại đơn.
        protected const int CurrentOrderViewIndex = 0;
        protected const int SupplementOrderViewIndex = 1;

        public List<VppRequestResDTO> ActiveOrders { get; set; } = new();
        public List<VppRequestResDTO> AdditionalOrders { get; set; } = new();

        // DATA STATE: Bật sẵn loading để render skeleton trong lúc dữ liệu đang tải.
        // Bật sẵn để lần render interactive đầu tiên hiển thị skeleton trong lúc
        // period-info + orders đang tải; nếu để false, story hiện thoáng qua ở
        // trạng thái rỗng (không đơn, không nút xuất) trước khi dữ liệu về.
        public bool IsLoading { get; set; } = true;
        public bool ViewerVisible { get; set; }
        public VppRequestResDTO? ViewingOrder { get; set; }
        public VppPeriodInfoResDTO? RegularPeriodInfo { get; set; }
        public VppPeriodInfoResDTO? SupplementPeriodInfo { get; set; }
        public VppPeriodInfoResDTO? PeriodInfo => OrderViewSelectedIndex == SupplementOrderViewIndex
            ? SupplementPeriodInfo
            : RegularPeriodInfo;
        private IReadOnlyList<VppOpenPeriodOptionResDTO> _availablePeriods = [];
        private readonly HashSet<Guid> _cancellingOrderIds = new();
        private readonly HashSet<Guid> _restoringOrderIds = new();
        private Guid? _selectedSupplementOrderId;
        protected int OrderViewSelectedIndex { get; set; }

        // PERIOD SUMMARY: Ngày/kỳ và các số liệu tóm tắt lấy từ PeriodInfo và danh sách đơn.
        // P1: Ngày của kỳ được suy ra từ PeriodInfo có thẩm quyền của BE, không dùng DateTime.Now.
        // Chỉ fallback về "tháng lịch hiện tại" trong lúc PeriodInfo đang tải;
        // không bao giờ dùng để điều khiển logic submit/edit vốn được BE kiểm tra.
        public DateTime CurrentOrderPeriodDate => PeriodInfo is { } p
            ? new DateTime(p.CurrentPeriodYear, p.CurrentPeriodMonth, 1)
            : new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private DateTime? RegularPeriodDate => ToPeriodDate(RegularPeriodInfo);
        private DateTime? SupplementPeriodDate => ToPeriodDate(SupplementPeriodInfo);

        public DateTime RegularDeadlineDate =>
            PeriodInfo?.DeadlineDate ?? CurrentOrderPeriodDate.AddMonths(1).AddDays(4);
        public DateTime SupplementDeadlineDate =>
            PeriodInfo?.SupplementApprovalDeadlineDate ?? CurrentOrderPeriodDate.AddMonths(1).AddDays(9);
        public string CurrentOrderPeriodText => DateFormatter.Format(CurrentOrderPeriodDate, DateFormatter.MonthYear);

        public string FormatDeadline(DateTime deadlineDate) =>
            DateFormatter.Format(deadlineDate, DateFormatter.LongDate);

        public string GetDeadlineStatusText(DateTime deadlineDate) => GetRemainingDeadlineDays(deadlineDate) switch
        {
            < 0 => Loc["DeadlinePassed"].Value,
            0 => Loc["DeadlineIsToday"].Value,
            var remainingDays => string.Format(Loc["RemainingDeadlineDaysFormat"], remainingDays)
        };

        public string GetDeadlineToneClass(DateTime deadlineDate) => GetRemainingDeadlineDays(deadlineDate) < 0
            ? "is-expired"
            : GetRemainingDeadlineDays(deadlineDate) <= 2
                ? "is-urgent"
                : string.Empty;

        // Cửa sổ bổ sung chỉ bắt đầu sau khi kỳ đóng. Trước mốc đó card phải
        // báo thời gian chờ mở, không được diễn đạt như thể người dùng đang còn hạn gửi.
        public string SupplementDeadlineStatusText => PeriodInfo?.PeriodState switch
        {
            "Open" => GetSupplementOpeningStatusText(RegularDeadlineDate),
            "SubmissionClosed" => GetDeadlineStatusText(SupplementDeadlineDate),
            "Pricing" or "Settled" => Loc["SupplementWindowClosed"].Value,
            _ => Loc["SupplementWindowNotOpen"].Value
        };

        public string SupplementDeadlineToneClass => PeriodInfo?.PeriodState == "SubmissionClosed"
            ? GetDeadlineToneClass(SupplementDeadlineDate)
            : string.Empty;

        private string GetSupplementOpeningStatusText(DateTime opensAt) => GetRemainingDeadlineDays(opensAt) switch
        {
            > 0 and var remainingDays => string.Format(
                Loc["SupplementOpensInDaysFormat"],
                remainingDays),
            0 => Loc["SupplementOpensToday"].Value,
            _ => Loc["SupplementWindowNotOpen"].Value
        };

        private static int GetRemainingDeadlineDays(DateTime deadlineDate) =>
            (deadlineDate.Date - DateTime.Today).Days;
        protected IReadOnlyList<VppDecisionOption<Guid?>> OpenPeriodOptions => ActivePeriodOptions
            .Select(option => new VppDecisionOption<Guid?>(
                option.PeriodId,
                FormatPeriodOption(option)))
            .ToArray() ?? [];
        public IReadOnlyList<VppRequestResDTO> CurrentPeriodAdditionalOrders => AdditionalOrders
            .Where(order => SupplementPeriodDate is { } period
                && order.Year == period.Year
                && order.Month == period.Month)
            .ToArray();
        protected VppRequestResDTO? CurrentRegularOrder => ActiveOrders.FirstOrDefault();
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
            new(CurrentOrderViewIndex, Loc["RegularOrder"]),
            new(SupplementOrderViewIndex, Loc["AdditionalOrders"])
        ];
        public string EmptyCurrentOrdersDescription => CanCreateRegular
            ? Loc["NoCurrentRegularOrdersNextStep"].Value
            : Loc["NoOrdersSubmittedCurrentPeriod"].Value;

        private bool CanView => PermissionState.HasPermission(Permissions.RequestViewOwn);
        private bool CanCreate => PermissionState.HasPermission(Permissions.RequestCreate);
        private bool CanUpdateOwnOrders => PermissionState.HasPermission(Permissions.RequestUpdateOwn);
        private bool CanCancelOwnOrders => PermissionState.HasPermission(Permissions.RequestCancelOwn);
        private bool CanCreateRegular => CanCreate && RegularPeriodInfo?.CanCreateOrder == true;
        private bool CanCreateSupplement => CanCreate && SupplementPeriodInfo?.CanCreateAdditional == true;
        private bool CanCopyPrevious => CanCreate && RegularPeriodInfo?.CanCopyPrevious == true;
        // Luôn cho người có quyền thấy CTA trong đúng tab; capability từ backend
        // quyết định enabled/disabled để người dùng hiểu vì sao chưa thể tạo.
        private bool ShowSupplementAction => CanCreate && SupplementPeriodInfo is not null;
        private string SupplementActionHint => CanCreateSupplement
            ? Loc["RequestAdditional"].Value
            : SupplementUnavailableDescription;
        private string SupplementUnavailableDescription
        {
            get
            {
                if (SupplementPeriodInfo is null)
                {
                    return Loc["SupplementUnavailable"].Value;
                }

                if (SupplementPeriodInfo.HasPendingAdditional)
                {
                    return Loc["SupplementPendingMustResolve"].Value;
                }

                if (SupplementPeriodInfo.RemainingApprovedSupplementQuota <= 0)
                {
                    return Loc["SupplementApprovedQuotaFull"].Value;
                }

                if (SupplementPeriodInfo.RemainingSupplementAttempts <= 0)
                {
                    return Loc["SupplementAttemptLimitFull"].Value;
                }

                return SupplementPeriodInfo.CanCreateAdditionalReason
                    ?? Loc["SupplementUnavailable"].Value;
            }
        }

        private static string FormatPeriodOption(VppOpenPeriodOptionResDTO option)
        {
            var period = DateFormatter.Format(
                new DateTime(option.Year, option.Month, 1),
                DateFormatter.MonthYear);
            return period;
        }

        private static bool IsRegularPeriod(VppOpenPeriodOptionResDTO option) =>
            string.Equals(option.State, "Open", StringComparison.OrdinalIgnoreCase);

        private static bool IsSupplementPeriod(VppOpenPeriodOptionResDTO option) =>
            option.State is not null
            && (option.State.Equals("SubmissionClosed", StringComparison.OrdinalIgnoreCase)
                || option.State.Equals("Pricing", StringComparison.OrdinalIgnoreCase)
                || option.State.Equals("Settled", StringComparison.OrdinalIgnoreCase));

        private static DateTime? ToPeriodDate(VppPeriodInfoResDTO? info) => info is null
            ? null
            : new DateTime(info.CurrentPeriodYear, info.CurrentPeriodMonth, 1);

        private async Task<VppPeriodInfoResDTO?> LoadSelectedPeriodInfoAsync(
            VppPeriodInfoResDTO? overview,
            Guid? selectedPeriodId)
        {
            if (selectedPeriodId is not Guid periodId)
            {
                return null;
            }

            return overview?.SelectedPeriodId == periodId
                ? overview
                : await Requests.GetPeriodInfoAsync(periodId);
        }

        private string BuildPeriodQueryUri(string orderView) =>
            NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
            {
                ["orderView"] = orderView,
                ["regularPeriodId"] = RegularPeriodInfo?.SelectedPeriodId,
                ["supplementPeriodId"] = SupplementPeriodInfo?.SelectedPeriodId,
                ["periodId"] = null
            });

        private IReadOnlyList<VppOpenPeriodOptionResDTO> ActivePeriodOptions =>
            OrderViewSelectedIndex == SupplementOrderViewIndex
                ? _availablePeriods.Where(IsSupplementPeriod).ToArray()
                : _availablePeriods.Where(IsRegularPeriod).ToArray();
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
        protected override void OnParametersSet()
        {
            OrderViewSelectedIndex = OrderViewQuery?.Trim().ToLowerInvariant() switch
            {
                "supplement" => SupplementOrderViewIndex,
                _ => CurrentOrderViewIndex
            };
        }

        protected Task SelectOrderViewAsync(int index)
        {
            var normalizedIndex = index switch
            {
                SupplementOrderViewIndex => SupplementOrderViewIndex,
                _ => CurrentOrderViewIndex
            };
            var queryValue = normalizedIndex switch
            {
                SupplementOrderViewIndex => "supplement",
                _ => "current"
            };

            OrderViewSelectedIndex = normalizedIndex;
            if (!string.Equals(OrderViewQuery, queryValue, StringComparison.OrdinalIgnoreCase))
            {
                var uri = BuildPeriodQueryUri(queryValue);
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
            // Tải cả hai context kỳ trước khi lấy đơn. Đơn thường mặc định kỳ đang mở,
            // đơn bổ sung mặc định kỳ trước gần nhất; backend vẫn quyết định kỳ đó
            // còn được tạo bổ sung hay chỉ được xem dữ liệu.
            await LoadPeriodInfosAsync();
            await LoadOrdersAsync();
        }

        private async Task LoadPeriodInfosAsync()
        {
            try
            {
                var overview = await Requests.GetPeriodInfoAsync();
                _availablePeriods = overview?.OpenPeriods ?? [];

                var legacyRegularId = OrderViewSelectedIndex == CurrentOrderViewIndex
                    ? PeriodIdQuery
                    : null;
                var legacySupplementId = OrderViewSelectedIndex == SupplementOrderViewIndex
                    ? PeriodIdQuery
                    : null;
                var selection = OrderPeriodSelectionPolicy.Select(
                    _availablePeriods,
                    RegularPeriodIdQuery ?? legacyRegularId,
                    SupplementPeriodIdQuery ?? legacySupplementId);

                RegularPeriodInfo = await LoadSelectedPeriodInfoAsync(overview, selection.RegularPeriodId);
                SupplementPeriodInfo = await LoadSelectedPeriodInfoAsync(overview, selection.SupplementPeriodId);
                RegularPeriodIdQuery = RegularPeriodInfo?.SelectedPeriodId;
                SupplementPeriodIdQuery = SupplementPeriodInfo?.SelectedPeriodId;
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
                var periods = new[] { RegularPeriodDate, SupplementPeriodDate }
                    .OfType<DateTime>()
                    .Select(period => new OrderPeriod(period.Year, period.Month))
                    .Distinct()
                    .ToArray();
                var batches = await Task.WhenAll(periods.Select(period =>
                    Requests.GetMyOrdersAsync([period])));
                var allOrders = batches
                    .SelectMany(batch => batch)
                    .GroupBy(order => order.Id)
                    .Select(group => group.First())
                    .OrderByDescending(order => order.UpdatedAtUtc)
                    .ToList();

                ActiveOrders = allOrders.Where(order =>
                        !order.IsAdditionalOrder
                        && RegularPeriodDate is { } period
                        && order.Year == period.Year
                        && order.Month == period.Month)
                    .ToList();
                AdditionalOrders = allOrders.Where(order => order.IsAdditionalOrder)
                    .OrderByDescending(order => order.SubmittedDate ?? order.UpdatedAtUtc)
                    .ToList();

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

        protected async Task ChangeSelectedPeriodAsync(Guid? selectedPeriodId)
        {
            if (selectedPeriodId is not Guid periodId)
            {
                return;
            }

            if (ActivePeriodOptions.All(option => option.PeriodId != periodId)
                || PeriodInfo?.SelectedPeriodId == periodId)
            {
                return;
            }

            var selectedInfo = await Requests.GetPeriodInfoAsync(periodId);
            if (OrderViewSelectedIndex == SupplementOrderViewIndex)
            {
                SupplementPeriodInfo = selectedInfo;
                SupplementPeriodIdQuery = periodId;
            }
            else
            {
                RegularPeriodInfo = selectedInfo;
                RegularPeriodIdQuery = periodId;
            }

            var uri = BuildPeriodQueryUri(OrderViewSelectedIndex == SupplementOrderViewIndex
                ? "supplement"
                : "current");
            NavigationManager.NavigateTo(uri, replace: true);
            await LoadOrdersAsync();
        }

        protected async Task GoToCreatePage(bool isAdditional = false)
        {
            if (isAdditional ? !CanCreateSupplement : !CanCreateRegular)
            {
                var reason = isAdditional
                    ? SupplementPeriodInfo?.CanCreateAdditionalReason
                    : RegularPeriodInfo?.CanCreateOrderReason;
                Toast.Warning(Loc["Order"], reason ?? Loc["RequestActionUnavailable"]);
                return;
            }

            var selectedPeriod = isAdditional ? SupplementPeriodInfo : RegularPeriodInfo;
            NavigationManager.NavigateTo($"/dashboard/order-create?isAdditional={isAdditional}&periodId={selectedPeriod?.SelectedPeriodId}");
            await Task.CompletedTask;
        }

        protected async Task CopyPreviousAsync()
        {
            if (!CanCreate || RegularPeriodInfo?.CanCopyPrevious != true)
            {
                return;
            }

            NavigationManager.NavigateTo($"/dashboard/order-create?copyFrom=previous&periodId={RegularPeriodInfo?.SelectedPeriodId}");
            await Task.CompletedTask;
        }

        protected async Task GoToEditPage(VppRequestResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?orderId={row.Id}&periodId={row.PeriodId}");
            await Task.CompletedTask;
        }

        private Task CreateRegularOrderAsync() => GoToCreatePage(false);

        private Task CreateSupplementOrderAsync() => GoToCreatePage(true);

        protected async Task GoToRecreatePage(VppRequestResDTO row)
        {
            NavigationManager.NavigateTo($"/dashboard/order-create?orderId={row.Id}&mode=recreate&periodId={row.PeriodId}");
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
                await LoadPeriodInfosAsync();
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
                await LoadPeriodInfosAsync();
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
