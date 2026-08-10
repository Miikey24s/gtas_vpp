// PAGE LOGIC: VPPRequest/Tabs/BaseOrderTab.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.JSInterop;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    /// <summary>
    /// Base class dùng chung cho pattern "danh sách yêu cầu VPP có grid phân trang,
    /// bộ lọc và lazy-load phần chi tiết mở rộng" (F-24).
    ///
    /// Logic trước đây bị lặp ở các tab lịch sử và tổng hợp đơn,
    /// Tab_DepartmentSummary và Tab_AdminApproval. Mỗi tab hiện chỉ sở hữu state bộ lọc,
    /// cách dựng endpoint và kiểm tra quyền riêng; phần loading/paging/expand/debounce
    /// được tái sử dụng tại đây.
    ///
    /// Tab kế thừa:
    ///  - Bắt buộc override <see cref="CanView"/>, <see cref="ErrorSummary"/> và <see cref="QueryOrdersAsync"/>.
    ///  - Có thể override <see cref="OnInit"/> để seed tùy chọn lọc trước lần tải đầu.
    ///  - Có thể gọi trực tiếp <see cref="LoadAsync"/> để refresh, ví dụ sau action quản trị.
    /// </summary>
    public abstract class BaseOrderTab : ComponentBase, IDisposable
    {
        // ─── Service inject do base sở hữu; Razor/C# kế thừa được dùng trực tiếp ───
        [Inject] protected RequestsQueryClient Requests { get; set; } = default!;
        [Inject] protected IStringLocalizer<App> BaseLoc { get; set; } = default!;
        [Inject] protected IToastService Toast { get; set; } = default!;
        [Inject] protected RequestsExportClient Exports { get; set; } = default!;
        [Inject] protected PermissionState PermissionState { get; set; } = default!;
        [Inject] protected Microsoft.JSInterop.IJSRuntime JSRuntime { get; set; } = default!;

        // ─── Giá trị cascade từ page host ─────────────────────────────────────
        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        // ─── Tập trạng thái ───────────────────────────────────────────────────
        public List<VppRequestResDTO> Orders { get; set; } = new();
        protected HashSet<Guid> LoadedDetailOrderIds { get; } = new();
        protected HashSet<Guid> LoadingDetailOrderIds { get; } = new();

        // ─── State UI, public để template Razor có thể bind ───────────────────
        public bool IsLoading { get; set; } = true;
        public bool IsGridLoading { get; set; }
        public int TotalCount { get; set; }
        public int TotalLines { get; set; }
        public int TotalQty { get; set; }
        public int PageSize { get; set; } = VppPagingProfiles.SplitList.DefaultPageSize;
        public int CurrentSkip { get; set; }
        protected bool HasGridLoadError { get; private set; }
        protected string? CurrentFilterExpression { get; private set; }
        protected string? CurrentOrderByExpression { get; private set; }
        protected IReadOnlyList<FilterDescriptor> CurrentFilters { get; private set; } = Array.Empty<FilterDescriptor>();

        // ─── Hạ tầng private ──────────────────────────────────────────────────
        private CancellationTokenSource? _filterDebounce;
        private bool _isFirstLoad = true;

        // ─── Contract cho class kế thừa ───────────────────────────────────────
        /// <summary>Trả về true khi user đã đăng nhập được xem dữ liệu của tab.</summary>
        protected abstract bool CanView { get; }

        /// <summary>Tóm tắt notification khi lời gọi load/expand thất bại.</summary>
        protected abstract string ErrorSummary { get; }

        /// <summary>Tải collection theo query typed do tab kế thừa cung cấp.</summary>
        protected abstract Task<RequestStatsPage<VppRequestResDTO>> QueryOrdersAsync();

        /// <summary>Scope giúp popup filter đọc đúng dataset của tab hiện tại.</summary>
        protected abstract OrderFilterScope FilterScope { get; }

        protected virtual int? FilterFromPeriod => null;
        protected virtual int? FilterToPeriod => null;

        /// <summary>Hook gọi một lần trước lần tải đầu; dùng để seed danh sách tùy chọn lọc.</summary>
        protected virtual void OnInit() { }

        // ─── Vòng đời ────────────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            OnInit();
            await LoadAsync();
        }

        // ─── Helper public dùng bởi template Razor ────────────────────────────
        /// <summary>
        /// Entry point thay đổi bộ lọc cho binding Razor. Debounce 300 ms,
        /// reset paging rồi tải lại.
        /// </summary>
        protected async Task OnFilterChanged()
        {
            _filterDebounce?.Cancel();
            _filterDebounce = new CancellationTokenSource();
            var token = _filterDebounce.Token;

            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    CurrentSkip = 0;
                    await LoadAsync();
                }
            }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// Áp dụng filter do toolbar canonical sở hữu thay vì filter row của DataGrid.
        /// Filter vẫn được gửi server trước Count/Skip/Take.
        /// </summary>
        protected async Task ApplyManualFilterAsync(string? filterExpression, int debounceMilliseconds = 300)
        {
            _filterDebounce?.Cancel();
            _filterDebounce?.Dispose();
            _filterDebounce = new CancellationTokenSource();
            var token = _filterDebounce.Token;

            try
            {
                if (debounceMilliseconds > 0)
                {
                    await Task.Delay(debounceMilliseconds, token);
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }

                CurrentFilterExpression = filterExpression;
                CurrentFilters = Array.Empty<FilterDescriptor>();
                CurrentSkip = 0;
                await LoadAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }

        /// <summary>Handler LoadData của grid, nối paging Radzen với skip/top.</summary>
        protected async Task OnLoadData(LoadDataArgs args)
        {
            CurrentSkip = args.Skip ?? 0;
            if (args.Top.HasValue && args.Top.Value > 0) PageSize = args.Top.Value;
            CurrentFilterExpression = args.Filter;
            CurrentOrderByExpression = args.OrderBy;
            CurrentFilters = args.Filters?.ToList() ?? new List<FilterDescriptor>();
            await LoadAsync();
        }

        /// <summary>
        /// Tải giá trị distinct của cột cho CheckBoxList filter để hiển thị mọi giá trị
        /// trên tất cả page, không chỉ page hiện tại.
        /// </summary>
        protected async Task OnLoadColumnFilterData(DataGridLoadColumnFilterDataEventArgs<VppRequestResDTO> args)
        {
            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                if (string.IsNullOrWhiteSpace(property)) return;

                var scopedFilters = VppOrderGridFilterHelper.BuildColumnFilterScopes(CurrentFilters, property);
                var response = await Requests.GetOrderFilterValuesAsync(new OrderFilterValuesQuery(
                    property,
                    FilterScope,
                    FilterFromPeriod,
                    FilterToPeriod,
                    scopedFilters.Count > 0 ? JsonSerializer.Serialize(scopedFilters) : null,
                    scopedFilters.Count == 0 ? CurrentFilterExpression : null,
                    args.Filter));
                var distinctDtos = response
                    .Select(VppOrderGridFilterHelper.BuildFilterValueDto)
                    .ToList();

                args.Data = distinctDtos;
                args.Count = distinctDtos.Count;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BaseOrderTab] LoadColumnFilterData failed: {ex.Message}");
            }
        }

        /// <summary>Buộc tải lại, ví dụ sau khi duyệt/từ chối trong tab quản trị.</summary>
        protected async Task ReloadAsync() => await LoadAsync();

        /// <summary>
        /// Pipeline tải dùng chung: gate quyền → trạng thái bận lần đầu/refresh grid →
        /// gọi API kèm thống kê → cập nhật tổng → xóa cache chi tiết → log và báo lỗi.
        /// </summary>
        protected async Task LoadAsync()
        {
            if (!CanView) return;

            if (_isFirstLoad)
            {
                IsLoading = true;
            }
            else
            {
                IsGridLoading = true;
            }

            HasGridLoadError = false;

            // Render trạng thái bận trước khi await network để khi đổi bộ lọc vẫn giữ
            // nội dung hiện có và hiển thị tiến trình local ngay lập tức.
            await InvokeAsync(StateHasChanged);

            try
            {
                var result = await QueryOrdersAsync();

                Orders = result.Items.ToList();
                TotalCount = result.TotalCount;
                TotalLines = result.TotalLines;
                TotalQty = result.TotalQuantity;

                LoadedDetailOrderIds.Clear();
                LoadingDetailOrderIds.Clear();
            }
            catch (Exception ex)
            {
                HasGridLoadError = true;
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = ErrorSummary,
                    Detail = UiErrorMapper.GetMessage(ex, BaseLoc, "LoadFailed"),
                    Duration = 6000
                });
            }
            finally
            {
                if (_isFirstLoad)
                {
                    _isFirstLoad = false;
                }
                IsLoading = false;
                IsGridLoading = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Handler mở rộng dòng: lazy-load chi tiết đơn ở lần mở đầu tiên và cache theo
        /// row ID để đóng/mở lặp lại không fetch lại.
        /// </summary>
        protected async Task OnRowExpandAsync(VppRequestResDTO row)
        {
            if (row == null || row.Id == Guid.Empty
                || LoadedDetailOrderIds.Contains(row.Id)
                || LoadingDetailOrderIds.Contains(row.Id))
            {
                return;
            }

            LoadingDetailOrderIds.Add(row.Id);
            try
            {
                var detail = await Requests.GetOrderAsync(row.Id);
                row.Items = detail?.Items ?? new List<VppRequestDetailResDTO>();
                LoadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = ErrorSummary,
                    Detail = UiErrorMapper.GetMessage(ex, BaseLoc, "LoadDetailsFailed"),
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

        // ─── Xuất phiếu theo đơn (D10): GET /orders/{id}/export.pdf|xlsx ──────
        protected VppFileExportFormat? ExportingOrderFormat { get; private set; }

        protected async Task ExportOrderAsync(VppRequestResDTO row, VppFileExportFormat format)
        {
            if (ExportingOrderFormat.HasValue) return;

            ExportingOrderFormat = format;
            try
            {
                var result = await Exports.ExportOrderAsync(row.Id, format);
                Toast.Success(BaseLoc["Order"], BaseLoc["ExportCompleted", result.FileName, FileSizeFormatter.Format(result.Size)]);
            }
            catch (Exception ex)
            {
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = BaseLoc["Order"],
                    Detail = UiErrorMapper.GetMessage(ex, BaseLoc),
                    Duration = 6000
                });
            }
            finally
            {
                ExportingOrderFormat = null;
                StateHasChanged();
            }
        }

        protected bool HasDashboardPermission(string permission)
        {
            return PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, permission);
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

        public void Dispose()
        {
            _filterDebounce?.Cancel();
            _filterDebounce?.Dispose();
        }
    }
}
