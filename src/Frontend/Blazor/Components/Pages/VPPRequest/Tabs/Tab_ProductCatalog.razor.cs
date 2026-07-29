using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

public partial class Tab_ProductCatalog : IDisposable
{
    public sealed class ProductItem
    {
        public Guid Id { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string? Description { get; set; }
        public string? VppCategoryCode { get; set; }
        public string? VppCategoryName { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
    }

    [Inject] public IAPIServices ApiServices { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    [Parameter] public IEnumerable<Claim>? claims { get; set; }

    public List<ProductItem> Products { get; set; } = [];
    public List<VppFilterOption<string>> CategoryOptions { get; set; } = [];
    public List<VppFilterOption<string>> UnitOptions { get; set; } = [];
    public int ProductCount { get; set; }
    public int CurrentSkip { get; set; }
    public bool IsFirstLoading { get; set; } = true;
    public bool IsGridLoading { get; set; }
    public bool HasLoadError { get; set; }
    public Guid? CategoryFilter { get; set; }
    public string? UnitFilter { get; set; }
    public string? SearchText { get; set; }
    public RadzenDataGrid<ProductItem>? productGrid { get; set; }

    private bool _isFirstLoad = true;
    private CancellationTokenSource? _searchDebounceCts;

    private bool CanView => PermissionState.HasVisibleComponent(
        Config.Page_ComponentCode.PageCode.Dashboard,
        Permissions.RequestProductCatalog);
    private bool HasFilters => !string.IsNullOrWhiteSpace(SearchText) || CategoryFilter.HasValue || !string.IsNullOrWhiteSpace(UnitFilter);
    private string CategoryFilterValue => CategoryFilter?.ToString() ?? string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadCategoriesAsync(), LoadUnitsAsync());
        await LoadProductsAsync(new LoadDataArgs { Skip = 0, Top = VppPagingProfiles.Collection.DefaultPageSize });
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var data = await ApiServices.GetFromApiAsync<List<CategoryItem>>(Config.VppApi.Categories) ?? [];
            CategoryOptions = [new(string.Empty, Loc["AllCategories"])];
            CategoryOptions.AddRange(data.Select(category => new VppFilterOption<string>(
                category.Id.ToString(),
                category.VppCategoryName ?? category.VppCategoryCode ?? string.Empty)));
        }
        catch
        {
            CategoryOptions = [new(string.Empty, Loc["AllCategories"])];
        }
    }

    private async Task LoadUnitsAsync()
    {
        try
        {
            const int batchSize = 100;
            var data = new List<ProductItem>();
            for (var skip = 0; ; skip += batchSize)
            {
                var result = await ApiServices.GetFromApiWithTotalCountAsync<List<ProductItem>>(
                    $"/api/VPPRequest/products?distinct=UomName&skip={skip}&top={batchSize}");
                var batch = result.Data ?? [];
                data.AddRange(batch);
                if (data.Count >= result.TotalCount || batch.Count == 0)
                {
                    break;
                }
            }

            UnitOptions = [new(string.Empty, Loc["AllUnits"])];
            UnitOptions.AddRange(data
                .Where(item => !string.IsNullOrWhiteSpace(item.UomName))
                .Select(item => item.UomName!.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .Select(name => new VppFilterOption<string>(name, name)));
        }
        catch
        {
            UnitOptions = [new(string.Empty, Loc["AllUnits"])];
        }
    }

    protected async Task LoadProductsAsync(LoadDataArgs args)
    {
        if (!CanView)
        {
            return;
        }

        IsFirstLoading = _isFirstLoad;
        IsGridLoading = true;
        HasLoadError = false;

        try
        {
            CurrentSkip = args.Skip ?? 0;
            var result = await ApiServices.GetFromApiWithTotalCountAsync<List<ProductItem>>(BuildProductsEndpoint(args));
            Products = result.Data ?? [];
            ProductCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            Products = [];
            ProductCount = 0;
            HasLoadError = true;
            Toast.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = Loc["ProductCatalog"],
                Detail = UiErrorMapper.GetMessage(ex, Loc),
                Duration = 6000
            });
        }
        finally
        {
            _isFirstLoad = false;
            IsFirstLoading = false;
            IsGridLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnSearchInput(ChangeEventArgs args)
    {
        SearchText = args.Value?.ToString();
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _searchDebounceCts.Token);
            await ReloadFromFirstPageAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task OnCategoryChanged(string value)
    {
        CategoryFilter = Guid.TryParse(value, out var categoryId) ? categoryId : null;
        await ReloadFromFirstPageAsync();
    }

    private async Task OnUnitChanged(string value)
    {
        UnitFilter = string.IsNullOrWhiteSpace(value) ? null : value;
        await ReloadFromFirstPageAsync();
    }

    private async Task ClearFiltersAsync()
    {
        SearchText = null;
        CategoryFilter = null;
        UnitFilter = null;
        await ReloadFromFirstPageAsync();
    }

    protected async Task ReloadAsync() => await ReloadFromFirstPageAsync();

    private async Task ReloadFromFirstPageAsync()
    {
        if (productGrid is not null)
        {
            await productGrid.FirstPage(true);
        }
    }

    private string BuildProductsEndpoint(LoadDataArgs args)
    {
        var query = new List<string>();
        if (CategoryFilter.HasValue) query.Add($"categoryId={CategoryFilter.Value}");
        if (!string.IsNullOrWhiteSpace(SearchText)) query.Add($"search={Uri.EscapeDataString(SearchText.Trim())}");
        if (!string.IsNullOrWhiteSpace(UnitFilter)) query.Add($"filter={Uri.EscapeDataString(BuildUnitFilter(UnitFilter))}");
        query.Add($"skip={args.Skip ?? 0}");
        query.Add($"top={args.Top ?? VppPagingProfiles.Collection.DefaultPageSize}");
        if (!string.IsNullOrWhiteSpace(args.OrderBy)) query.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
        return $"/api/VPPRequest/products?{string.Join("&", query)}";
    }

    private static string BuildUnitFilter(string value)
        => $"UomName == \"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

    public void Dispose()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
    }

    private sealed class CategoryItem
    {
        public Guid Id { get; set; }
        public string? VppCategoryCode { get; set; }
        public string? VppCategoryName { get; set; }
    }
}
