using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Components.DesignSystem.Composites;
using gtas_vpp_fe.Features.CatalogPricing.Api;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Radzen.Blazor;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_LookupLibrary
    {
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] public LookupApiClient LookupApi { get; set; } = default!;
        [Inject] public CurrentUserState CurrentUserState { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;

        // Nhóm lookup.
        public List<LookupCategoryResDTO> lookupCategories { get; set; } = new List<LookupCategoryResDTO>();
        public IList<LookupCategoryResDTO> selectedLookupCategories { get; set; } = new List<LookupCategoryResDTO>();
        public LookupCategoryResDTO? selectedLookupCategory => selectedLookupCategories?.FirstOrDefault();
        public RadzenDataGrid<LookupCategoryResDTO> categoryGrid { get; set; } = default!;
        private bool isCategoryLoading { get; set; } = false;
        private int categoryCount { get; set; } = 0;
        private int currentCategorySkip { get; set; }
        private string categorySearchText = string.Empty;
        private string categoryStatusFilter = string.Empty;
        private bool hasAutoSelectedInitialCategory;

        // Giá trị lookup.
        public List<LookupValueResDTO> lookupValues { get; set; } = new List<LookupValueResDTO>();
        public RadzenDataGrid<LookupValueResDTO> valueGrid { get; set; } = default!;
        private bool isValueLoading { get; set; } = false;
        private int valueCount { get; set; } = 0;
        private int currentValueSkip { get; set; }
        private string valueSearchText = string.Empty;
        private string valueStatusFilter = string.Empty;
        private bool CanModifyLookup => PagePermissionResDTO.Components.Any(component => component.IsVisible && component.IsEnable);

        private IReadOnlyList<VppFilterOption<string>> CategoryStatusOptions =>
        [
            new(string.Empty, Loc["LibraryAllStatuses"]),
            new("active", Loc["Active"]),
            new("inactive", Loc["Inactive"])
        ];

        private IReadOnlyList<VppFilterOption<string>> ValueStatusOptions => CategoryStatusOptions;
        private bool HasCategoryFilters => !string.IsNullOrWhiteSpace(categorySearchText) || !string.IsNullOrWhiteSpace(categoryStatusFilter);
        private bool HasValueFilters => !string.IsNullOrWhiteSpace(valueSearchText) || !string.IsNullOrWhiteSpace(valueStatusFilter);

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                // Grid tự tải qua LoadData, nhưng vẫn có thể kích hoạt thủ công.
                await Task.CompletedTask;
            }
            else
            {
                UriHelper.NavigateTo("Home", true);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Kích hoạt lần tải đầu tiên.
                await categoryGrid.Reload();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        #region Lookup category methods

        protected async Task LoadCategories(LoadDataArgs args)
        {
            isCategoryLoading = true;
            var skip = Math.Max(0, args.Skip ?? 0);
            var top = args.Top is > 0 ? args.Top.Value : VppPagingProfiles.SplitList.DefaultPageSize;
            currentCategorySkip = skip;
            StateHasChanged();
            try
            {
                var result = await LookupApi.GetCategoriesAsync(new LookupQuery(
                    skip,
                    top,
                    categorySearchText,
                    categoryStatusFilter,
                    args.OrderBy));
                currentCategorySkip = result.AppliedSkip;
                lookupCategories = result.Items.ToList();
                categoryCount = result.TotalCount;

                if (!hasAutoSelectedInitialCategory
                    && selectedLookupCategory is null
                    && lookupCategories.Count > 0)
                {
                    hasAutoSelectedInitialCategory = true;
                    selectedLookupCategories = [lookupCategories[0]];
                    await valueGrid.Reload();
                }
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
            finally
            {
                isCategoryLoading = false;
                StateHasChanged();
            }
        }

        protected async Task OnCategorySelected(LookupCategoryResDTO data)
        {
            selectedLookupCategories = new List<LookupCategoryResDTO> { data };
            await valueGrid.Reload();
        }

        private async Task OnCategorySearchInputAsync(ChangeEventArgs args)
        {
            categorySearchText = args.Value?.ToString() ?? string.Empty;
            await categoryGrid.FirstPage(true);
        }

        private async Task OnCategoryStatusChangedAsync(string value)
        {
            categoryStatusFilter = value;
            await categoryGrid.FirstPage(true);
        }

        private async Task ClearCategoryFiltersAsync()
        {
            categorySearchText = string.Empty;
            categoryStatusFilter = string.Empty;
            await categoryGrid.FirstPage(true);
        }

        private async Task OnValueSearchInputAsync(ChangeEventArgs args)
        {
            valueSearchText = args.Value?.ToString() ?? string.Empty;
            await valueGrid.FirstPage(true);
        }

        private async Task OnValueStatusChangedAsync(string value)
        {
            valueStatusFilter = value;
            await valueGrid.FirstPage(true);
        }

        private async Task ClearValueFiltersAsync()
        {
            valueSearchText = string.Empty;
            valueStatusFilter = string.Empty;
            await valueGrid.FirstPage(true);
        }

        protected async Task ToggleCategoryDeleted(LookupCategoryResDTO data, bool isDeleted)
        {
            var previous = data.IsDeleted;
            try
            {
                if (isDeleted && !await CanDeactivateAsync(
                        () => LookupApi.GetCategoryDependencyImpactAsync(data.Id)))
                {
                    data.IsDeleted = previous;
                    StateHasChanged();
                    return;
                }

                var change = new LookupStatusChange(
                    isDeleted,
                    DateTime.Now,
                    CurrentUserState.Current?.UserId ?? 0);
                var result = await LookupApi.SetCategoryDeletedAsync(data.Id, change);
                if (result != null)
                {
                    data.IsDeleted = result.IsDeleted;
                    string message = isDeleted ? Loc["LookupCategoryDeactivated"].Value : Loc["LookupCategoryRestored"].Value;
                    _toastService.Show(NotificationSeverity.Success, Loc["Success"], message, 3000, false);
                    await categoryGrid.Reload();
                }
                else
                {
                    data.IsDeleted = previous;
                    _toastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = previous;
                _toastService.Error(ex, Loc, "ChangeRecordStatusFailed");
            }
        }

        protected async Task HardDeleteCategoryAsync(LookupCategoryResDTO data)
        {
            if (!data.IsDeleted || !CanModifyLookup) return;

            var confirm = await DialogService.Confirm(
                $"{Loc["LookupCategoryHardDeleteConfirm"]}\n\n{Loc["PermanentDeleteWarning"]}",
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            try
            {
                await LookupApi.DeleteCategoryAsync(data.Id);
                _toastService.Show(NotificationSeverity.Success, Loc["Success"], Loc["LookupCategoryPermanentlyDeleted"], 3000, false);

                selectedLookupCategories = [];
                lookupValues = [];
                valueCount = 0;
                currentValueSkip = 0;
                hasAutoSelectedInitialCategory = false;
                await categoryGrid.FirstPage(true);
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "DeleteRecordFailed");
            }
        }

        // Nối lại 2 dialog thêm mới (trước đây mồ côi — Atlas yêu cầu primary action "Thêm ...").
        protected async Task OpenAddCategoryAsync()
        {
            var example = lookupCategories.FirstOrDefault(x => !x.IsDeleted) ?? lookupCategories.FirstOrDefault();
            var options = VppAdminDialogProfiles.Create(VppAdminDialogSize.Compact, Loc["AddClass"].Value, closeAriaLabel: Loc["Close"].Value);
            var result = await DialogService.OpenAsync<Dialog.Dialog_AddLookupCategory>(
                Loc["AddClass"].Value,
                new Dictionary<string, object?>
                {
                    ["IsCreate"] = true,
                    ["ExampleCode"] = example?.Code,
                    ["ExampleName"] = example?.Name,
                    ["ExampleModuleName"] = example?.ModuleName,
                    ["ExampleDescription"] = example?.Description
                },
                options);

            if (result is LookupCategoryResDTO)
            {
                await categoryGrid.Reload();
            }
        }

        protected async Task OpenEditCategoryAsync(LookupCategoryResDTO source)
        {
            var model = CloneCategory(source);
            var result = await DialogService.OpenAsync<Dialog.Dialog_AddLookupCategory>(
                Loc["Edit"].Value,
                new Dictionary<string, object?> { ["IsCreate"] = false, ["Model"] = model },
                VppAdminDialogProfiles.Create(VppAdminDialogSize.Compact, Loc["Edit"].Value, closeAriaLabel: Loc["Close"].Value));

            if (result is LookupCategoryResDTO)
            {
                await categoryGrid.Reload();
            }
        }

        protected async Task OpenAddValueAsync()
        {
            if (selectedLookupCategory is null)
            {
                return;
            }

            // Dialog không tự gán LookupCategoryId — truyền model đã gắn category đang chọn.
            var model = new LookupValueResDTO { Code = "", Value = "", LookupCategoryId = selectedLookupCategory.Id };
            var example = lookupValues.FirstOrDefault(x => !x.IsDeleted) ?? lookupValues.FirstOrDefault();
            var options = VppAdminDialogProfiles.Create(VppAdminDialogSize.Compact, Loc["AddLookupValue"].Value, closeAriaLabel: Loc["Close"].Value);
            var result = await DialogService.OpenAsync<Dialog.Dialog_AddLookupValue>(
                Loc["AddLookupValue"].Value,
                new Dictionary<string, object?>
                {
                    ["IsCreate"] = true,
                    ["Model"] = model,
                    ["ExampleCode"] = example?.Code,
                    ["ExampleValue"] = example?.Value,
                    ["ExampleSort"] = example?.Sort,
                    ["ExampleDescription"] = example?.Description
                },
                options);

            if (result is LookupValueResDTO)
            {
                await valueGrid.Reload();
            }
        }

        protected async Task OpenEditValueAsync(LookupValueResDTO source)
        {
            var model = CloneValue(source);
            var result = await DialogService.OpenAsync<Dialog.Dialog_AddLookupValue>(
                Loc["Edit"].Value,
                new Dictionary<string, object?> { ["IsCreate"] = false, ["Model"] = model },
                VppAdminDialogProfiles.Create(VppAdminDialogSize.Compact, Loc["Edit"].Value, closeAriaLabel: Loc["Close"].Value));

            if (result is LookupValueResDTO)
            {
                await valueGrid.Reload();
            }
        }

        #endregion

        #region Lookup value methods

        protected async Task LoadValues(LoadDataArgs args)
        {
            if (selectedLookupCategory == null)
            {
                lookupValues = new List<LookupValueResDTO>();
                valueCount = 0;
                currentValueSkip = 0;
                return;
            }

            isValueLoading = true;
            var skip = Math.Max(0, args.Skip ?? 0);
            var top = args.Top is > 0 ? args.Top.Value : VppPagingProfiles.SplitList.DefaultPageSize;
            currentValueSkip = skip;
            StateHasChanged();
            try
            {
                var result = await LookupApi.GetValuesAsync(
                    selectedLookupCategory.Id,
                    new LookupQuery(
                        skip,
                        top,
                        valueSearchText,
                        valueStatusFilter,
                        args.OrderBy));
                currentValueSkip = result.AppliedSkip;
                lookupValues = result.Items.ToList();
                valueCount = result.TotalCount;
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
            finally
            {
                isValueLoading = false;
                StateHasChanged();
            }
        }

        protected async Task ToggleValueDeleted(LookupValueResDTO data, bool isDeleted)
        {
            var previous = data.IsDeleted;
            try
            {
                if (isDeleted && !await CanDeactivateAsync(
                        () => LookupApi.GetValueDependencyImpactAsync(data.Id)))
                {
                    data.IsDeleted = previous;
                    StateHasChanged();
                    return;
                }

                var change = new LookupStatusChange(
                    isDeleted,
                    DateTime.Now,
                    CurrentUserState.Current?.UserId ?? 0);
                var result = await LookupApi.SetValueDeletedAsync(data.Id, change);
                if (result != null)
                {
                    data.IsDeleted = result.IsDeleted;
                    string message = isDeleted ? Loc["LookupValueDeactivated"].Value : Loc["LookupValueRestored"].Value;
                    _toastService.Show(NotificationSeverity.Success, Loc["Success"], message, 3000, false);
                    await valueGrid.Reload();
                }
                else
                {
                    data.IsDeleted = previous;
                    _toastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = previous;
                _toastService.Error(ex, Loc, "ChangeRecordStatusFailed");
            }
        }

        protected async Task HardDeleteValueAsync(LookupValueResDTO data)
        {
            if (!data.IsDeleted || !CanModifyLookup) return;

            var confirm = await DialogService.Confirm(
                $"{Loc["LookupValueHardDeleteConfirm"]}\n\n{Loc["PermanentDeleteWarning"]}",
                Loc["HardDelete"].Value,
                new ConfirmOptions { OkButtonText = Loc["Yes"], CancelButtonText = Loc["No"] });

            if (confirm != true) return;

            try
            {
                await LookupApi.DeleteValueAsync(data.Id);
                _toastService.Show(NotificationSeverity.Success, Loc["Success"], Loc["LookupValuePermanentlyDeleted"], 3000, false);
                await valueGrid.Reload();
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "DeleteRecordFailed");
            }
        }

        protected void OnCategoryRowRender(RowRenderEventArgs<LookupCategoryResDTO> args)
        {
            if (args.Data != null && args.Data.IsDeleted)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
        }

        protected void OnValueRowRender(RowRenderEventArgs<LookupValueResDTO> args)
        {
            if (args.Data != null && args.Data.IsDeleted)
            {
                AppendRowClass(args.Attributes, "vpp-admin-row-deleted");
            }
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

        private static LookupCategoryResDTO CloneCategory(LookupCategoryResDTO source)
            => new()
            {
                Id = source.Id,
                Code = source.Code,
                Name = source.Name,
                ModuleName = source.ModuleName,
                Description = source.Description,
                IsDeleted = source.IsDeleted,
                CreatedAtUtc = source.CreatedAtUtc,
                CreatedByUserId = source.CreatedByUserId,
                UpdatedAtUtc = source.UpdatedAtUtc,
                UpdatedByUserId = source.UpdatedByUserId
            };

        private static LookupValueResDTO CloneValue(LookupValueResDTO source)
            => new()
            {
                Id = source.Id,
                LookupCategoryId = source.LookupCategoryId,
                Code = source.Code,
                Value = source.Value,
                Sort = source.Sort,
                ExtraField1 = source.ExtraField1,
                ExtraField2 = source.ExtraField2,
                ExtraField3 = source.ExtraField3,
                Description = source.Description,
                IsDeleted = source.IsDeleted,
                CreatedAtUtc = source.CreatedAtUtc,
                CreatedByUserId = source.CreatedByUserId,
                UpdatedAtUtc = source.UpdatedAtUtc,
                UpdatedByUserId = source.UpdatedByUserId
            };

        private async Task<bool> CanDeactivateAsync(
            Func<Task<LibraryDependencyImpactResDTO?>> loadImpact)
        {
            var impact = await loadImpact();

            if (impact is null || impact.CanDeactivate)
            {
                return true;
            }

            _toastService.Show(
                NotificationSeverity.Warning,
                Loc["ValidationTitle"],
                string.Format(Loc["DependencyDeactivateBlocked"].Value, impact.ActiveReferenceCount),
                5000,
                false);
            return false;
        }
        #endregion
    }
}
