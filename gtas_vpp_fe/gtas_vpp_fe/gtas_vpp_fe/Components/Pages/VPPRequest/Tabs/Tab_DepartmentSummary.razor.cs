using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_DepartmentSummary : BaseOrderTab, IAsyncDisposable
    {
        public sealed class OptionItem
        {
            public int? Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        public int? YearFilter { get; set; } = DateTime.Now.Year;
        public int? MonthFilter { get; set; }
        public int? StatusFilter { get; set; }
        public int? OrderTypeFilter { get; set; }
        public string SearchText { get; set; } = string.Empty;

        private CancellationTokenSource? _searchDebounce;

        private VppRequestResDTO? SelectedOrder { get; set; }

        private string AsideSearch { get; set; } = string.Empty;
        private string? AsideCategory { get; set; }
        private string? AsideUom { get; set; }

        private string CurrentDepartmentCode =>
            claims?.FirstOrDefault(c => c.Type == ClaimKeys.DepartmentCode)?.Value ?? string.Empty;

        public List<OptionItem> YearOptions { get; } = new();
        public List<OptionItem> MonthOptions { get; } = new();
        public List<OptionItem> StatusOptions { get; } = new();
        public List<OptionItem> OrderTypeOptions { get; } = new();

        protected override bool CanView => HasDashboardPermission(Permissions.RequestDepartmentSummary);
        protected override string ErrorSummary => Loc["DepartmentSummary"];

        protected string PeriodMetaText => YearFilter.HasValue
            ? (MonthFilter.HasValue
                ? string.Format(Loc["DepartmentPeriodMetaFormat"].Value, $"{MonthFilter.Value:00}/{YearFilter.Value}")
                : string.Format(Loc["DepartmentYearMetaFormat"].Value, YearFilter.Value))
            : Loc["DepartmentAllPeriodsMeta"];

        protected bool HasToolbarFilters =>
            !string.IsNullOrWhiteSpace(SearchText)
            || OrderTypeFilter.HasValue
            || StatusFilter.HasValue
            || MonthFilter.HasValue
            || YearFilter != DateTime.Now.Year;

        protected bool HasAsideFilters =>
            !string.IsNullOrWhiteSpace(AsideSearch)
            || !string.IsNullOrEmpty(AsideCategory)
            || !string.IsNullOrEmpty(AsideUom);

        protected IEnumerable<string> AsideCategoryOptions => DistinctItemValues(item => item.CategoryName);
        protected IEnumerable<string> AsideUomOptions => DistinctItemValues(item => item.UomName);

        protected IEnumerable<VppRequestDetailResDTO> FilteredAsideItems
        {
            get
            {
                var items = SelectedOrder?.Items ?? new List<VppRequestDetailResDTO>();
                var search = AsideSearch.Trim();
                return items.Where(item =>
                    (string.IsNullOrEmpty(search)
                        || (item.VppName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                        || (item.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
                    && (string.IsNullOrEmpty(AsideCategory) || string.Equals(item.CategoryName, AsideCategory, StringComparison.CurrentCultureIgnoreCase))
                    && (string.IsNullOrEmpty(AsideUom) || string.Equals(item.UomName, AsideUom, StringComparison.CurrentCultureIgnoreCase)));
            }
        }

        protected override void OnInit()
        {
            var currentYear = DateTime.Now.Year;
            YearOptions.Clear();
            YearOptions.Add(new OptionItem { Value = null, Text = Loc["All"] });
            for (var i = currentYear - 3; i <= currentYear + 1; i++)
            {
                YearOptions.Add(new OptionItem { Value = i, Text = i.ToString() });
            }

            MonthOptions.Clear();
            MonthOptions.Add(new OptionItem { Value = null, Text = Loc["All"] });
            for (var i = 1; i <= 12; i++)
            {
                MonthOptions.Add(new OptionItem { Value = i, Text = i.ToString("00") });
            }

            StatusOptions.Clear();
            StatusOptions.AddRange(new[]
            {
                new OptionItem { Value = null, Text = Loc["HistoryAllStatuses"] },
                new OptionItem { Value = 1, Text = Loc["Submitted"] },
                new OptionItem { Value = 4, Text = Loc["Cancelled"] },
                new OptionItem { Value = 6, Text = Loc["Pending"] },
                new OptionItem { Value = 7, Text = Loc["Approved"] },
                new OptionItem { Value = 8, Text = Loc["Rejected"] }
            });

            OrderTypeOptions.Clear();
            OrderTypeOptions.AddRange(new[]
            {
                new OptionItem { Value = null, Text = Loc["HistoryAllOrderTypes"] },
                new OptionItem { Value = 0, Text = Loc["Regular"] },
                new OptionItem { Value = 1, Text = Loc["AdditionalOrder"] }
            });
        }

        protected override string BuildEndpoint()
        {
            var query = new List<string>();
            if (YearFilter.HasValue) query.Add($"year={YearFilter.Value}");
            if (MonthFilter.HasValue) query.Add($"month={MonthFilter.Value}");
            if (StatusFilter.HasValue) query.Add($"status={StatusFilter.Value}");

            if (!string.IsNullOrWhiteSpace(CurrentDepartmentCode))
            {
                query.Add($"departmentCode={Uri.EscapeDataString(CurrentDepartmentCode)}");
            }

            var filter = BuildToolbarFilterExpression();
            if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
            if (!string.IsNullOrWhiteSpace(CurrentOrderByExpression)) query.Add($"orderby={Uri.EscapeDataString(CurrentOrderByExpression)}");

            query.Add($"skip={CurrentSkip}");
            query.Add($"top={PageSize}");

            return $"{Config.VppApi.DepartmentOrders}?{string.Join("&", query)}";
        }

        // Toolbar tìm kiếm/lọc chạy trên endpoint department-orders sẵn có: search và loại
        // đơn đi qua tham số filter (Dynamic LINQ) vì endpoint không có tham số riêng.
        private string? BuildToolbarFilterExpression()
        {
            var clauses = new List<string>();
            var search = SearchText.Trim();
            if (!string.IsNullOrEmpty(search))
            {
                var escaped = search.Replace("\\", "\\\\").Replace("\"", "\\\"").ToLowerInvariant();
                clauses.Add($"((VppCode ?? \"\").ToLower().Contains(\"{escaped}\") || (RequesterName ?? \"\").ToLower().Contains(\"{escaped}\"))");
            }

            if (OrderTypeFilter.HasValue)
            {
                clauses.Add($"IsAdditionalOrder == {(OrderTypeFilter.Value == 1 ? "true" : "false")}");
            }

            return clauses.Count > 0 ? string.Join(" && ", clauses) : null;
        }

        protected override void AppendFilterScopeQuery(List<string> query)
        {
            query.Add("scope=department");
            if (YearFilter.HasValue) query.Add($"year={YearFilter.Value}");
            if (MonthFilter.HasValue) query.Add($"month={MonthFilter.Value}");
            if (StatusFilter.HasValue) query.Add($"status={StatusFilter.Value}");
            if (!string.IsNullOrWhiteSpace(CurrentDepartmentCode)) query.Add($"departmentCode={Uri.EscapeDataString(CurrentDepartmentCode)}");
        }

        protected async Task OnSearchInput(ChangeEventArgs args)
        {
            SearchText = args.Value?.ToString() ?? string.Empty;
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            _searchDebounce = new CancellationTokenSource();
            try
            {
                await Task.Delay(280, _searchDebounce.Token);
                CurrentSkip = 0;
                await ReloadAsync();
            }
            catch (TaskCanceledException)
            {
            }
        }

        protected async Task OnOrderTypeChanged(object? value)
        {
            OrderTypeFilter = value as int?;
            CurrentSkip = 0;
            await ReloadAsync();
        }

        protected async Task OnStatusChanged(object? value)
        {
            StatusFilter = value as int?;
            CurrentSkip = 0;
            await ReloadAsync();
        }

        protected async Task OnYearChanged(object? value)
        {
            YearFilter = value as int?;
            CurrentSkip = 0;
            await ReloadAsync();
        }

        protected async Task OnMonthChanged(object? value)
        {
            MonthFilter = value as int?;
            CurrentSkip = 0;
            await ReloadAsync();
        }

        protected async Task ClearToolbarFiltersAsync()
        {
            SearchText = string.Empty;
            OrderTypeFilter = null;
            StatusFilter = null;
            MonthFilter = null;
            YearFilter = DateTime.Now.Year;
            CurrentSkip = 0;
            await ReloadAsync();
        }

        private async Task SelectDepartmentOrderAsync(VppRequestResDTO order)
        {
            SelectedOrder = order;
            AsideSearch = string.Empty;
            AsideCategory = null;
            AsideUom = null;
            await OnRowExpandAsync(order);
        }

        protected void OnAsideSearchInput(ChangeEventArgs args)
        {
            AsideSearch = args.Value?.ToString() ?? string.Empty;
        }

        protected void OnAsideCategoryChanged(object? value)
        {
            AsideCategory = value as string;
        }

        protected void OnAsideUomChanged(object? value)
        {
            AsideUom = value as string;
        }

        protected void ClearAsideFilters()
        {
            AsideSearch = string.Empty;
            AsideCategory = null;
            AsideUom = null;
        }

        private IEnumerable<string> DistinctItemValues(Func<VppRequestDetailResDTO, string?> selector) =>
            (SelectedOrder?.Items ?? new List<VppRequestDetailResDTO>())
                .Select(selector)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase);

        public ValueTask DisposeAsync()
        {
            _searchDebounce?.Cancel();
            _searchDebounce?.Dispose();
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
