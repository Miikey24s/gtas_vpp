using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;
using Radzen.Blazor;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_LookupLibrary
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public PagePermissionResDTO PagePermissionResDTO { get; set; } = new();
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public IToastService _toastService { get; set; } = default!;

        // Lookup categories
        public List<LookupCategoryResDTO> lookupCategories { get; set; } = new List<LookupCategoryResDTO>();
        public IList<LookupCategoryResDTO> selectedLookupCategories { get; set; } = new List<LookupCategoryResDTO>();
        public LookupCategoryResDTO? selectedLookupCategory => selectedLookupCategories?.FirstOrDefault();
        public RadzenDataGrid<LookupCategoryResDTO> categoryGrid { get; set; } = default!;
        private bool isCategoryLoading { get; set; } = false;
        private int categoryCount { get; set; } = 0;
        private string? currentCategoryFilter { get; set; }
        private int currentCategorySkip { get; set; }

        // Lookup values
        public List<LookupValueResDTO> lookupValues { get; set; } = new List<LookupValueResDTO>();
        public RadzenDataGrid<LookupValueResDTO> valueGrid { get; set; } = default!;
        private bool isValueLoading { get; set; } = false;
        private int valueCount { get; set; } = 0;
        private string? currentValueFilter { get; set; }
        private int currentValueSkip { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                // Grid will auto-load via LoadData, but we can trigger it manually
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
                // Trigger initial load
                await categoryGrid.Reload();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        #region Lookup category methods

        protected async Task LoadCategories(LoadDataArgs args)
        {
            isCategoryLoading = true;
            currentCategoryFilter = args.Filter;
            currentCategorySkip = args.Skip ?? 0;
            StateHasChanged();
            try
            {
                // Build query parameters for server-side filtering
                var queryParams = new List<string> { "showDeleted=true" };
                
                if (!string.IsNullOrEmpty(args.Filter))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(args.Filter)}");
                }
                
                queryParams.Add($"skip={args.Skip ?? 0}");
                queryParams.Add($"top={args.Top ?? 20}");
                
                if (!string.IsNullOrEmpty(args.OrderBy))
                {
                    queryParams.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
                }

                string apiUrl = Config.LibraryApi.LookupCategories;
                if (queryParams.Any())
                {
                    apiUrl += "?" + string.Join("&", queryParams);
                }

                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<LookupCategoryResDTO>>(apiUrl);
                lookupCategories = result.Data ?? [];
                categoryCount = result.TotalCount;
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

        protected async Task LoadCategoryFilterData(DataGridLoadColumnFilterDataEventArgs<LookupCategoryResDTO> args)
        {
            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                
                // Request distinct values from server
                var queryParams = new List<string>
                {
                    "showDeleted=true",
                    $"distinct={Uri.EscapeDataString(property)}"
                };

                if (!string.IsNullOrWhiteSpace(currentCategoryFilter))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(currentCategoryFilter)}");
                }

                if (!string.IsNullOrWhiteSpace(args.Filter))
                {
                    queryParams.Add($"distinctFilter={Uri.EscapeDataString(args.Filter)}");
                }

                if (args.Skip.HasValue)
                {
                    queryParams.Add($"skip={args.Skip.Value}");
                }

                if (args.Top.HasValue)
                {
                    queryParams.Add($"top={args.Top.Value}");
                }

                string apiUrl = $"{Config.LibraryApi.LookupCategories}?{string.Join("&", queryParams)}";
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<LookupCategoryResDTO>>(apiUrl);

                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
        }

        protected async Task OnCategorySelected(LookupCategoryResDTO data)
        {
            selectedLookupCategories = new List<LookupCategoryResDTO> { data };
            await valueGrid.Reload();
        }

        protected async Task ToggleCategoryDeleted(LookupCategoryResDTO data, bool isDeleted)
        {
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int userId);
                
                var patchData = new
                {
                    IsDeleted = isDeleted,
                    UpdatedAtUtc = DateTime.Now,
                    UpdatedByUserId = userId == 0 ? glb.UserInfo.UserID : userId
                };

                var result = await _apiServices.PatchFromApiAsync<LookupCategoryResDTO>($"{Config.LibraryApi.LookupCategories}/{data.Id}", patchData);
                if (result != null)
                {
                    string message = isDeleted ? Loc["LookupCategoryDeactivated"].Value : Loc["LookupCategoryRestored"].Value;
                    _toastService.Show(NotificationSeverity.Success, Loc["Success"], message, 3000, false);
                    await categoryGrid.Reload();
                }
                else
                {
                    data.IsDeleted = !isDeleted;
                    _toastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = !isDeleted;
                _toastService.Error(ex, Loc, "ChangeRecordStatusFailed");
            }
        }

        // Nối lại 2 dialog thêm mới (trước đây mồ côi — Atlas yêu cầu primary action "Thêm ...").
        protected async Task OpenAddCategoryAsync()
        {
            var result = await DialogService.OpenAsync<Dialog.Dialog_AddLookupCategory>(
                Loc["AddClass"].Value,
                new Dictionary<string, object?> { ["IsCreate"] = true },
                new DialogOptions { Width = "min(560px, 96vw)", Resizable = false, Draggable = true });

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
            var result = await DialogService.OpenAsync<Dialog.Dialog_AddLookupValue>(
                Loc["AddLookupValue"].Value,
                new Dictionary<string, object?> { ["IsCreate"] = true, ["Model"] = model },
                new DialogOptions { Width = "min(560px, 96vw)", Resizable = false, Draggable = true });

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
                currentValueFilter = null;
                currentValueSkip = 0;
                return;
            }

            isValueLoading = true;
            currentValueFilter = args.Filter;
            currentValueSkip = args.Skip ?? 0;
            StateHasChanged();
            try
            {
                // Build query parameters for server-side filtering
                var queryParams = new List<string>
                {
                    "showDeleted=true",
                    $"lookupCategoryId={selectedLookupCategory.Id}"
                };
                
                if (!string.IsNullOrEmpty(args.Filter))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(args.Filter)}");
                }
                
                queryParams.Add($"skip={args.Skip ?? 0}");
                queryParams.Add($"top={args.Top ?? 20}");
                
                if (!string.IsNullOrEmpty(args.OrderBy))
                {
                    queryParams.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
                }

                string apiUrl = $"{Config.LibraryApi.LookupValues}?{string.Join("&", queryParams)}";
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<LookupValueResDTO>>(apiUrl);
                lookupValues = result.Data ?? [];
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

        protected async Task LoadValueFilterData(DataGridLoadColumnFilterDataEventArgs<LookupValueResDTO> args)
        {
            if (selectedLookupCategory == null) return;

            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                
                // Request distinct values from server
                var queryParams = new List<string>
                {
                    "showDeleted=true",
                    $"lookupCategoryId={selectedLookupCategory.Id}",
                    $"distinct={Uri.EscapeDataString(property)}"
                };

                if (!string.IsNullOrWhiteSpace(currentValueFilter))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(currentValueFilter)}");
                }

                if (!string.IsNullOrWhiteSpace(args.Filter))
                {
                    queryParams.Add($"distinctFilter={Uri.EscapeDataString(args.Filter)}");
                }

                if (args.Skip.HasValue)
                {
                    queryParams.Add($"skip={args.Skip.Value}");
                }

                if (args.Top.HasValue)
                {
                    queryParams.Add($"top={args.Top.Value}");
                }

                string apiUrl = $"{Config.LibraryApi.LookupValues}?{string.Join("&", queryParams)}";
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<LookupValueResDTO>>(apiUrl);

                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                _toastService.Error(ex, Loc, "LoadLibraryDataFailed");
            }
        }

        protected async Task ToggleValueDeleted(LookupValueResDTO data, bool isDeleted)
        {
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int userId);
                
                var patchData = new
                {
                    IsDeleted = isDeleted,
                    UpdatedAtUtc = DateTime.Now,
                    UpdatedByUserId = userId == 0 ? glb.UserInfo.UserID : userId
                };

                var result = await _apiServices.PatchFromApiAsync<LookupValueResDTO>($"{Config.LibraryApi.LookupValues}/{data.Id}", patchData);
                if (result != null)
                {
                    string message = isDeleted ? Loc["LookupValueDeactivated"].Value : Loc["LookupValueRestored"].Value;
                    _toastService.Show(NotificationSeverity.Success, Loc["Success"], message, 3000, false);
                    await valueGrid.Reload();
                }
                else
                {
                    data.IsDeleted = !isDeleted;
                    _toastService.Show(NotificationSeverity.Error, Loc["Error"], Loc["ChangeRecordStatusFailed"], 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = !isDeleted;
                _toastService.Error(ex, Loc, "ChangeRecordStatusFailed");
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
        #endregion
    }
}
