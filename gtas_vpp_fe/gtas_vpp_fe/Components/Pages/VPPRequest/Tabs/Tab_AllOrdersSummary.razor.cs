using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_AllOrdersSummary
    {
        public sealed class OptionItem
        {
            public int? Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;

        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        public List<VPP01_RequestHeaderResDTO> Orders { get; set; } = new();

        public bool IsLoading { get; set; }
        public int? YearFilter { get; set; } = DateTime.Now.Year;
        public int? MonthFilter { get; set; }
        public int? StatusFilter { get; set; }

        public List<OptionItem> YearOptions { get; } = new();
        public List<OptionItem> MonthOptions { get; } = new();
        public List<OptionItem> StatusOptions { get; } = new()
        {
            new() { Value = null, Text = "All" },
            new() { Value = 0, Text = "Draft" },
            new() { Value = 1, Text = "Submitted" },
            new() { Value = 2, Text = "Pending Approval" },
            new() { Value = 3, Text = "Rejected" },
            new() { Value = 4, Text = "Cancelled" },
            new() { Value = 5, Text = "Closed" },
            new() { Value = 6, Text = "Deleted" },
            new() { Value = 7, Text = "Approved" }
        };

        private bool CanView => claims.HasPermission(Permissions.RequestAllOrdersSummary);

        protected override async Task OnInitializedAsync()
        {
            InitFilters();
            await LoadOrdersAsync();
        }

        private void InitFilters()
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

        protected async Task LoadOrdersAsync()
        {
            if (!CanView) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                var endpoint = BuildEndpoint();
                var data = await _apiServices.GetFromApiAsync<List<VPP01_RequestHeaderResDTO>>(endpoint) ?? new();
                Orders = data
                    .OrderByDescending(x => x.Y)
                    .ThenByDescending(x => x.M)
                    .ThenByDescending(x => x.UpdateDate)
                    .ToList();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "All Orders Summary",
                    Detail = $"Load failed: {ex.Message}",
                    Duration = 6000
                });
            }
            finally
            {
                glb.isBusyPage = false;
                IsLoading = false;
                StateHasChanged();
            }
        }

        protected async Task ReloadAsync() => await LoadOrdersAsync();

        private string BuildEndpoint()
        {
            var query = new List<string>();
            
            if (YearFilter.HasValue) query.Add($"year={YearFilter.Value}");
            if (MonthFilter.HasValue) query.Add($"month={MonthFilter.Value}");
            if (StatusFilter.HasValue) query.Add($"status={StatusFilter.Value}");

            // Use all-orders endpoint which returns ALL orders from ALL departments
            if (query.Count == 0) return Config.VppApi.AllOrders;
            return $"{Config.VppApi.AllOrders}?{string.Join("&", query)}";
        }

        protected string GetStatusText(int status) => status switch
        {
            0 => "Draft",
            1 => "Submitted",
            2 => "Pending Approval",
            3 => "Rejected",
            4 => "Cancelled",
            5 => "Closed",
            6 => "Deleted",
            7 => "Approved",
            _ => "-"
        };
    }
}
