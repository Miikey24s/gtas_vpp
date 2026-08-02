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

public partial class Tab_DepartmentLibrary : VppServerGridComponentBase<DepartmentResDTO>
{
    [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
    [Inject] public IAPIServices ApiServices { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public GlobalClass Global { get; set; } = default!;
    [Inject] public DialogService DialogService { get; set; } = default!;
    private List<DepartmentResDTO> rows = [];
    private List<DepartmentResDTO> allDepartments = [];
    private RadzenDataGrid<DepartmentResDTO> grid = default!;
    private int totalCount; private int currentSkip; private bool isLoading; private string searchText = string.Empty; private bool HasFilters => !string.IsNullOrWhiteSpace(searchText); private bool CanModify => PagePermissionResDTO.Components.Any(x => x.IsVisible && x.IsEnable);
    protected override RadzenDataGrid<DepartmentResDTO>? InitialGrid => grid;

    protected override async Task OnInitializedAsync()
    {
        try { allDepartments = await ApiServices.GetFromApiAsync<List<DepartmentResDTO>>($"{Config.LibraryApi.Departments}?showDeleted=false") ?? []; } catch (Exception ex) { ToastService.Error(ex, Loc, "LoadLibraryDataFailed"); }
    }
    private async Task LoadDataAsync(LoadDataArgs args)
    {
        isLoading = true; currentSkip = args.Skip ?? 0; var searchFilter = BuildSearchFilter();
        try { var result = await ApiServices.GetFromApiWithTotalCountAsync<List<DepartmentResDTO>>(BuildQuery(searchFilter, args.Skip, args.Top, args.OrderBy)); rows = result.Data ?? []; totalCount = result.TotalCount; }
        catch (Exception ex) { rows = []; totalCount = 0; ToastService.Error(ex, Loc, "LoadLibraryDataFailed"); }
        finally { isLoading = false; StateHasChanged(); }
    }
    private async Task OpenCreateAsync()
    { var result = await DialogService.OpenAsync<Dialog.Dialog_DepartmentEditor>(Loc["AddDepartment"].Value, new Dictionary<string, object?> { ["IsCreate"] = true, ["Departments"] = allDepartments }, VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["AddDepartment"].Value, closeAriaLabel: Loc["Close"].Value)); if (result is DepartmentResDTO) await grid.Reload(); }
    private async Task OpenEditAsync(DepartmentResDTO row)
    { var result = await DialogService.OpenAsync<Dialog.Dialog_DepartmentEditor>(Loc["Edit"].Value, new Dictionary<string, object?> { ["IsCreate"] = false, ["Model"] = Clone(row), ["Departments"] = allDepartments }, VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, Loc["Edit"].Value, closeAriaLabel: Loc["Close"].Value)); if (result is DepartmentResDTO) await grid.Reload(); }
    private async Task ToggleDeletedAsync(DepartmentResDTO row, bool value)
    { if (value && !await CanDeactivateAsync(Config.LibraryApi.Departments, row.Id)) return; try { var result = await ApiServices.PatchFromApiAsync<DepartmentResDTO>($"{Config.LibraryApi.Departments}/{row.Id}", new { IsDeleted = value, UpdatedAtUtc = DateTime.Now, UpdatedByUserId = Global.UserInfo.UserID }); if (result is null) { row.IsDeleted = !value; ToastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true); return; } row.IsDeleted = result.IsDeleted; await grid.Reload(); } catch (Exception ex) { row.IsDeleted = !value; ToastService.Error(ex, Loc, "ChangeRecordStatusFailed"); } }
    private async Task HardDeleteAsync(DepartmentResDTO row)
    {
        if (!CanModify || !row.IsDeleted) return;
        var confirm = await DialogService.Confirm($"{row.Name ?? row.Code}\n\n{Loc["PermanentDeleteWarning"]}", Loc["HardDelete"].Value, new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
        if (confirm != true) return;
        try { await ApiServices.DeleteFromApiAsync($"{Config.LibraryApi.Departments}/{row.Id}"); ToastService.Show(NotificationSeverity.Success, Loc["Success"], Loc["RecordPermanentlyDeleted"], 3000, false); await grid.Reload(); }
        catch (Exception ex) { ToastService.Error(ex, Loc, "DeleteRecordFailed"); }
    }
    private async Task<bool> CanDeactivateAsync(string endpoint, Guid id) { var impact = await ApiServices.GetFromApiAsync<LibraryDependencyImpactResDTO>($"{endpoint}/{id}/dependency-impact"); if (impact is null || impact.CanDeactivate) return true; ToastService.Show(NotificationSeverity.Warning, Loc["ValidationTitle"], string.Format(Loc["DependencyDeactivateBlocked"].Value, impact.ActiveReferenceCount), 5000, false); return false; }
    private async Task OnSearchInputAsync(ChangeEventArgs args) { searchText = args.Value?.ToString() ?? string.Empty; await grid.FirstPage(true); }
    private async Task ClearFiltersAsync() { searchText = string.Empty; await grid.FirstPage(true); }
    private string? BuildSearchFilter() { var search = searchText.Trim(); if (string.IsNullOrWhiteSpace(search)) return null; var escaped = search.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).ToLowerInvariant(); return $"((Code ?? \"\").ToLower().Contains(\"{escaped}\") || (Name ?? \"\").ToLower().Contains(\"{escaped}\"))"; }
    private static string BuildQuery(string? filter, int? skip, int? top, string? orderby) { var query = new List<string> { "showDeleted=true" }; if (!string.IsNullOrWhiteSpace(filter)) query.Add($"filter={Uri.EscapeDataString(filter)}"); if (skip.HasValue) query.Add($"skip={skip.Value}"); if (top.HasValue) query.Add($"top={top.Value}"); if (!string.IsNullOrWhiteSpace(orderby)) query.Add($"orderby={Uri.EscapeDataString(orderby)}"); return $"{Config.LibraryApi.Departments}?{string.Join("&", query)}"; }
    private string GetParentName(Guid? id) => id.HasValue ? allDepartments.FirstOrDefault(x => x.Id == id)?.Name ?? "–" : "–";
    private static DepartmentResDTO Clone(DepartmentResDTO row) => new() { Id = row.Id, Code = row.Code, Name = row.Name, ParentDepartmentId = row.ParentDepartmentId, Description = row.Description, IsDeleted = row.IsDeleted, CreatedAtUtc = row.CreatedAtUtc, CreatedByUserId = row.CreatedByUserId, UpdatedAtUtc = row.UpdatedAtUtc, UpdatedByUserId = row.UpdatedByUserId };
    private static void OnRowRender(RowRenderEventArgs<DepartmentResDTO> args) { if (args.Data?.IsDeleted == true) args.Attributes["class"] = args.Attributes.TryGetValue("class", out var current) ? $"{current} vpp-admin-row-deleted" : "vpp-admin-row-deleted"; }
}
