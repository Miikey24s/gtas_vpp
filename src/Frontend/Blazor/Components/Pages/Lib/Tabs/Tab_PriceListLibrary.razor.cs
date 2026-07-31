using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_PriceListLibrary : VppServerGridComponentBase<PriceListResDTO>
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<PriceListResDTO> priceLists = [];
        private List<SupplierResDTO> suppliers = [];
        private RadzenDataGrid<PriceListResDTO> grid = default!;
        private bool isLoading;
        private int count;
        private int currentSkip;
        private string selectedStatus = string.Empty;
        private string searchText = string.Empty;
        private bool HasFilters => !string.IsNullOrWhiteSpace(selectedStatus) || !string.IsNullOrWhiteSpace(searchText);
        private bool CanModify => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);
        protected override RadzenDataGrid<PriceListResDTO>? InitialGrid => grid;

        private IReadOnlyList<VppFilterOption<string>> PriceListStatusOptions =>
        [
            new(string.Empty, Loc["LibraryAllStatuses"].Value),
            new("Draft", Loc["PriceListStatusDraft"].Value),
            new("Published", Loc["PriceListStatusPublished"].Value),
            new("Expired", Loc["PriceListStatusExpired"].Value)
        ];

        protected override async Task OnInitializedAsync()
        {
            try
            {
                suppliers = await _apiServices.GetFromApiAsync<List<SupplierResDTO>>(
                    $"{Config.LibraryApi.Suppliers}?showDeleted=false") ?? [];
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
                var endpoint = BuildPriceListEndpoint(SelectedStatusFilter, args.Skip ?? 0, args.Top ?? 20, args.OrderBy);
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<PriceListResDTO>>(endpoint);
                priceLists = result.Data ?? [];
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
                await _apiServices.PostFromApiAsync<PriceListResDTO>(
                    Config.LibraryApi.PriceList,
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
                await _apiServices.PutFromApiAsync<PriceListResDTO>(
                    $"{Config.LibraryApi.PriceList}/{row.Id}", result);
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
                var result = await _apiServices.PatchFromApiAsync<PriceListResDTO>(
                    $"{Config.LibraryApi.PriceList}/{row.Id}/deleted",
                    new { IsDeleted = isDeleted });

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
            if (!CanModify || !row.IsDeleted || row.Status == "Published") return;
            var confirm = await DialogService.Confirm(
                $"{Loc["PriceListHardDeleteConfirm"]}\n\n{Loc["PermanentDeleteWarning"]}",
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
            if (confirm != true) return;

            try
            {
                await _apiServices.DeleteFromApiAsync($"{Config.LibraryApi.PriceList}/{row.Id}/hard");
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["RecordPermanentlyDeleted"].Value);
                await LoadAsync();
            }
            catch (Exception ex) { _toastService.Error(ex, Loc, "DeleteRecordFailed"); }
        }

        private async Task OnStatusChangedAsync(string value)
        {
            selectedStatus = value ?? string.Empty;
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
            selectedStatus = string.Empty;
            await grid.FirstPage(true);
        }

        private string? SelectedStatusFilter => string.IsNullOrWhiteSpace(selectedStatus)
            ? null
            : $"Status == \"{selectedStatus}\"";

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
                await _apiServices.PostFromApiAsync<object>(
                    string.Format(Config.LibraryApi.PriceList_SetDefault, row.Id), null);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["DefaultUpdated"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "UpdateRecordFailed");
            }
        }

        private async Task PublishAsync(PriceListResDTO row)
        {
            var confirm = await DialogService.Confirm(
                Loc["PriceListPublishConfirm"].Value,
                Loc["PriceListPublishTitle"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
            if (confirm != true) return;

            try
            {
                await _apiServices.PostFromApiAsync<PriceListResDTO>(
                    string.Format(Config.LibraryApi.PriceList_Publish, row.Id),
                    new PriceBookStatusReqDTO
                    {
                        RowVersion = row.RowVersion,
                        Reason = "Approved by procurement admin"
                    });
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListPublished"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "UpdateRecordFailed");
            }
        }

        private async Task ExpireAsync(PriceListResDTO row)
        {
            var confirm = await DialogService.Confirm(
                Loc["PriceListExpireConfirm"].Value,
                Loc["PriceListExpireTitle"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });
            if (confirm != true) return;

            try
            {
                await _apiServices.PostFromApiAsync<PriceListResDTO>(
                    string.Format(Config.LibraryApi.PriceList_Expire, row.Id),
                    new PriceBookStatusReqDTO
                    {
                        RowVersion = row.RowVersion,
                        Reason = "Expired by procurement admin",
                        EffectiveToUtc = DateTime.UtcNow
                    });
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListExpired"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "UpdateRecordFailed");
            }
        }

        private async Task CloneAsync(PriceListResDTO row)
        {
            var result = await OpenEditorAsync(Loc["Clone"].Value, new PriceListUpdateReqDTO
            {
                Code = $"{row.PriceListCode}-COPY",
                Name = $"{row.PriceListName} Copy",
                Description = row.Description
            }, isClone: true);
            if (result is null) return;

            try
            {
                await _apiServices.PostFromApiAsync<PriceListResDTO>(
                    Config.LibraryApi.PriceList_Clone,
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
            NavigationManager.NavigateTo($"/library?tab=4&priceListId={row.Id}");
            return Task.CompletedTask;
        }

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
                VppAdminDialogProfiles.Create(VppAdminDialogSize.Workspace, title, closeAriaLabel: Loc["Close"].Value));

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

        private static string GetStatusLabel(string? status) => status switch
        {
            "Draft" => "Bản nháp",
            "Published" => "Đã công bố",
            "Expired" => "Hết hiệu lực",
            _ => status ?? "Chưa xác định"
        };

        private static VppStatusTone GetStatusTone(string? status) => status switch
        {
            "Published" => VppStatusTone.Success,
            "Draft" => VppStatusTone.Info,
            // Atlas library-price-lists: "Hết hiệu lực" là badge amber (cảnh báo), không phải xám.
            "Expired" => VppStatusTone.Warning,
            _ => VppStatusTone.Neutral
        };

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

        private string BuildPriceListEndpoint(
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null)
        {
            var query = new List<string> { "showDeleted=true" };

            if (!string.IsNullOrWhiteSpace(filter))
            {
                query.Add($"filter={Uri.EscapeDataString(filter)}");
            }

            if (skip.HasValue)
            {
                query.Add($"skip={skip.Value}");
            }

            if (top.HasValue)
            {
                query.Add($"top={top.Value}");
            }

            if (!string.IsNullOrWhiteSpace(orderby))
            {
                query.Add($"orderby={Uri.EscapeDataString(orderby)}");
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query.Add($"search={Uri.EscapeDataString(searchText.Trim())}");
            }

            return $"{Config.LibraryApi.PriceList}?{string.Join("&", query)}";
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
