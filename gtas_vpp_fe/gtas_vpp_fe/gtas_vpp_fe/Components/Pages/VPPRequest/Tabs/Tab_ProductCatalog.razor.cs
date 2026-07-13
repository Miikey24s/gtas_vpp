using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Globalization;
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
            public string? DefaultSupplierName { get; set; }
            public decimal? DefaultPrice { get; set; }
            public decimal DefaultVatRate { get; set; }
        }

        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public PermissionState PermissionState { get; set; } = default!;

        [Parameter] public IEnumerable<Claim>? claims { get; set; }

        public List<ProductItem> Products { get; set; } = new();
        public List<CategoryOption> CategoryOptions { get; set; } = new();
        public int ProductCount { get; set; }

        public bool IsFirstLoading { get; set; } = true;
        public bool IsGridLoading { get; set; }
        public Guid? CategoryFilter { get; set; }
        public string? SearchText { get; set; }
        public RadzenDataGrid<ProductItem>? productGrid { get; set; }
        private int _currentSkip;
        private string? _currentFilterExpression;
        private bool _isFirstLoad = true;

        private bool CanView => PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Dashboard, Permissions.RequestProductCatalog);

        protected override async Task OnInitializedAsync()
        {
            await LoadCategoriesAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && CanView && productGrid != null)
            {
                await productGrid.Reload();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var data = await _apiServices.GetFromApiAsync<List<CategoryItem>>(Config.VppApi.Categories) ?? new();
                CategoryOptions = new List<CategoryOption> { new() { Value = null, Text = Loc["All"] } };
                CategoryOptions.AddRange(data.Select(c => new CategoryOption
                {
                    Value = c.Id,
                    Text = $"{c.VPPCategoryCode} - {c.VPPCategoryName}"
                }));
            }
            catch
            {
                CategoryOptions = new List<CategoryOption> { new() { Value = null, Text = Loc["All"] } };
            }
        }

        protected async Task LoadProductsAsync(LoadDataArgs args)
        {
            if (!CanView) return;

            if (_isFirstLoad)
            {
                IsFirstLoading = true;
            }
            IsGridLoading = true;
            _currentSkip = args.Skip ?? 0;
            _currentFilterExpression = args.Filter;
            try
            {
                var endpoint = BuildProductsEndpoint(args);
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<ProductItem>>(endpoint);
                Products = result.Data ?? new();
                ProductCount = result.TotalCount;
            }
            catch (Exception ex)
            {
                Products = new();
                ProductCount = 0;
                Toast.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = Loc["ProductCatalog"],
                    Detail = string.Format(Loc["LoadFailedFormat"], ex.Message),
                    Duration = 6000
                });
            }
            finally
            {
                if (_isFirstLoad)
                {
                    _isFirstLoad = false;
                    IsFirstLoading = false;
                }
                IsGridLoading = false;
                StateHasChanged();
            }
        }

        protected async Task ReloadAsync()
        {
            if (productGrid != null)
            {
                await productGrid.Reload();
            }
        }

        protected async Task LoadColumnFilterDataProducts(DataGridLoadColumnFilterDataEventArgs<ProductItem> args)
        {
            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                if (string.IsNullOrWhiteSpace(property)) return;

                var query = new List<string> { $"distinct={Uri.EscapeDataString(property)}" };
                if (CategoryFilter.HasValue) query.Add($"categoryId={CategoryFilter.Value}");
                if (!string.IsNullOrWhiteSpace(SearchText)) query.Add($"search={Uri.EscapeDataString(SearchText)}");
                if (!string.IsNullOrWhiteSpace(_currentFilterExpression)) query.Add($"filter={Uri.EscapeDataString(_currentFilterExpression)}");
                if (!string.IsNullOrWhiteSpace(args.Filter)) query.Add($"distinctFilter={Uri.EscapeDataString(args.Filter)}");

                var apiUrl = $"/api/VPPRequest/products?{string.Join("&", query)}";
                var response = await _apiServices.GetFromApiAsync<List<Dictionary<string, object?>>>(apiUrl);

                if (response != null && response.Count > 0)
                {
                    var distinctDtos = response
                        .Select(dict =>
                        {
                            var dto = new ProductItem();
                            var propInfo = typeof(ProductItem).GetProperty(property);
                            if (propInfo != null && dict.TryGetValue(property, out var val) && val != null)
                            {
                                try
                                {
                                    var targetType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                                    var converted = Convert.ChangeType(val.ToString(), targetType, CultureInfo.InvariantCulture);
                                    propInfo.SetValue(dto, converted);
                                }
                                catch { }
                            }
                            return dto;
                        })
                        .ToList();

                    args.Data = distinctDtos;
                    args.Count = distinctDtos.Count;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tab_ProductCatalog] LoadColumnFilterData failed: {ex.Message}");
            }
        }

        private string BuildProductsEndpoint(LoadDataArgs args)
        {
            var query = new List<string>();
            if (CategoryFilter.HasValue) query.Add($"categoryId={CategoryFilter.Value}");
            if (!string.IsNullOrWhiteSpace(SearchText)) query.Add($"search={Uri.EscapeDataString(SearchText)}");
            if (!string.IsNullOrWhiteSpace(args.Filter)) query.Add($"filter={Uri.EscapeDataString(args.Filter)}");
            if (args.Skip.HasValue) query.Add($"skip={args.Skip.Value}");
            if (args.Top.HasValue) query.Add($"top={args.Top.Value}");
            if (!string.IsNullOrWhiteSpace(args.OrderBy)) query.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");

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
