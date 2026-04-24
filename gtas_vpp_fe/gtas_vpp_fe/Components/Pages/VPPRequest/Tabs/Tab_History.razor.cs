﻿using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_History
    {
        public sealed class OptionItem
        {
            public int Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;

        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }

        public List<VPP01_RequestHeaderResDTO> Orders { get; set; } = new();
        private HashSet<Guid> LoadedDetailOrderIds { get; } = new();
        private HashSet<Guid> LoadingDetailOrderIds { get; } = new();

        public bool IsLoading { get; set; }
        public IEnumerable<int> YearFilter { get; set; } = new[] { DateTime.Now.Year };
        public IEnumerable<int> MonthFilter { get; set; } = Enumerable.Empty<int>();
        public IEnumerable<int> StatusFilter { get; set; } = new[] { 1, 4, 5 };

        public List<OptionItem> YearOptions { get; } = new();
        public List<OptionItem> MonthOptions { get; } = new();
        public List<OptionItem> StatusOptions { get; } = new()
        {
            new() { Value = 1, Text = "Submitted" },
            new() { Value = 4, Text = "Cancelled" },
            new() { Value = 5, Text = "Closed" },
            new() { Value = 6, Text = "Pending" },
            new() { Value = 7, Text = "Approved" },
            new() { Value = 8, Text = "Rejected" }
        };

        private bool CanView =>
            sp_Authentication_GetPermissionSinglePage?.List_Component?.Any(x =>
                (x.ComponentCode == Config.Page_ComponentCode.ComponentCode.RequestHistory)) == true;

        protected override async Task OnInitializedAsync()
        {
            InitFilters();
            await LoadHistoryAsync();
        }

        private void InitFilters()
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
        }

        protected async Task LoadHistoryAsync()
        {
            if (!CanView) return;

            IsLoading = true;
            glb.isBusyPage = true;

            try
            {
                var endpoint = BuildHistoryEndpoint();
                var data = await _apiServices.GetFromApiAsync<List<VPP01_RequestHeaderResDTO>>(endpoint) ?? new();

                Orders = data
                    .OrderByDescending(x => x.Y)
                    .ThenByDescending(x => x.M)
                    .ThenByDescending(x => x.SubmittedDate ?? x.UpdateDate)
                    .ToList();

                LoadedDetailOrderIds.Clear();
                LoadingDetailOrderIds.Clear();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "History",
                    Detail = $"Load history failed: {ex.Message}",
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

        protected async Task ReloadAsync()
        {
            await LoadHistoryAsync();
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
                var detail = await _apiServices.GetFromApiAsync<VPP01_RequestHeaderResDTO>($"{Config.VppApi.Orders}/{row.Id}");
                row.Items = detail?.Items ?? new List<VPP02_RequestDetailResDTO>();
                LoadedDetailOrderIds.Add(row.Id);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "History",
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

        private string BuildHistoryEndpoint()
        {
            var query = new List<string>();

            AddQueryValues(query, "years", YearFilter);
            AddQueryValues(query, "months", MonthFilter);
            AddQueryValues(query, "statuses", StatusFilter);

            return query.Count == 0
                ? "/api/VPPRequest/my-orders-summary"
                : $"/api/VPPRequest/my-orders-summary?{string.Join("&", query)}";
        }

        private static void AddQueryValues(List<string> query, string key, IEnumerable<int>? values)
        {
            if (values == null)
            {
                return;
            }

            foreach (var value in values.Distinct())
            {
                query.Add($"{key}={value}");
            }
        }

        protected string GetStatusText(int status) => status switch
        {
            0 => "Draft",
            1 => "Submitted",
            4 => "Cancelled",
            5 => "Closed",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "-"
        };

        protected BadgeStyle GetStatusBadgeStyle(int status) => status switch
        {
            1 => BadgeStyle.Success,
            4 => BadgeStyle.Danger,
            5 => BadgeStyle.Info,
            _ => BadgeStyle.Light
        };
    }
}
