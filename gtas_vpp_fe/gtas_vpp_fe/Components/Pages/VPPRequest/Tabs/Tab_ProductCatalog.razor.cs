using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Components;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs
{
    public partial class Tab_ProductCatalog
    {
        public sealed class CategoryOption
        {
            public Guid? Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        public sealed class ProductItem
        {
            public Guid Id { get; set; }
            public string? VPPCode { get; set; }
            public string? VPPName { get; set; }
            public string? Description { get; set; }
            public string? VPPCategoryCode { get; set; }
            public string? VPPCategoryName { get; set; }
            public string? UOMCode { get; set; }
            public string? UOMName { get; set; }
            public int SupplierCount { get; set; }
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;

        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }

        public List<ProductItem> Products { get; set; } = new();
        public List<CategoryOption> CategoryOptions { get; set; } = new();

        public bool IsLoading { get; set; }
        public Guid? CategoryFilter { get; set; }
        public string? SearchText { get; set; }

        private bool CanView =>
            sp_Authentication_GetPermissionSinglePage?.List_Component?.Any(x =>
                (x.ComponentCode == Config.Page_ComponentCode.ComponentCode.RequestProductCatalog && x.IsVisible)) == true;

        protected override async Task OnInitializedAsync()
        {
            await LoadCategoriesAsync();
            await LoadProductsAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var data = await _apiServices.GetFromApiAsync<List<CategoryItem>>(Config.VppApi.Categories) ?? new();
                CategoryOptions = new List<CategoryOption> { new() { Value = null, Text = "All" } };
                CategoryOptions.AddRange(data.Select(c => new CategoryOption
                {
                    Value = c.Id,
                    Text = $"{c.VPPCategoryCode} - {c.VPPCategoryName}"
                }));
            }
            catch
            {
                CategoryOptions = new List<CategoryOption> { new() { Value = null, Text = "All" } };
            }
        }

        protected async Task LoadProductsAsync()
        {
            if (!CanView) return;

            IsLoading = true;
            glb.isBusyPage = true;
            try
            {
                var endpoint = BuildProductsEndpoint();
                Products = await _apiServices.GetFromApiAsync<List<ProductItem>>(endpoint) ?? new();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Product Catalog",
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

        protected async Task ReloadAsync() => await LoadProductsAsync();

        private string BuildProductsEndpoint()
        {
            var query = new List<string>();
            if (CategoryFilter.HasValue) query.Add($"categoryId={CategoryFilter.Value}");
            if (!string.IsNullOrWhiteSpace(SearchText)) query.Add($"search={Uri.EscapeDataString(SearchText)}");

            if (query.Count == 0) return "/api/VPPRequest/products";
            return $"/api/VPPRequest/products?{string.Join("&", query)}";
        }

        private sealed class CategoryItem
        {
            public Guid Id { get; set; }
            public string? VPPCategoryCode { get; set; }
            public string? VPPCategoryName { get; set; }
        }
    }
}
