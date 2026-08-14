// PAGE LOGIC: Lib/Tabs/Tab_CategoryLibrary.razor.cs
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

public partial class Tab_CategoryLibrary : VppServerGridComponentBase<VppCategoryResDTO>, IDisposable
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    [Inject] public CatalogApiClient CatalogApi { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public CurrentUserState CurrentUserState { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<VppCategoryResDTO> rows = [];
    private RadzenDataGrid<VppCategoryResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private string searchText = string.Empty;
    private string selectedActivity = string.Empty;
    private bool isLoading;
    private CancellationTokenSource? searchDebounce;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText)
        || !string.IsNullOrWhiteSpace(selectedActivity);
    private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);

    protected override RadzenDataGrid<VppCategoryResDTO>? InitialGrid => grid;

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
            var result = await CatalogApi.GetCategoriesAsync(new CatalogQuery(
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
        var result = await DialogService.OpenAsync<Dialog.Dialog_CategoryEditor>(
            Loc["AddCategory"].Value,
            new Dictionary<string, object?> { ["IsCreate"] = true },
            VppAdminDialogProfiles.Create(VppAdminDialogSize.Compact, Loc["AddCategory"].Value, closeAriaLabel: Loc["Close"].Value));

        if (result is VppCategoryResDTO)
        {
            await grid.Reload();
        }
    }

    private async Task OpenEditAsync(VppCategoryResDTO row)
    {
        var model = Clone(row);
        var result = await DialogService.OpenAsync<Dialog.Dialog_CategoryEditor>(
            Loc["Edit"].Value,
            new Dictionary<string, object?> { ["IsCreate"] = false, ["Model"] = model },
            VppAdminDialogProfiles.Create(VppAdminDialogSize.Compact, Loc["Edit"].Value, closeAriaLabel: Loc["Close"].Value));

        if (result is VppCategoryResDTO)
        {
            await grid.Reload();
        }
    }

    private async Task ToggleDeletedAsync(VppCategoryResDTO row, bool value)
    {
        try
        {
            var result = await CatalogApi.SetCategoryDeletedAsync(
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

    private async Task HardDeleteAsync(VppCategoryResDTO row)
    {
        if (!CanModify || !row.IsDeleted) return;
        var confirm = await DialogService.Confirm(
            $"{row.VppCategoryName ?? row.VppCategoryCode}\n\n{Loc["PermanentDeleteWarning"]}",
            Loc["HardDelete"].Value,
            new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirm != true) return;

        try
        {
            await CatalogApi.DeleteCategoryAsync(row.Id);
            ToastService.Show(NotificationSeverity.Success, Loc["Success"], Loc["RecordPermanentlyDeleted"], 3000, false);
            await grid.Reload();
        }
        catch (Exception ex) { ToastService.Error(ex, Loc, "DeleteRecordFailed"); }
    }

    private async Task OnSearchInputAsync(ChangeEventArgs args)
    {
        searchText = args.Value?.ToString() ?? string.Empty;
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
        searchDebounce = new CancellationTokenSource();

        try
        {
            await Task.Delay(280, searchDebounce.Token);
            await grid.FirstPage(true);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ClearFiltersAsync()
    {
        searchText = string.Empty;
        selectedActivity = string.Empty;
        await grid.FirstPage(true);
    }

    private async Task OnStatusChangedAsync(string value)
    {
        selectedActivity = value ?? string.Empty;
        await grid.FirstPage(true);
    }

    private static VppCategoryResDTO Clone(VppCategoryResDTO row)
        => new()
        {
            Id = row.Id,
            VppCategoryCode = row.VppCategoryCode,
            VppCategoryName = row.VppCategoryName,
            Description = row.Description,
            IsDeleted = row.IsDeleted,
            CreatedByUserId = row.CreatedByUserId,
            CreatedAtUtc = row.CreatedAtUtc,
            UpdatedByUserId = row.UpdatedByUserId,
            UpdatedAtUtc = row.UpdatedAtUtc
        };

    private void OnRowRender(RowRenderEventArgs<VppCategoryResDTO> args)
    {
        if (args.Data?.IsDeleted == true)
        {
            args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current)
                ? $"{current} vpp-admin-row-deleted"
                : "vpp-admin-row-deleted";
        }
    }

    public void Dispose()
    {
        searchDebounce?.Cancel();
        searchDebounce?.Dispose();
    }
}
