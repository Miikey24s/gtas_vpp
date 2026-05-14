using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.Components;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;
using System.Collections;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

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
        [Inject] protected IStringLocalizer<App> BaseLoc { get; set; } = default!;
        [Inject] protected NotificationService NotificationService { get; set; } = default!;
        [Inject] protected PermissionState PermissionState { get; set; } = default!;

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
        protected string? CurrentFilterExpression { get; private set; }
        protected string? CurrentOrderByExpression { get; private set; }
        protected IReadOnlyList<FilterDescriptor> CurrentFilters { get; private set; } = Array.Empty<FilterDescriptor>();

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

        /// <summary>Appends tab-specific scope values so filter popup data matches the current dataset.</summary>
        protected abstract void AppendFilterScopeQuery(List<string> query);

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
            CurrentFilterExpression = args.Filter;
            CurrentOrderByExpression = args.OrderBy;
            CurrentFilters = args.Filters?.ToList() ?? new List<FilterDescriptor>();
            await LoadAsync();
        }

        /// <summary>
        /// Loads distinct column values for CheckBoxList filters so they show
        /// all possible values across all pages, not just the current page.
        /// </summary>
        protected async Task OnLoadColumnFilterData(DataGridLoadColumnFilterDataEventArgs<VPP01_RequestHeaderResDTO> args)
        {
            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                if (string.IsNullOrWhiteSpace(property)) return;

                var query = new List<string>
                {
                    $"column={Uri.EscapeDataString(property)}"
                };
                AppendFilterScopeQuery(query);

                var scopedFilters = CurrentFilters
                    .Where(filter => !TargetsCurrentColumn(filter, property))
                    .Select(BuildColumnFilterScope)
                    .Where(filter => filter != null)
                    .Cast<ColumnFilterScope>()
                    .ToList();

                if (scopedFilters.Count > 0)
                {
                    query.Add($"filters={Uri.EscapeDataString(JsonSerializer.Serialize(scopedFilters))}");
                }
                else if (!string.IsNullOrWhiteSpace(CurrentFilterExpression))
                {
                    query.Add($"filter={Uri.EscapeDataString(CurrentFilterExpression)}");
                }

                if (!string.IsNullOrWhiteSpace(args.Filter))
                {
                    query.Add($"distinctFilter={Uri.EscapeDataString(args.Filter)}");
                }

                var apiUrl = $"/api/VPPRequest/order-filter-values?{string.Join("&", query)}";
                var response = await _apiServices.GetFromApiAsync<List<Dictionary<string, object?>>>(apiUrl);

                if (response != null)
                {
                    var distinctDtos = response
                        .Select(BuildFilterValueDto)
                        .ToList();

                    args.Data = distinctDtos;
                    args.Count = distinctDtos.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BaseOrderTab] LoadColumnFilterData failed: {ex.Message}");
            }
        }

        private static VPP01_RequestHeaderResDTO BuildFilterValueDto(Dictionary<string, object?> dict)
        {
            var dto = new VPP01_RequestHeaderResDTO();

            if (dict.TryGetValue("Y", out var yearVal) && int.TryParse(yearVal?.ToString(), out var year))
            {
                dto.Y = year;
            }

            if (dict.TryGetValue("M", out var monthVal) && int.TryParse(monthVal?.ToString(), out var month))
            {
                dto.M = month;
            }

            if (dict.TryGetValue("Status", out var statusVal) && int.TryParse(statusVal?.ToString(), out var status))
            {
                dto.Status = status;
            }

            if (dict.TryGetValue(nameof(VPP01_RequestHeaderResDTO.TotalLines), out var totalLinesVal) && int.TryParse(totalLinesVal?.ToString(), out var totalLines))
            {
                dto.TotalLines = totalLines;
            }

            if (dict.TryGetValue(nameof(VPP01_RequestHeaderResDTO.TotalQty), out var totalQtyVal) && int.TryParse(totalQtyVal?.ToString(), out var totalQty))
            {
                dto.TotalQty = totalQty;
            }

            if (dict.TryGetValue(nameof(VPP01_RequestHeaderResDTO.SubmittedDate), out var submittedDateVal)
                && DateTime.TryParse(submittedDateVal?.ToString(), out var submittedDate))
            {
                dto.SubmittedDate = submittedDate;
            }

            if (dict.TryGetValue("IsAdditionalOrder", out var additionalVal) && bool.TryParse(additionalVal?.ToString(), out var isAdditional))
            {
                dto.IsAdditionalOrder = isAdditional;
            }

            if (dict.TryGetValue("IsDeadlinePassed", out var deadlineVal) && bool.TryParse(deadlineVal?.ToString(), out var isDeadlinePassed))
            {
                dto.IsDeadlinePassed = isDeadlinePassed;
            }

            SetStringProperty(dict, dto, nameof(VPP01_RequestHeaderResDTO.VPPCode));
            SetStringProperty(dict, dto, nameof(VPP01_RequestHeaderResDTO.DepartmentCode));
            SetStringProperty(dict, dto, nameof(VPP01_RequestHeaderResDTO.MemberCompanyCode));
            SetStringProperty(dict, dto, nameof(VPP01_RequestHeaderResDTO.RequesterName));
            SetStringProperty(dict, dto, nameof(VPP01_RequestHeaderResDTO.Description));

            return dto;
        }

        private static void SetStringProperty(Dictionary<string, object?> dict, VPP01_RequestHeaderResDTO dto, string propertyName)
        {
            if (!dict.TryGetValue(propertyName, out var value) || value == null)
            {
                return;
            }

            var property = typeof(VPP01_RequestHeaderResDTO).GetProperty(propertyName);
            property?.SetValue(dto, value.ToString());
        }

        private static bool TargetsCurrentColumn(FilterDescriptor filter, string property)
        {
            return string.Equals(filter.FilterProperty, property, StringComparison.OrdinalIgnoreCase)
                || string.Equals(filter.Property, property, StringComparison.OrdinalIgnoreCase);
        }

        private static ColumnFilterScope? BuildColumnFilterScope(FilterDescriptor filter)
        {
            var property = !string.IsNullOrWhiteSpace(filter.FilterProperty)
                ? filter.FilterProperty
                : filter.Property;

            if (string.IsNullOrWhiteSpace(property))
            {
                return null;
            }

            var values = GetFilterValues(filter.FilterValue)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return values.Count == 0
                ? null
                : new ColumnFilterScope
                {
                    Property = property,
                    Values = values
                };
        }

        private static IEnumerable<string> GetFilterValues(object? filterValue)
        {
            if (filterValue is IEnumerable values && filterValue is not string)
            {
                foreach (var value in values.Cast<object?>())
                {
                    var formatted = FormatFilterValue(value);
                    if (!string.IsNullOrWhiteSpace(formatted))
                    {
                        yield return formatted;
                    }
                }

                yield break;
            }

            var singleValue = FormatFilterValue(filterValue);
            if (!string.IsNullOrWhiteSpace(singleValue))
            {
                yield return singleValue;
            }
        }

        private static string? FormatFilterValue(object? value)
        {
            return value switch
            {
                null => null,
                DateTime dateTime => dateTime.ToString(DateFormatter.LongDate, CultureInfo.GetCultureInfo("vi-VN")),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString(DateFormatter.LongDate, CultureInfo.GetCultureInfo("vi-VN")),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        private sealed class ColumnFilterScope
        {
            public string Property { get; set; } = string.Empty;
            public List<string> Values { get; set; } = new();
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
                    Detail = string.Format(BaseLoc["LoadFailedFormat"], ex.Message),
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
                    Detail = string.Format(BaseLoc["LoadDetailsFailedFormat"], ex.Message),
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

        protected bool HasDashboardPermission(string permission)
        {
            return PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, permission);
        }

        public void Dispose()
        {
            _filterDebounce?.Cancel();
            _filterDebounce?.Dispose();
        }
    }
}
