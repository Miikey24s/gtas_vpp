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
    public partial class Tab_ClassLibrary
    {
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;

        // L01 - Class
        public List<L01_ClassResDTO> classListL01 { get; set; } = new List<L01_ClassResDTO>();
        public IList<L01_ClassResDTO> selectedClassesL01 { get; set; } = new List<L01_ClassResDTO>();
        public L01_ClassResDTO? selectedClassL01 => selectedClassesL01?.FirstOrDefault();
        public RadzenDataGrid<L01_ClassResDTO> gridL01 { get; set; } = default!;
        private bool isLoadingL01 { get; set; } = false;
        private int countL01 { get; set; } = 0;
        private string? currentFilterExpressionL01 { get; set; }
        private int currentSkipL01 { get; set; }

        // L02 - Class Detail
        public List<L02_ClassDetailResDTO> classDetailListL02 { get; set; } = new List<L02_ClassDetailResDTO>();
        public RadzenDataGrid<L02_ClassDetailResDTO> gridL02 { get; set; } = default!;
        private bool isLoadingL02 { get; set; } = false;
        private int countL02 { get; set; } = 0;
        private string? currentFilterExpressionL02 { get; set; }
        private int currentSkipL02 { get; set; }

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
                await gridL01.Reload();
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        #region L01 - Class Methods

        protected async Task LoadDataL01(LoadDataArgs args)
        {
            isLoadingL01 = true;
            currentFilterExpressionL01 = args.Filter;
            currentSkipL01 = args.Skip ?? 0;
            StateHasChanged();
            try
            {
                // Build query parameters for server-side filtering
                var queryParams = new List<string>();
                
                if (!string.IsNullOrEmpty(args.Filter))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(args.Filter)}");
                }
                
                if (args.Skip.HasValue)
                {
                    queryParams.Add($"skip={args.Skip.Value}");
                }
                
                if (args.Top.HasValue)
                {
                    queryParams.Add($"top={args.Top.Value}");
                }
                
                if (!string.IsNullOrEmpty(args.OrderBy))
                {
                    queryParams.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
                }

                string apiUrl = Config.LibraryApi.L01_Class;
                if (queryParams.Any())
                {
                    apiUrl += "?" + string.Join("&", queryParams);
                }

                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<L01_ClassResDTO>>(apiUrl);
                classListL01 = result.Data ?? [];
                countL01 = result.TotalCount;
            }
            catch (Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error loading classes: {ex.Message}", 15000, true);
            }
            finally
            {
                isLoadingL01 = false;
                StateHasChanged();
            }
        }

        protected async Task LoadColumnFilterDataL01(DataGridLoadColumnFilterDataEventArgs<L01_ClassResDTO> args)
        {
            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                
                // Request distinct values from server
                var queryParams = new List<string>
                {
                    $"distinct={Uri.EscapeDataString(property)}"
                };

                if (!string.IsNullOrWhiteSpace(currentFilterExpressionL01))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(currentFilterExpressionL01)}");
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

                string apiUrl = $"{Config.LibraryApi.L01_Class}?{string.Join("&", queryParams)}";
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<L01_ClassResDTO>>(apiUrl);

                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error loading filter data: {ex.Message}", 15000, true);
            }
        }

        protected async Task OnRowSelectL01(L01_ClassResDTO data)
        {
            selectedClassesL01 = new List<L01_ClassResDTO> { data };
            await gridL02.Reload();
        }

        protected async Task ToggleDeleteL01(L01_ClassResDTO data, bool isDeleted)
        {
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int userId);
                
                var patchData = new
                {
                    IsDeleted = isDeleted,
                    UpdateDate = DateTime.Now,
                    UpdateUserId = userId == 0 ? glb.UserInfo.UserID : userId
                };

                var result = await _apiServices.PatchFromApiAsync<L01_ClassResDTO>($"{Config.LibraryApi.L01_Class}/{data.Id}", patchData);
                if (result != null)
                {
                    string message = isDeleted ? "Class disabled successfully" : "Class enabled successfully";
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", message, 3000, false);
                    await gridL01.Reload();
                }
                else
                {
                    data.IsDeleted = !isDeleted;
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Failed to update class status", 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = !isDeleted;
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error updating class status: {ex.Message}", 15000, true);
            }
        }

        #endregion

        #region L02 - Class Detail Methods

        protected async Task LoadDataL02(LoadDataArgs args)
        {
            if (selectedClassL01 == null)
            {
                classDetailListL02 = new List<L02_ClassDetailResDTO>();
                countL02 = 0;
                currentFilterExpressionL02 = null;
                currentSkipL02 = 0;
                return;
            }

            isLoadingL02 = true;
            currentFilterExpressionL02 = args.Filter;
            currentSkipL02 = args.Skip ?? 0;
            StateHasChanged();
            try
            {
                // Build query parameters for server-side filtering
                var queryParams = new List<string>
                {
                    $"classId={selectedClassL01.Id}"
                };
                
                if (!string.IsNullOrEmpty(args.Filter))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(args.Filter)}");
                }
                
                if (args.Skip.HasValue)
                {
                    queryParams.Add($"skip={args.Skip.Value}");
                }
                
                if (args.Top.HasValue)
                {
                    queryParams.Add($"top={args.Top.Value}");
                }
                
                if (!string.IsNullOrEmpty(args.OrderBy))
                {
                    queryParams.Add($"orderby={Uri.EscapeDataString(args.OrderBy)}");
                }

                string apiUrl = $"{Config.LibraryApi.L02_ClassDetail}?{string.Join("&", queryParams)}";
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<L02_ClassDetailResDTO>>(apiUrl);
                classDetailListL02 = result.Data ?? [];
                countL02 = result.TotalCount;
            }
            catch (Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error loading class details: {ex.Message}", 15000, true);
            }
            finally
            {
                isLoadingL02 = false;
                StateHasChanged();
            }
        }

        protected async Task LoadColumnFilterDataL02(DataGridLoadColumnFilterDataEventArgs<L02_ClassDetailResDTO> args)
        {
            if (selectedClassL01 == null) return;

            try
            {
                if (args.Column == null) return;

                var property = args.Column.GetFilterProperty();
                
                // Request distinct values from server
                var queryParams = new List<string>
                {
                    $"classId={selectedClassL01.Id}",
                    $"distinct={Uri.EscapeDataString(property)}"
                };

                if (!string.IsNullOrWhiteSpace(currentFilterExpressionL02))
                {
                    queryParams.Add($"filter={Uri.EscapeDataString(currentFilterExpressionL02)}");
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

                string apiUrl = $"{Config.LibraryApi.L02_ClassDetail}?{string.Join("&", queryParams)}";
                var result = await _apiServices.GetFromApiWithTotalCountAsync<List<L02_ClassDetailResDTO>>(apiUrl);

                args.Data = result.Data ?? [];
                args.Count = result.TotalCount;
            }
            catch (Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error loading filter data: {ex.Message}", 15000, true);
            }
        }

        protected async Task ToggleDeleteL02(L02_ClassDetailResDTO data, bool isDeleted)
        {
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int userId);
                
                var patchData = new
                {
                    IsDeleted = isDeleted,
                    UpdateDate = DateTime.Now,
                    UpdateUserId = userId == 0 ? glb.UserInfo.UserID : userId
                };

                var result = await _apiServices.PatchFromApiAsync<L02_ClassDetailResDTO>($"{Config.LibraryApi.L02_ClassDetail}/{data.Id}", patchData);
                if (result != null)
                {
                    string message = isDeleted ? "Class detail disabled successfully" : "Class detail enabled successfully";
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", message, 3000, false);
                    await gridL02.Reload();
                }
                else
                {
                    data.IsDeleted = !isDeleted;
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Failed to update class detail status", 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = !isDeleted;
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error updating class detail status: {ex.Message}", 15000, true);
            }
        }

        #endregion
    }
}
