// PAGE LOGIC: VPPRequest/Tabs/Tab_ProductCatalog.razor.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.VPPRequest.Tabs;

public partial class Tab_ProductCatalog : IDisposable
{
    [Inject] public RequestsQueryClient Requests { get; set; } = default!;
    [Inject] public PermissionState PermissionState { get; set; } = default!;

    public List<VppItemResDTO> Products { get; set; } = [];
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
    public RadzenDataGrid<VppItemResDTO>? productGrid { get; set; }

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
            var data = await Requests.GetCatalogCategoriesAsync();
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
            var data = await Requests.GetCatalogUnitNamesAsync();

            UnitOptions = [new(string.Empty, Loc["AllUnits"])];
            UnitOptions.AddRange(data
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
            var result = await Requests.GetCatalogItemsAsync(new ProductCatalogQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                CategoryFilter,
                SearchText ?? string.Empty,
                UnitFilter,
                args.OrderBy));
            Products = result.Items.ToList();
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

    public void Dispose()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
    }
}
