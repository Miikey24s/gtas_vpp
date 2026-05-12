using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_History : BaseOrderTab
    {
        public sealed class OptionItem
        {
            public int Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        public IEnumerable<int> YearFilter { get; set; } = new[] { DateTime.Now.Year };
        public IEnumerable<int> MonthFilter { get; set; } = Enumerable.Empty<int>();
        public IEnumerable<int> StatusFilter { get; set; } = new[] { 1, 4, 6, 7, 8 }; // all statuses

        public List<OptionItem> YearOptions { get; } = new();
        public List<OptionItem> MonthOptions { get; } = new();
        public List<OptionItem> StatusOptions { get; } = new();

        protected override bool CanView => claims.HasPermission(Permissions.RequestHistory);
        protected override string ErrorSummary => Loc["History"];

        // History uses a default page size of 10 (smaller than the other tabs).
        public Tab_History()
        {
            PageSize = 10;
        }

        protected override void OnInit()
        {
            var currentYear = DateTime.Now.Year;
            YearOptions.Clear();
            for (var i = currentYear - 3; i <= currentYear + 1; i++)
            {
                YearOptions.Add(new OptionItem { Value = i, Text = i.ToString() });
            }

            MonthOptions.Clear();
            for (var i = 1; i <= 12; i++)
            {
                MonthOptions.Add(new OptionItem { Value = i, Text = i.ToString("00") });
            }

            StatusOptions.Clear();
            StatusOptions.AddRange(new[]
            {
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

            AddQueryValues(query, "years", YearFilter);
            AddQueryValues(query, "months", MonthFilter);
            AddQueryValues(query, "statuses", StatusFilter);

            query.Add($"skip={CurrentSkip}");
            query.Add($"top={PageSize}");

            return $"/api/VPPRequest/my-orders-summary?{string.Join("&", query)}";
        }

        private static void AddQueryValues(List<string> query, string key, IEnumerable<int>? values)
        {
            if (values == null) return;
            foreach (var value in values.Distinct())
            {
                query.Add($"{key}={value}");
            }
        }
    }
}
