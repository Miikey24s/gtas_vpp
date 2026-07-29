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

public partial class Tab_SupplierLibrary : IDisposable
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
    [Inject] public IAPIServices ApiServices { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public GlobalClass Global { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<SupplierResDTO> rows = [];
    private RadzenDataGrid<SupplierResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private string searchText = string.Empty;
    private string? currentFilter;
    private bool isLoading;
    private CancellationTokenSource? searchDebounce;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText);
    private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true;
        currentSkip = args.Skip ?? 0;
        currentFilter = CombineWithSearch(args.Filter);
        try
        {
            var result = await ApiServices.GetFromApiWithTotalCountAsync<List<SupplierResDTO>>(BuildQuery(currentFilter, args.Skip, args.Top, args.OrderBy));
            rows = result.Data ?? [];
            totalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            rows = [];
            totalCount = 0;
            ToastService.Error(ex, Loc, "LoadLibraryDataFailed");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadColumnFilterDataAsync(DataGridLoadColumnFilterDataEventArgs<SupplierResDTO> args)
    {
        if (args.Column is null) return;
        try
        {
            var property = args.Column.GetFilterProperty();
            var result = await ApiServices.GetFromApiWithTotalCountAsync<List<SupplierResDTO>>(BuildQuery(currentFilter, args.Skip, args.Top, null, property, args.Filter));
            args.Data = result.Data ?? [];
            args.Count = result.TotalCount;
        }
        catch (Exception ex)
        {
            args.Data = Array.Empty<SupplierResDTO>();
            args.Count = 0;
            ToastService.Error(ex, Loc, "LoadLibraryDataFailed");
        }
    }

    private async Task OpenCreateAsync()
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_SupplierEditor>(
            Loc["AddSupplier"].Value,
            new Dictionary<string, object?> { ["IsCreate"] = true },
            VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["AddSupplier"].Value, closeAriaLabel: Loc["Close"].Value));
        if (result is SupplierResDTO) await grid.Reload();
    }

    private async Task OpenEditAsync(SupplierResDTO row)
    {
        var result = await DialogService.OpenAsync<Dialog.Dialog_SupplierEditor>(
            Loc["Edit"].Value,
            new Dictionary<string, object?> { ["IsCreate"] = false, ["Model"] = Clone(row) },
            VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["Edit"].Value, closeAriaLabel: Loc["Close"].Value));
        if (result is SupplierResDTO) await grid.Reload();
    }

    private async Task ToggleDeletedAsync(SupplierResDTO row, bool value)
    {
        if (value && !await CanDeactivateAsync(Config.LibraryApi.Suppliers, row.Id))
        {
            return;
        }

        try
        {
            var result = await ApiServices.PatchFromApiAsync<SupplierResDTO>($"{Config.LibraryApi.Suppliers}/{row.Id}", new { IsDeleted = value, UpdatedAtUtc = DateTime.Now, UpdatedByUserId = Global.UserInfo.UserID });
            if (result is null)
            {
                row.IsDeleted = !value;
                ToastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true);
                return;
            }
            row.IsDeleted = result.IsDeleted;
            await grid.Reload();
        }
        catch (Exception ex)
        {
            row.IsDeleted = !value;
            ToastService.Error(ex, Loc, "ChangeRecordStatusFailed");
        }
    }

    private async Task<bool> CanDeactivateAsync(string endpoint, Guid id)
    {
        var impact = await ApiServices.GetFromApiAsync<LibraryDependencyImpactResDTO>($"{endpoint}/{id}/dependency-impact");
        if (impact is null || impact.CanDeactivate) return true;

        ToastService.Show(NotificationSeverity.Warning, Loc["ValidationTitle"],
            string.Format(Loc["DependencyDeactivateBlocked"].Value, impact.ActiveReferenceCount), 5000, false);
        return false;
    }

    private async Task OnSearchInputAsync(ChangeEventArgs args)
    {
        searchText = args.Value?.ToString() ?? string.Empty;
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        searchDebounce = new CancellationTokenSource();
        try { await Task.Delay(280, searchDebounce.Token); await grid.FirstPage(true); }
        catch (OperationCanceledException) { }
    }

    private async Task ClearFiltersAsync() { searchText = string.Empty; await grid.FirstPage(true); }

    private string? CombineWithSearch(string? gridFilter)
    {
        var search = searchText.Trim();
        if (string.IsNullOrWhiteSpace(search)) return gridFilter;
        var escaped = search.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).ToLowerInvariant();
        var clause = $"((SupplierShortName ?? \"\").ToLower().Contains(\"{escaped}\") || (SupplierName ?? \"\").ToLower().Contains(\"{escaped}\"))";
        return string.IsNullOrWhiteSpace(gridFilter) ? clause : $"({gridFilter}) && {clause}";
    }

    private static string BuildQuery(string? filter, int? skip, int? top, string? orderby, string? distinct = null, string? distinctFilter = null)
    {
        var query = new List<string> { "showDeleted=true" };
        if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
        if (skip.HasValue) query.Add($"skip={skip.Value}");
        if (top.HasValue) query.Add($"top={top.Value}");
        if (!string.IsNullOrWhiteSpace(orderby)) query.Add($"orderby={Uri.EscapeDataString(orderby)}");
        if (!string.IsNullOrWhiteSpace(distinct)) query.Add($"distinct={Uri.EscapeDataString(distinct)}");
        if (!string.IsNullOrWhiteSpace(distinctFilter)) query.Add($"distinctFilter={Uri.EscapeDataString(distinctFilter)}");
        return $"{Config.LibraryApi.Suppliers}?{string.Join("&", query)}";
    }

    private static SupplierResDTO Clone(SupplierResDTO row) => new()
    {
        Id = row.Id, SupplierShortName = row.SupplierShortName, SupplierName = row.SupplierName,
        Address1 = row.Address1, Address2 = row.Address2, Address3 = row.Address3,
        Ward = row.Ward, City = row.City, Description = row.Description, IsDeleted = row.IsDeleted,
        CreatedAtUtc = row.CreatedAtUtc, CreatedByUserId = row.CreatedByUserId,
        UpdatedAtUtc = row.UpdatedAtUtc, UpdatedByUserId = row.UpdatedByUserId
    };

    private static void OnRowRender(RowRenderEventArgs<SupplierResDTO> args)
    {
        if (args.Data?.IsDeleted == true)
            args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current) ? $"{current} vpp-admin-row-deleted" : "vpp-admin-row-deleted";
    }

    public void Dispose() { searchDebounce?.Cancel(); searchDebounce?.Dispose(); }
}
