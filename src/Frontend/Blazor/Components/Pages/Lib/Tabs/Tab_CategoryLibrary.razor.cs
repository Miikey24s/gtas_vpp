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

public partial class Tab_CategoryLibrary : VppServerGridComponentBase<VppCategoryResDTO>, IDisposable
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();

    [Inject] public IAPIServices ApiServices { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public GlobalClass Global { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;

    private List<VppCategoryResDTO> rows = [];
    private RadzenDataGrid<VppCategoryResDTO> grid = default!;
    private int totalCount;
    private int currentSkip;
    private string searchText = string.Empty;
    private bool isLoading;
    private CancellationTokenSource? searchDebounce;

    private bool HasFilters => !string.IsNullOrWhiteSpace(searchText);
    private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);

    protected override RadzenDataGrid<VppCategoryResDTO>? InitialGrid => grid;

    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true;
        currentSkip = args.Skip ?? 0;
        var searchFilter = BuildSearchFilter();

        try
        {
            var query = BuildQuery(searchFilter, args.Skip, args.Top, args.OrderBy);
            var result = await ApiServices.GetFromApiWithTotalCountAsync<List<VppCategoryResDTO>>(query);
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
            var result = await ApiServices.PatchFromApiAsync<VppCategoryResDTO>(
                $"{Config.LibraryApi.VppCategories}/{row.Id}",
                new { IsDeleted = value, UpdatedAtUtc = DateTime.Now, UpdatedByUserId = Global.UserInfo.UserID });

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
        await grid.FirstPage(true);
    }

    private string? BuildSearchFilter()
    {
        var search = searchText.Trim();
        if (string.IsNullOrWhiteSpace(search)) return null;

        var escaped = search.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).ToLowerInvariant();
        return $"((VppCategoryCode ?? \"\").ToLower().Contains(\"{escaped}\") || (VppCategoryName ?? \"\").ToLower().Contains(\"{escaped}\"))";
    }

    private static string BuildQuery(string? filter, int? skip, int? top, string? orderby)
    {
        var query = new List<string> { "showDeleted=true" };
        if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}");
        if (skip.HasValue) query.Add($"skip={skip.Value}");
        if (top.HasValue) query.Add($"top={top.Value}");
        if (!string.IsNullOrWhiteSpace(orderby)) query.Add($"orderby={Uri.EscapeDataString(orderby)}");
        return $"{Config.LibraryApi.VppCategories}?{string.Join("&", query)}";
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
