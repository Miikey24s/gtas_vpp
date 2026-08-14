// PAGE LOGIC: Lib/Tabs/Tab_SupplierLibrary.razor.cs
using gtas_vpp_fe.Features.IdentityAccess.State;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs;

public partial class Tab_SupplierLibrary : VppServerGridComponentBase<SupplierResDTO>, IDisposable
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
    [Inject] public CatalogApiClient CatalogApi { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public CurrentUserState CurrentUserState { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<SupplierResDTO> rows = [];
    private RadzenDataGrid<SupplierResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private string searchText = string.Empty;
    private string selectedActivity = string.Empty;
    private bool isLoading;
    private CancellationTokenSource? searchDebounce;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || !string.IsNullOrWhiteSpace(selectedActivity);
    private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);

    protected override RadzenDataGrid<SupplierResDTO>? InitialGrid => grid;

    private IReadOnlyList<VppFilterOption<string>> StatusOptions =>
    [
        new(string.Empty, Loc["LibraryAllStatuses"]),
        new("active", Loc["LibraryStatusActive"]),
        new("inactive", Loc["LibraryStatusInactive"])
    ];

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true;
        currentSkip = args.Skip ?? 0;
        try
        {
            var result = await CatalogApi.GetSuppliersAsync(new CatalogQuery(
                args.Skip ?? 0,
                args.Top ?? VppPagingProfiles.Collection.DefaultPageSize,
                searchText,
                args.OrderBy,
                selectedActivity));
            rows = result.Items.ToList();
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
        if (value && !await CanDeactivateAsync(row.Id))
        {
            return;
        }

        try
        {
            var result = await CatalogApi.SetSupplierDeletedAsync(
                row.Id,
                new CatalogStatusChange(
                    value,
                    DateTime.Now,
                    CurrentUserState.Current?.UserId ?? 0));
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

    private async Task HardDeleteAsync(SupplierResDTO row)
    {
        if (!CanModify || !row.IsDeleted) return;
        var confirm = await DialogService.Confirm(
            $"{row.SupplierName ?? row.SupplierShortName}\n\n{Loc["PermanentDeleteWarning"]}",
            Loc["HardDelete"].Value,
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirm != true) return;
        try
        {
            await CatalogApi.DeleteSupplierAsync(row.Id);
            ToastService.Show(NotificationSeverity.Success, Loc["Success"], Loc["RecordPermanentlyDeleted"], 3000, false);
            await grid.Reload();
        }
        catch (Exception ex) { ToastService.Error(ex, Loc, "DeleteRecordFailed"); }
    }

    private async Task<bool> CanDeactivateAsync(Guid id)
    {
        var impact = await CatalogApi.GetSupplierDependencyImpactAsync(id);
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

    private async Task OnStatusChangedAsync(string value)
    {
        selectedActivity = value ?? string.Empty;
        await grid.FirstPage(true);
    }

    private async Task ClearFiltersAsync()
    {
        searchText = string.Empty;
        selectedActivity = string.Empty;
        await grid.FirstPage(true);
    }

    private static string FormatSupplierAddress(SupplierResDTO row)
    {
        var parts = new[] { row.Address1, row.Address2, row.Address3, row.Ward, row.City }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var value = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(value) ? "–" : value;
    }

    private static SupplierResDTO Clone(SupplierResDTO row) => new()
    {
        Id = row.Id,
        SupplierShortName = row.SupplierShortName,
        SupplierName = row.SupplierName,
        Address1 = row.Address1,
        Address2 = row.Address2,
        Address3 = row.Address3,
        Ward = row.Ward,
        City = row.City,
        ItemCount = row.ItemCount,
        PriceListCount = row.PriceListCount,
        Description = row.Description,
        IsDeleted = row.IsDeleted,
        CreatedAtUtc = row.CreatedAtUtc,
        CreatedByUserId = row.CreatedByUserId,
        UpdatedAtUtc = row.UpdatedAtUtc,
        UpdatedByUserId = row.UpdatedByUserId
    };

    private static void OnRowRender(RowRenderEventArgs<SupplierResDTO> args)
    {
        if (args.Data?.IsDeleted == true)
            args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current) ? $"{current} vpp-admin-row-deleted" : "vpp-admin-row-deleted";
    }

    public void Dispose() { searchDebounce?.Cancel(); searchDebounce?.Dispose(); }
}
