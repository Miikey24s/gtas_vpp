using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
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
    public partial class Tab_PriceListLibrary
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Inject] public DialogService DialogService { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private List<L07_PriceListResDTO> priceLists = [];
        private RadzenDataGrid<L07_PriceListResDTO> grid = default!;
        private bool isLoading;

        protected override async Task OnInitializedAsync()
        {
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            isLoading = true;
            try
            {
                priceLists = await _apiServices.GetFromApiAsync<List<L07_PriceListResDTO>>($"{Config.LibraryApi.L07_PriceList}?showDeleted=true") ?? [];
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
            finally
            {
                isLoading = false;
                await ReloadGridAsync();
            }
        }

        private async Task AddAsync()
        {
            var result = await OpenEditorAsync(Loc["AddPriceList"].Value, new L07_PriceListUpdateReqDTO());
            if (result is null) return;

            try
            {
                await _apiServices.PostFromApiAsync<L07_PriceListResDTO>(
                    Config.LibraryApi.L07_PriceList,
                    new L07_PriceListCreateReqDTO
                    {
                        Code = result.Code,
                        Name = result.Name,
                        Description = result.Description,
                        IsDefault = result.IsDefault
                    });
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListSaved"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task EditAsync(L07_PriceListResDTO row)
        {
            var result = await OpenEditorAsync(Loc["Edit"].Value, new L07_PriceListUpdateReqDTO
            {
                Id = row.Id,
                Code = row.PriceListCode,
                Name = row.PriceListName,
                Description = row.Description,
                IsDefault = row.IsDefault
            });
            if (result is null) return;

            try
            {
                await _apiServices.PutFromApiAsync<L07_PriceListResDTO>(
                    $"{Config.LibraryApi.L07_PriceList}/{row.Id}", result);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListSaved"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task DeleteAsync(L07_PriceListResDTO row)
        {
            var confirm = await DialogService.Confirm(
                Loc["ConfirmDeletePriceList"],
                Loc["Delete"],
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            try
            {
                var deleted = await _apiServices.DeleteFromApiAsync($"{Config.LibraryApi.L07_PriceList}/{row.Id}");
                if (!deleted)
                {
                    Notify(NotificationSeverity.Error, Loc["Error"].Value, Loc["DeleteFailed"].Value);
                    return;
                }

                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["PriceListDeleted"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task SetDefaultAsync(L07_PriceListResDTO row)
        {
            try
            {
                await _apiServices.PostFromApiAsync<object>(
                    string.Format(Config.LibraryApi.L07_PriceList_SetDefault, row.Id), null);
                Notify(NotificationSeverity.Success, Loc["Success"].Value, Loc["DefaultUpdated"].Value);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private async Task CloneAsync(L07_PriceListResDTO row)
        {
            var result = await OpenEditorAsync(Loc["Clone"].Value, new L07_PriceListUpdateReqDTO
            {
                Code = $"{row.PriceListCode}-COPY",
                Name = $"{row.PriceListName} Copy",
                Description = row.Description
            }, isClone: true);
            if (result is null) return;

            try
            {
                await _apiServices.PostFromApiAsync<L07_PriceListResDTO>(
                    Config.LibraryApi.L07_PriceList_Clone,
                    new L07_PriceListCloneReqDTO
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
                Notify(NotificationSeverity.Error, Loc["Error"].Value, ex.Message);
            }
        }

        private Task OpenPricesAsync(L07_PriceListResDTO row)
        {
            NavigationManager.NavigateTo($"/library?tab=4&priceListId={row.Id}");
            return Task.CompletedTask;
        }

        private async Task<L07_PriceListUpdateReqDTO?> OpenEditorAsync(
            string title,
            L07_PriceListUpdateReqDTO model,
            bool isClone = false)
        {
            var result = await DialogService.OpenAsync<Dialog_PriceListEditor>(
                title,
                new Dictionary<string, object?>
                {
                    [nameof(Dialog_PriceListEditor.Model)] = model,
                    [nameof(Dialog_PriceListEditor.IsClone)] = isClone
                },
                new DialogOptions { Width = "520px", Resizable = true, Draggable = true });

            return result as L07_PriceListUpdateReqDTO;
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
            _notificationService.CustomContentNotification(severity, summary, detail, 5000, false);
        }
    }
}
