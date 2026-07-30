using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Models;
using gtas_vpp_fe.Services;
using gtas_vpp_fe.State;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs;

public partial class Tab_ItemLibrary : IDisposable
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
    [Inject] public IAPIServices ApiServices { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public GlobalClass Global { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<VppItemResDTO> rows = [];
    private List<VppCategoryResDTO> categories = [];
    private List<LookupValueResDTO> uoms = [];
    private RadzenDataGrid<VppItemResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private string searchText = string.Empty;
    private string categoryFilter = string.Empty;
    private string uomFilter = string.Empty;
    private string? currentFilter;
    private bool isLoading;
    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText) || !string.IsNullOrWhiteSpace(categoryFilter) || !string.IsNullOrWhiteSpace(uomFilter);
    private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);
    private IReadOnlyList<VppFilterOption<string>> categoryOptions => [new(string.Empty, Loc["AllCategories"]), .. categories.Where(x => !x.IsDeleted).Select(x => new VppFilterOption<string>(x.Id.ToString(), x.VppCategoryName ?? x.VppCategoryCode ?? string.Empty))];
    private IReadOnlyList<VppFilterOption<string>> uomOptions => [new(string.Empty, Loc["AllUnits"]), .. uoms.Where(x => !x.IsDeleted).Select(x => new VppFilterOption<string>(x.Id.ToString(), x.Value ?? x.Code ?? string.Empty))];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var categoriesTask = ApiServices.GetFromApiAsync<List<VppCategoryResDTO>>($"{Config.LibraryApi.VppCategories}?showDeleted=true");
            var uomsTask = ApiServices.GetFromApiAsync<List<LookupValueResDTO>>($"{Config.LibraryApi.LookupValues}?showDeleted=true");
            await Task.WhenAll(categoriesTask, uomsTask);
            categories = await categoriesTask ?? [];
            uoms = await uomsTask ?? [];
        }
        catch (Exception ex) { ToastService.Error(ex, Loc, "LoadLibraryDataFailed"); }
    }

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true; currentSkip = args.Skip ?? 0; currentFilter = args.Filter;
        try
        {
            var query = new List<string> { $"skip={args.Skip ?? 0}", $"top={args.Top ?? VppPagingProfiles.Collection.DefaultPageSize}", "showDeleted=true" };
            if (!string.IsNullOrWhiteSpace(searchText)) query.Add($"search={Uri.EscapeDataString(searchText.Trim())}");
            if (Guid.TryParse(categoryFilter, out var categoryId)) query.Add($"categoryId={categoryId}");
            if (Guid.TryParse(uomFilter, out var parsedUomId))
            {
                var uomFilterExpression = $"UomId == \"{parsedUomId}\"";
                query.Add($"filter={Uri.EscapeDataString(uomFilterExpression)}");
            }
            if (!string.IsNullOrWhiteSpace(args.Filter)) query.Add($"filter={Uri.EscapeDataString(args.Filter)}");
            if (!string.IsNullOrWhiteSpace(args.OrderBy)) query.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
            var result = await ApiServices.GetFromApiWithTotalCountAsync<List<VppItemResDTO>>($"{Config.ApiCatalogItems}?{string.Join("&", query)}");
            rows = result.Data ?? []; totalCount = result.TotalCount;
        }
        catch (Exception ex) { rows = []; totalCount = 0; ToastService.Error(ex, Loc, "LoadLibraryDataFailed"); }
        finally { isLoading = false; StateHasChanged(); }
    }

    private async Task LoadColumnFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<VppItemResDTO> args)
    {
        if (args.Column is null) return;
        try
        {
            var property = args.Column.GetFilterProperty();
            var query = $"{Config.ApiCatalogItems}?showDeleted=true&distinct={Uri.EscapeDataString(property)}&skip={args.Skip ?? 0}&top={args.Top ?? 50}";
            if (!string.IsNullOrWhiteSpace(args.Filter)) query += $"&distinctFilter={Uri.EscapeDataString(args.Filter)}";
            var result = await ApiServices.GetFromApiWithTotalCountAsync<List<VppItemResDTO>>(query);
            args.Data = result.Data ?? []; args.Count = result.TotalCount;
        }
        catch (Exception ex) { args.Data = Array.Empty<VppItemResDTO>(); args.Count = 0; ToastService.Error(ex, Loc, "LoadLibraryDataFailed"); }
    }

    private async Task OpenCreateAsync()
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_ItemEditor>(Loc["AddItem"].Value, new Dictionary<string, object?> { ["IsCreate"] = true, ["Categories"] = categories, ["Uoms"] = uoms }, VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["AddItem"].Value, closeAriaLabel: Loc["Close"].Value));
        if (result is VppItemResDTO) await grid.Reload();
    }

    private async Task OpenEditAsync(VppItemResDTO row)
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_ItemEditor>(Loc["Edit"].Value, new Dictionary<string, object?> { ["IsCreate"] = false, ["Model"] = Clone(row), ["Categories"] = categories, ["Uoms"] = uoms }, VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["Edit"].Value, closeAriaLabel: Loc["Close"].Value));
        if (result is VppItemResDTO) await grid.Reload();
    }

    private async Task ToggleDeletedAsync(VppItemResDTO row, bool value)
    {
        try
        {
            var result = await ApiServices.PatchFromApiAsync<VppItemResDTO>($"{Config.ApiCatalogItems}/{row.Id}/status", new { IsDeleted = value });
            if (result is null) { row.IsDeleted = !value; ToastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true); return; }
            row.IsDeleted = result.IsDeleted; await grid.Reload();
        }
        catch (Exception ex) { row.IsDeleted = !value; ToastService.Error(ex, Loc, "ChangeRecordStatusFailed"); }
    }

    private async Task OnSearchInputAsync(ChangeEventArgs args) { searchText = args.Value?.ToString() ?? string.Empty; await ReloadAsync(); }
    private async Task OnCategoryChangedAsync(string value) { categoryFilter = value; await ReloadAsync(); }
    private async Task OnUomChangedAsync(string value) { uomFilter = value; await ReloadAsync(); }
    private async Task ClearFiltersAsync() { searchText = string.Empty; categoryFilter = string.Empty; uomFilter = string.Empty; await ReloadAsync(); }
    private async Task ReloadAsync() { if (grid is not null) await grid.FirstPage(true); }

    private static VppItemResDTO Clone(VppItemResDTO row) => new() { Id = row.Id, VppCode = row.VppCode, VppName = row.VppName, Description = row.Description, UomId = row.UomId, UomCode = row.UomCode, UomName = row.UomName, VppCategoryId = row.VppCategoryId, VppCategoryCode = row.VppCategoryCode, VppCategoryName = row.VppCategoryName, IsDeleted = row.IsDeleted, CreatedAtUtc = row.CreatedAtUtc, CreatedByUserId = row.CreatedByUserId, UpdatedAtUtc = row.UpdatedAtUtc, UpdatedByUserId = row.UpdatedByUserId };
    private static void OnRowRender(RowRenderEventArgs<VppItemResDTO> args) { if (args.Data?.IsDeleted == true) args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current) ? $"{current} vpp-admin-row-deleted" : "vpp-admin-row-deleted"; }
    public void Dispose() { }
}
