using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.AI;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.AI.Tabs
{
    public partial class Tab_KeyManage
    {
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public gtas_vpp_shared.DTOs.Share.GlobalClass glb { get; set; } = default!;
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();

        public RadzenDataGrid<GeminiKeyInfo> grid { get; set; } = default!;
        public List<GeminiKeyInfo> keys { get; set; } = new List<GeminiKeyInfo>();
        public bool IsLoading { get; set; } = false;

        public List<string> routingModes { get; set; } = new List<string> 
        { 
            "Auto (3-Tier Load Balancer)", 
            "Gemini 3 Flash", 
            "Gemini 3.1 Flash Lite", 
            "Gemma 4 31B" 
        };
        public string selectedMode { get; set; } = "Auto (3-Tier Load Balancer)";

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadData();
        }

        private async Task LoadData()
        {
            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                var result = await _apiServices.GetFromApiAsync<List<GeminiKeyInfo>>("/api/AI/keys");
                if (result != null)
                {
                    keys = result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading AI keys: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }

        private string GetMaskedKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length <= 8) return "********";
            return key.Substring(0, 4) + new string('*', key.Length - 8) + key.Substring(key.Length - 4);
        }
    }
}
