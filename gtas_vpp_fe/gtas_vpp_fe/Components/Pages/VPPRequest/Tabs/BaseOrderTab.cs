using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    /// <summary>
    /// Shared base class for the "list of VPP01 orders with paged grid + filter +
    /// lazy-loaded detail expansion" pattern (F-24).
    ///
    /// Originally duplicated across Tab_History, Tab_AllOrdersSummary,
    /// Tab_DepartmentSummary and Tab_AdminApproval. Each of those tabs now only
    /// owns its own filter state + endpoint-building + permission check, and
    /// reuses the shared loading / paging / expand / debounce plumbing here.
    ///
    /// Derived tabs:
    ///  - Must override <see cref="CanView"/>, <see cref="ErrorSummary"/>, <see cref="BuildEndpoint"/>.
    ///  - May override <see cref="OnInit"/> to seed filter options before first load.
    ///  - May call <see cref="LoadAsync"/> directly (e.g. after an admin action) to refresh.
    /// </summary>
    public abstract class BaseOrderTab : ComponentBase, IDisposable
    {
        // ─── Injected services (base-owned; derived razor/cs can use directly) ───
        [Inject] protected IAPIServices _apiServices { get; set; } = default!;
        [Inject] protected GlobalClass glb { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;

        // ─── Cascaded from the host page ─────────────────────────────────────
        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        // ─── Collection state ─────────────────────────────────────────────────
        public List<VPP01_RequestHeaderResDTO> Orders { get; set; } = new();
        protected HashSet<Guid> LoadedDetailOrderIds { get; } = new();
        protected HashSet<Guid> LoadingDetailOrderIds { get; } = new();

        // ─── UI state (public so razor templates can bind) ────────────────────
        public bool IsLoading { get; set; } = true;
        public bool IsGridLoading { get; set; }
        public int TotalCount { get; set; }
        public int TotalLines { get; set; }
        public int TotalQty { get; set; }
        public int PageSize { get; set; } = 20;
        public int CurrentSkip { get; set; }

        // ─── Private plumbing ─────────────────────────────────────────────────
        private CancellationTokenSource? _filterDebounce;
        private bool _isFirstLoad = true;

        // ─── Derived-class contract ───────────────────────────────────────────
        /// <summary>Returns true when the signed-in user can view this tab's data.</summary>
        protected abstract bool CanView { get; }

        /// <summary>Notification summary used when a load/expand call fails.</summary>
        protected abstract string ErrorSummary { get; }

        /// <summary>
        /// Builds the relative API URL for the current filter / paging values.
        /// Derived tabs read their own filter state + <see cref="CurrentSkip"/> +
        /// <see cref="PageSize"/> and return something like
        /// "/api/VPPRequest/my-orders-summary?year=2026&skip=0&top=20".
        /// </summary>
        protected abstract string BuildEndpoint();

        /// <summary>Hook invoked once before the first load. Use to seed filter option lists.</summary>
        protected virtual void OnInit() { }

        // ─── Lifecycle ────────────────────────────────────────────────────────
        protected override async Task OnInitializedAsync()
        {
            OnInit();
            await LoadAsync();
        }

        // ─── Public helpers used by razor templates ───────────────────────────
        /// <summary>
        /// Filter-change entry point for razor bindings. Debounces 300 ms,
        /// resets paging, then reloads.
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

        /// <summary>Grid's LoadData handler — wires Radzen paging into our skip/top.</summary>
        protected async Task OnLoadData(LoadDataArgs args)
        {
            CurrentSkip = args.Skip ?? 0;
            if (args.Top.HasValue && args.Top.Value > 0) PageSize = args.Top.Value;
            await LoadAsync();
        }

        /// <summary>Forces a reload (e.g. after approve / reject in admin tab).</summary>
        protected async Task ReloadAsync() => await LoadAsync();

        /// <summary>
        /// Shared load pipeline: permission gate → first-load vs. grid-refresh busy
        /// indicator → call API with stats → update totals → clear detail caches →
        /// log and notify on error.
        /// </summary>
        protected async Task LoadAsync()
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
                var endpoint = BuildEndpoint();
                var (data, totalCount, totalLines, totalQty) =
                    await _apiServices.GetFromApiWithStatsAsync<List<VPP01_RequestHeaderResDTO>>(endpoint);

                Orders = data ?? new();
                TotalCount = totalCount;
                TotalLines = totalLines;
                TotalQty = totalQty;

                LoadedDetailOrderIds.Clear();
                LoadingDetailOrderIds.Clear();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = ErrorSummary,
                    Detail = $"Load failed: {ex.Message}",
                    Duration = 6000
                });
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

        /// <summary>
        /// Expand-row handler — lazy-loads order detail items on first expansion,
        /// cached per row-id so repeated expand/collapse doesn't re-fetch.
        /// </summary>
        protected async Task OnRowExpandAsync(VPP01_RequestHeaderResDTO row)
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
                var detail = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>(
                    $"{Config.VppApi.Orders}/{row.Id}");
                row.Items = detail?.Items ?? new List<VPP02_RequestDetailResDTO>();
                LoadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = ErrorSummary,
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

        public void Dispose()
        {
            _filterDebounce?.Cancel();
            _filterDebounce?.Dispose();
        }
    }
}
