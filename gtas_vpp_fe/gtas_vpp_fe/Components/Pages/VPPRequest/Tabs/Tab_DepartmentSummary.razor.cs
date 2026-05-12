using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;

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

        private string CurrentDepartmentCode =>
            claims?.FirstOrDefault(c => c.Type == "DepartmentCode")?.Value ?? string.Empty;

        public List<OptionItem> YearOptions { get; } = new();
        public List<OptionItem> MonthOptions { get; } = new();
        public List<OptionItem> StatusOptions { get; } = new()
        {
            new() { Value = null, Text = "All" },
            new() { Value = 1, Text = "Submitted" },
            new() { Value = 4, Text = "Cancelled" },
            new() { Value = 6, Text = "Pending" },
            new() { Value = 7, Text = "Approved" },
            new() { Value = 8, Text = "Rejected" }
        };

        protected override bool CanView => claims.HasPermission(Permissions.RequestDepartmentSummary);
        protected override string ErrorSummary => "Department Summary";

        protected override void OnInit()
        {
            var currentYear = DateTime.Now.Year;
            YearOptions.Clear();
            YearOptions.Add(new OptionItem { Value = null, Text = "All" });
            for (var i = currentYear - 3; i <= currentYear + 1; i++)
            {
                YearOptions.Add(new OptionItem { Value = i, Text = i.ToString() });
            }

            MonthOptions.Clear();
            MonthOptions.Add(new OptionItem { Value = null, Text = "All" });
            for (var i = 1; i <= 12; i++)
            {
                MonthOptions.Add(new OptionItem { Value = i, Text = i.ToString("00") });
            }
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

            query.Add($"skip={CurrentSkip}");
            query.Add($"top={PageSize}");

            return $"{Config.VppApi.DepartmentOrders}?{string.Join("&", query)}";
        }
    }
}
