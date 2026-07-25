using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_DepartmentSummary : BaseOrderTab
    {
        public sealed class OptionItem
        {
            public int? Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        public int? YearFilter { get; set; } = DateTime.Now.Year;
        public int? MonthFilter { get; set; }
        public int? StatusFilter { get; set; }

        private VppRequestResDTO? SelectedOrder { get; set; }

        private string CurrentDepartmentCode =>
            claims?.FirstOrDefault(c => c.Type == ClaimKeys.DepartmentCode)?.Value ?? string.Empty;

        public List<OptionItem> YearOptions { get; } = new();
        public List<OptionItem> MonthOptions { get; } = new();
        public List<OptionItem> StatusOptions { get; } = new();

        protected override bool CanView => HasDashboardPermission(Permissions.RequestDepartmentSummary);
        protected override string ErrorSummary => Loc["DepartmentSummary"];

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
                new OptionItem { Value = null, Text = Loc["All"] },
                new OptionItem { Value = 1, Text = Loc["Submitted"] },
                new OptionItem { Value = 4, Text = Loc["Cancelled"] },
                new OptionItem { Value = 6, Text = Loc["Pending"] },
                new OptionItem { Value = 7, Text = Loc["Approved"] },
                new OptionItem { Value = 8, Text = Loc["Rejected"] }
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

            if (!string.IsNullOrWhiteSpace(CurrentFilterExpression)) query.Add($"filter={Uri.EscapeDataString(CurrentFilterExpression)}");
            if (!string.IsNullOrWhiteSpace(CurrentOrderByExpression)) query.Add($"orderby={Uri.EscapeDataString(CurrentOrderByExpression)}");

            query.Add($"skip={CurrentSkip}");
            query.Add($"top={PageSize}");

            return $"{Config.VppApi.DepartmentOrders}?{string.Join("&", query)}";
        }

        protected override void AppendFilterScopeQuery(List<string> query)
        {
            query.Add("scope=department");
            if (YearFilter.HasValue) query.Add($"year={YearFilter.Value}");
            if (MonthFilter.HasValue) query.Add($"month={MonthFilter.Value}");
            if (StatusFilter.HasValue) query.Add($"status={StatusFilter.Value}");
            if (!string.IsNullOrWhiteSpace(CurrentDepartmentCode)) query.Add($"departmentCode={Uri.EscapeDataString(CurrentDepartmentCode)}");
        }

        private async Task SelectDepartmentOrderAsync(VppRequestResDTO order)
        {
            SelectedOrder = order;
            await OnRowExpandAsync(order);
        }
    }
}
