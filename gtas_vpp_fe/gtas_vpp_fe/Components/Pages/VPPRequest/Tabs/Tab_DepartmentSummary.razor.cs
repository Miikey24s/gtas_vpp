using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_DepartmentSummary
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
        private HashSet<Guid> LoadedDetailOrderIds { get; } = new();
        private HashSet<Guid> LoadingDetailOrderIds { get; } = new();
        public int? YearFilter { get; set; } = DateTime.Now.Year;
        public int? MonthFilter { get; set; }
        public int? StatusFilter { get; set; }
        
        private string CurrentDepartmentCode => claims?.FirstOrDefault(c => c.Type == "DepartmentCode")?.Value ?? string.Empty;

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

        private bool CanView => claims.HasPermission(Permissions.RequestDepartmentSummary);

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
                    Summary = "Department Summary",
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
            
            // Always filter by current user's department at database level
            if (!string.IsNullOrWhiteSpace(CurrentDepartmentCode))
            {
                query.Add($"departmentCode={Uri.EscapeDataString(CurrentDepartmentCode)}");
            }

            // Use department-orders endpoint which filters by DepartmentCode at database level
            if (query.Count == 0) return Config.VppApi.DepartmentOrders;
            return $"{Config.VppApi.DepartmentOrders}?{string.Join("&", query)}";
        }

        protected async Task OnRowExpandAsync(VPP01_RequestHeaderResDTO row)
        {
            if (row == null || row.Id == Guid.Empty || LoadedDetailOrderIds.Contains(row.Id) || LoadingDetailOrderIds.Contains(row.Id))
            {
                return;
            }

            LoadingDetailOrderIds.Add(row.Id);
            try
            {
                var detail = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>($"/api/VPPRequest/orders/{row.Id}");
                row.Items = detail?.Items ?? new List<VPP02_RequestDetailResDTO>();
                LoadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Department Summary",
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

        protected string GetStatusText(int status) => status switch
        {
            1 => "Submitted",
            4 => "Cancelled",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "-"
        };
    }
}
