// PAGE LOGIC: Lib/Tabs/Tab_PriceListLibrary.razor.cs
using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_PriceListLibrary : VppServerGridComponentBase<PriceListResDTO>
    {
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] public PricingApiClient PricingApi { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<PriceListResDTO> priceLists = [];
        private List<SupplierResDTO> suppliers = [];
        private RadzenDataGrid<PriceListResDTO> grid = default!;
        private bool isLoading;
        private int count;
        private int currentSkip;
        private string selectedActivity = string.Empty;
        private string searchText = string.Empty;
        private bool HasFilters => !string.IsNullOrWhiteSpace(selectedActivity) || !string.IsNullOrWhiteSpace(searchText);
        private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);
        protected override RadzenDataGrid<PriceListResDTO>? InitialGrid => grid;

        private IReadOnlyList<VppFilterOption<string>> PriceListStatusOptions =>
        [
            new(string.Empty, Loc["LibraryAllStatuses"].Value),
            new("active", Loc["LibraryStatusActive"].Value),
            new("inactive", Loc["LibraryStatusInactive"].Value)
        ];

        protected override async Task OnInitializedAsync()
        {
            try
            {
                suppliers = await PricingApi.GetActiveSuppliersAsync() ?? [];
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
        }

        private async Task LoadAsync()
        {
            if (grid is not null)
            {
                await grid.Reload();
            }
        }

        private async Task LoadDataAsync(LoadDataArgs args)
        {
            isLoading = true;
            currentSkip = args.Skip ?? 0;
            try
            {
                var result = await PricingApi.GetPriceListsAsync(new PriceListQuery(
                    args.Skip ?? 0,
                    args.Top ?? 20,
                    searchText,
                    string.IsNullOrWhiteSpace(selectedActivity) ? null : selectedActivity,
                    args.OrderBy));
                priceLists = result.Items.ToList();
                count = result.TotalCount;
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task AddAsync()
        {
            var result = await OpenEditorAsync(Loc["AddPriceList"].Value, new PriceListUpdateReqDTO());
            if (result is null) return;

            try
            {
                await PricingApi.CreatePriceListAsync(
                    new PriceListCreateReqDTO
                    {
                        Code = result.Code,
                        Name = result.Name,
                        Description = result.Description,
                        IsDefault = result.IsDefault,
                        SupplierId = result.SupplierId,
                        Version = result.Version,
                        EffectiveFromUtc = result.EffectiveFromUtc,
                        EffectiveToUtc = result.EffectiveToUtc,
                        CurrencyCode = result.CurrencyCode,
                        ContractCode = result.ContractCode,
                        DiscountRate = result.DiscountRate,
                        RebateAmount = result.RebateAmount,
                        FeeAmount = result.FeeAmount,
                        ShippingAmount = result.ShippingAmount
                    });
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListSaved"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "CreateRecordFailed");
            }
        }

        private async Task EditAsync(PriceListResDTO row)
        {
            var result = await OpenEditorAsync(Loc["Edit"].Value, new PriceListUpdateReqDTO
            {
                Id = row.Id,
                Code = row.PriceListCode,
                Name = row.PriceListName,
                Description = row.Description,
                IsDefault = row.IsDefault,
                SupplierId = row.SupplierId,
                Version = row.Version,
                EffectiveFromUtc = row.EffectiveFromUtc,
                EffectiveToUtc = row.EffectiveToUtc,
                CurrencyCode = row.CurrencyCode,
                VatPolicy = row.VatPolicy,
                ContractCode = row.ContractCode,
                DiscountRate = row.DiscountRate,
                RebateAmount = row.RebateAmount,
                FeeAmount = row.FeeAmount,
                ShippingAmount = row.ShippingAmount,
                RowVersion = row.RowVersion
            });
            if (result is null) return;

            try
            {
                await PricingApi.UpdatePriceListAsync(row.Id, result);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListSaved"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "UpdateRecordFailed");
            }
        }

        private async Task SetDeletedAsync(PriceListResDTO row, bool isDeleted)
        {
            var previous = row.IsDeleted;
            row.IsDeleted = isDeleted;

            try
            {
                var result = await PricingApi.SetPriceListDeletedAsync(row.Id, isDeleted);

                if (result is null)
                {
                    row.IsDeleted = previous;
                    Notify(NotificationSeverity.Error, Loc["Error"].Value, Loc["DeleteFailed"].Value);
                    return;
                }

                Notify(NotificationSeverity.Success, Loc["Success"].Value, isDeleted ? Loc["PriceListDeactivated"].Value : Loc["PriceListRestored"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                row.IsDeleted = previous;
                _toastService.Error(ex, Loc, "ChangeRecordStatusFailed");
            }
        }

        private async Task HardDeleteAsync(PriceListResDTO row)
        {
            if (!CanModify || !row.IsDeleted) return;
            var confirm = await DialogService.Confirm(
                $"{Loc["PriceListHardDeleteConfirm"]}\n\n{Loc["PermanentDeleteWarning"]}",
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
            if (confirm != true) return;

            try
            {
                await PricingApi.HardDeletePriceListAsync(row.Id);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["RecordPermanentlyDeleted"].Value);
                await LoadAsync();
            }
            catch (Exception ex) { _toastService.Error(ex, Loc, "DeleteRecordFailed"); }
        }

        private async Task OnStatusChangedAsync(string value)
        {
            selectedActivity = value ?? string.Empty;
            await LoadAsync();
        }

        private async Task OnSearchInputAsync(ChangeEventArgs args)
        {
            searchText = args.Value?.ToString() ?? string.Empty;
            await grid.FirstPage(true);
        }

        private async Task ClearFiltersAsync()
        {
            searchText = string.Empty;
            selectedActivity = string.Empty;
            await grid.FirstPage(true);
        }

        private void OnRowRenderPriceList(RowRenderEventArgs<PriceListResDTO> args)
        {
            if (args.Data?.IsDeleted == true)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
        }

        private async Task SetDefaultAsync(PriceListResDTO row)
        {
            try
            {
                await PricingApi.SetDefaultPriceListAsync(row.Id);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["DefaultUpdated"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "UpdateRecordFailed");
            }
        }

        private async Task CloneAsync(PriceListResDTO row)
        {
            var result = await OpenEditorAsync(Loc["CopyAsNewPriceList"].Value, new PriceListUpdateReqDTO
            {
                Code = $"{row.PriceListCode}-COPY",
                Name = $"{row.PriceListName} Copy",
                Description = row.Description
            }, isClone: true);
            if (result is null) return;

            try
            {
                await PricingApi.ClonePriceListAsync(
                    new PriceListCloneReqDTO
                    {
                        SourceId = row.Id,
                        Code = result.Code,
                        Name = result.Name,
                        Description = result.Description
                    });
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListCloned"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "CreateRecordFailed");
            }
        }

        private Task OpenPricesAsync(PriceListResDTO row)
        {
            NavigationManager.NavigateTo($"/library?tab=6&pricingTab=prices&priceListId={row.Id}");
            return Task.CompletedTask;
        }

        private string PriceListPrimaryActionText(PriceListResDTO row) => Loc["View"].Value;

        private IReadOnlyList<VppAdminActionMenuItem> PriceListSecondaryActions(PriceListResDTO row) =>
        [
            new(
                "edit",
                Loc["Edit"].Value,
                 "edit",
                 () => EditAsync(row),
                 row.IsDeleted || row.Status == "Expired",
                 DisabledReason: Loc["RequestActionUnavailable"].Value),
            new(
                "default",
                Loc["SetAsDefault"].Value,
                 "star",
                 () => SetDefaultAsync(row),
                 row.IsDefault || !IsPriceListActive(row),
                 DisabledReason: row.IsDefault ? Loc["Default"].Value : Loc["RequestActionUnavailable"].Value),
            new(
                "clone",
                Loc["CopyAsNewPriceList"].Value,
                 "content_copy",
                 () => CloneAsync(row),
                 !IsPriceListActive(row),
                 DisabledReason: Loc["RequestActionUnavailable"].Value),
            new(
                "toggle-active",
                IsPriceListActive(row) ? Loc["Deactivate"].Value : Loc["Restore"].Value,
                 IsPriceListActive(row) ? "block" : "restore_from_trash",
                 () => SetDeletedAsync(row, IsPriceListActive(row)),
                 row.IsDefault,
                 DisabledReason: row.IsDefault ? Loc["RequestActionUnavailable"].Value : null),
            new(
                "hard-delete",
                Loc["HardDelete"].Value,
                 "delete_forever",
                 () => HardDeleteAsync(row),
                 !row.IsDeleted,
                 VppAdminActionTone.Danger,
                 Loc["RequestActionUnavailable"].Value)
         ];

        private async Task<PriceListUpdateReqDTO?> OpenEditorAsync(
            string title,
            PriceListUpdateReqDTO model,
            bool isClone = false)
        {
            var result = await DialogService.OpenAsync<Dialog_PriceListEditor>(
                title,
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_PriceListEditor.Model)] = model,
                    [nameof(Dialog_PriceListEditor.IsClone)] = isClone,
                    [nameof(Dialog_PriceListEditor.Suppliers)] = suppliers
                },
                VppAdminDialogProfiles.Create(VppAdminDialogSize.Standard, title, closeAriaLabel: Loc["Close"].Value));

            return result as PriceListUpdateReqDTO;
        }

        private async Task ReloadGridAsync()
        {
            if (grid is not null)
            {
                await grid.Reload();
            }
            StateHasChanged();
        }

        private void Notify(NotificationSeverity severity, string summary, string detail)
        {
            _toastService.Show(severity, summary, detail, 5000, false);
        }

        private string GetStatusLabel(PriceListResDTO row) =>
            IsPriceListActive(row) ? Loc["LibraryStatusActive"] : Loc["LibraryStatusInactive"];

        private static VppStatusTone GetStatusTone(PriceListResDTO row) =>
            IsPriceListActive(row) ? VppStatusTone.Success : VppStatusTone.Neutral;

        private static bool IsPriceListActive(PriceListResDTO row) =>
            !row.IsDeleted && row.Status == "Published";

        // Atlas cột "Hiệu lực": khoảng dd/MM–dd/MM (kèm năm khi khác năm hiện tại);
        // bảng giá chưa có ngày kết thúc hiển thị mũi tên mở.
        private static string FormatEffectiveRange(PriceListResDTO row)
        {
            var from = row.EffectiveFromUtc.ToLocalTime();
            var fromText = from.ToString("dd/MM/yyyy");
            if (!row.EffectiveToUtc.HasValue)
            {
                return $"{fromText} →";
            }

            var to = row.EffectiveToUtc.Value.ToLocalTime();
            return $"{fromText} – {to:dd/MM/yyyy}";
        }

        private static void AppendRowClass(IDictionary<string, object> attributes, string className)
        {
            if (attributes.TryGetValue("class", out var current) && current is not null)
            {
                attributes["class"] = $"{current} {className}";
                return;
            }

            attributes["class"] = className;
        }

    }
}
