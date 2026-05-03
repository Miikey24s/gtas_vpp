using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Component_Library : IDisposable
    {
        private sealed record LibraryTabDefinition(int QueryIndex, string Permission);

        private static readonly LibraryTabDefinition[] LibraryTabs =
        [
            new(0, Permissions.LibraryClass),
            new(1, Permissions.LibraryCategory),
            new(2, Permissions.LibraryItem),
            new(3, Permissions.LibrarySupplier),
            new(4, Permissions.LibraryDepartment)
        ];

        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        private readonly TabPosition tabPosition = TabPosition.Top;
        private int SelectedIndex { get; set; }

        private IReadOnlyList<LibraryTabDefinition> AuthorizedTabs =>
            LibraryTabs.Where(tab => claims.HasPermission(tab.Permission)).ToArray();

        private bool HasAnyVisibleLibraryTab => AuthorizedTabs.Count > 0;

        public List<L03_VPPCategoryResDTO> operationCategories = new List<L03_VPPCategoryResDTO>();
        public List<L04_VPPResDTO> operations = new List<L04_VPPResDTO>();
        public List<L05_VPPSupplierResDTO> suppliers = new List<L05_VPPSupplierResDTO>();
        public List<LEX02_CompanyDepartmentLocationResDTO> departments = new List<LEX02_CompanyDepartmentLocationResDTO>();
        Dictionary<string, IList<DropdownModel>> CategoryDropdownDatas { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            SetSelectedIndexFromUri(NavigationManager.Uri);
            await GetLibraries();
        }

        protected override void OnParametersSet()
        {
            SetSelectedIndexFromUri(NavigationManager.Uri);
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            if (!IsLibraryUri(args.Location))
            {
                return;
            }

            SetSelectedIndexFromUri(args.Location);
            _ = InvokeAsync(StateHasChanged);
        }

        private void TabOnChange(int index)
        {
            var tab = AuthorizedTabs.ElementAtOrDefault(index);
            if (tab is null)
            {
                return;
            }

            SelectedIndex = index;
            NavigationManager.NavigateTo($"/library?tab={tab.QueryIndex}");
        }

        private bool IsActiveTab(int queryIndex)
        {
            return AuthorizedTabs.ElementAtOrDefault(SelectedIndex)?.QueryIndex == queryIndex;
        }

        private void SetSelectedIndexFromUri(string location)
        {
            var authorizedTabs = AuthorizedTabs;
            if (authorizedTabs.Count == 0)
            {
                SelectedIndex = 0;
                return;
            }

            var requestedTab = GetRequestedTabIndex(location);
            var selectedIndex = requestedTab.HasValue
                ? authorizedTabs.ToList().FindIndex(tab => tab.QueryIndex == requestedTab.Value)
                : 0;

            SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private int? GetRequestedTabIndex(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("tab", out var values) &&
                int.TryParse(values.FirstOrDefault(), out var tabIndex))
            {
                return tabIndex;
            }

            return null;
        }

        private bool IsLibraryUri(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            return uri.AbsolutePath.TrimEnd('/').EndsWith("/library", StringComparison.OrdinalIgnoreCase);
        }
        public async Task<List<string>> GetFormular()
        {
            List<string> result = new List<string>();
            try
            {
                List<L02_ClassDetailResDTO> l02_ClassDetail = await _apiServices.GetFromApiAsync<List<L02_ClassDetailResDTO>>(Config.LibraryApi.L02_ClassDetail)
             ?? new List<L02_ClassDetailResDTO>();
                foreach (var item in l02_ClassDetail)
                {
                    result.Add(item.ClassDetailValue!);
                }
            }
            catch
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when call EF get L02 by Id \"40A06BB8-63D5-424F-98E2-2A0E14FFFFDD\"");
            }
            finally
            {
                StateHasChanged();
            }
            return result;
        }
        public async Task GetLibraries()
        {
            glb.isBusyPage = true;
            try
            {
                operations = await _apiServices.GetFromApiAsync<List<L04_VPPResDTO>>(Config.LibraryApi.L04_Item) ?? new List<L04_VPPResDTO>();
                operationCategories = await _apiServices.GetFromApiAsync<List<L03_VPPCategoryResDTO>>(Config.LibraryApi.L03_Category) ?? new List<L03_VPPCategoryResDTO>();
                suppliers = await _apiServices.GetFromApiAsync<List<L05_VPPSupplierResDTO>>(Config.LibraryApi.L05_Supplier) ?? new List<L05_VPPSupplierResDTO>();
                departments = await _apiServices.GetFromApiAsync<List<LEX02_CompanyDepartmentLocationResDTO>>($"{Config.ApiLibraryBase}/lex02") ?? new List<LEX02_CompanyDepartmentLocationResDTO>();
                var FomulaTask = await GetFormular();
                var uomList = await _apiServices.GetFromApiAsync<List<L02_ClassDetailResDTO>>(Config.LibraryApi.L02_ClassDetail) ?? new List<L02_ClassDetailResDTO>();

                CategoryDropdownDatas = new Dictionary<string, IList<DropdownModel>>()
                {
                    {
                    nameof(L04_VPPResDTO.VPPCategoryId),
                    operationCategories.Select(x => new DropdownModel { Code = x.Id.ToString(), Name = x.VPPCategoryName }).ToList()
                    },
                    {
                    nameof(L04_VPPResDTO.UOMId),
                    uomList.Select(x => new DropdownModel { Code = x.Id.ToString(), Name = x.ClassDetailValue }).ToList()
                    },
                    {
                        nameof(L04_VPPResDTO),
                        FomulaTask.Select(x => new DropdownModel { Code = x, Name = x }).ToList()
                    }
                };
            }
            catch
            {
                throw;
            }
            finally
            {
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        async Task<T> ApiAddAsync<T>(T data) where T : BaseResDTO, new()
        {
            try
            {
                string typeName = typeof(T).Name;
                string tableCode = typeName.StartsWith("LEX") ? typeName.Substring(0, 5).ToLower() : typeName.Substring(0, 3).ToLower();
                string endpoint = $"{Config.ApiLibraryBase}/{tableCode}";
                T result = await _apiServices.PostFromApiAsync<T>(endpoint, data) ?? new T();
                if (result.Id != Guid.Empty)
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", "Record added successfully");
                else
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when adding record");
                return result!;
            }
            catch
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when adding record");
                return default!;
            }
        }
        async Task<T> ApiUpdateAsync<T>(T data) where T : BaseResDTO, new()
        {
            try
            {
                string typeName = typeof(T).Name;
                string tableCode = typeName.StartsWith("LEX") ? typeName.Substring(0, 5).ToLower() : typeName.Substring(0, 3).ToLower();
                string endpoint = $"{Config.ApiLibraryBase}/{tableCode}/{data.Id}";
                T result = await _apiServices.PatchFromApiAsync<T>(endpoint, data) ?? new T();
                if (result != null)
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", "Record updated successfully");
                else
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when updating record");
                return result!;
            }
            catch(Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error when updating record: {ex.Message}");
                return default!;
            }
        }
        async Task<bool> ApiDeleteAsync<T>(T data) where T : BaseResDTO, new()
        {
            try
            {
                string typeName = typeof(T).Name;
                string tableCode = typeName.StartsWith("LEX") ? typeName.Substring(0, 5).ToLower() : typeName.Substring(0, 3).ToLower();
                string endpoint = $"{Config.ApiLibraryBase}/{tableCode}/{data.Id}";
                var result = await _apiServices.DeleteFromApiAsync(endpoint);

                if (result is true)
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", "Record deleted successfully");
                else
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when deleting record");
                return result;
            }
            catch(Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error when deleting record: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            NavigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
