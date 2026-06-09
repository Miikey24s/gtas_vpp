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
        private const int PriceTabIndex = 4;
        private const int DepartmentTabIndex = 5;
        private const int PricingTabIndex = 6;
        private const int PriceListTabIndex = 6;

        private sealed record LibraryTabDefinition(int QueryIndex, params string[] Permissions);

        private static readonly LibraryTabDefinition[] LibraryTabs =
        [
            new(0, Permissions.LibraryClass),
            new(1, Permissions.LibraryCategory),
            new(2, Permissions.LibraryItem),
            new(3, Permissions.LibrarySupplier),
            new(PricingTabIndex, Permissions.LibraryPriceList, Permissions.LibraryPrice),
            new(DepartmentTabIndex, Permissions.LibraryDepartment)
        ];

        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;
        [Inject] private PermissionState PermissionState { get; set; } = default!;

        private readonly TabPosition tabPosition = TabPosition.Top;
        private int SelectedIndex { get; set; }
        private int PricingSelectedIndex { get; set; }

        private IReadOnlyList<LibraryTabDefinition> AuthorizedTabs =>
            LibraryTabs
                .Where(tab => CanViewLibraryTab(tab.Permissions))
                .OrderBy(GetVisualTabOrder)
                .ToArray();

        private bool HasAnyVisibleLibraryTab => AuthorizedTabs.Count > 0;

        private IReadOnlyList<int> AuthorizedPricingTabs
        {
            get
            {
                var tabs = new List<int>();

                if (CanShowPriceLists)
                {
                    tabs.Add(PriceListTabIndex);
                }

                if (CanShowPrices)
                {
                    tabs.Add(PriceTabIndex);
                }

                return tabs;
            }
        }

        private bool CanShowPriceLists => CanViewLibraryTab(Permissions.LibraryPriceList);

        private bool CanShowPrices => CanViewLibraryTab(Permissions.LibraryPrice);

        private bool CanShowPricingTabs => AuthorizedPricingTabs.Count > 1;

        public List<L03_VPPCategoryResDTO> operationCategories = new List<L03_VPPCategoryResDTO>();
        public List<L04_VPPResDTO> operations = new List<L04_VPPResDTO>();
        public List<L05_VPPSupplierResDTO> suppliers = new List<L05_VPPSupplierResDTO>();
        public List<LEX02_CompanyDepartmentLocationResDTO> departments = new List<LEX02_CompanyDepartmentLocationResDTO>();
        Dictionary<string, IList<DropdownModel>> CategoryDropdownDatas { get; set; } = new();

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += OnLocationChanged;
            PermissionState.Changed += OnPermissionStateChanged;
            await PermissionState.EnsureLoadedAsync();
            SetSelectedIndexFromUri(NavigationManager.Uri);
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

        private void OnPermissionStateChanged()
        {
            SetSelectedIndexFromUri(NavigationManager.Uri);
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
            NavigationManager.NavigateTo(tab.QueryIndex == PricingTabIndex
                ? BuildPricingUrl()
                : $"/library?tab={tab.QueryIndex}");
        }

        private bool IsActiveTab(int queryIndex)
        {
            return AuthorizedTabs.ElementAtOrDefault(SelectedIndex)?.QueryIndex == queryIndex;
        }

        private void PricingTabOnChange(int index)
        {
            var pricingTabs = AuthorizedPricingTabs;
            if (index < 0 || index >= pricingTabs.Count)
            {
                return;
            }

            PricingSelectedIndex = index;
            NavigationManager.NavigateTo(BuildPricingUrl());
        }

        private bool IsActivePricingTab(int queryIndex)
        {
            return AuthorizedPricingTabs.ElementAtOrDefault(PricingSelectedIndex) == queryIndex;
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
            var normalizedRequestedTab = requestedTab == PriceTabIndex ? PricingTabIndex : requestedTab;
            var selectedIndex = normalizedRequestedTab.HasValue
                ? authorizedTabs.ToList().FindIndex(tab => tab.QueryIndex == normalizedRequestedTab.Value)
                : 0;

            SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
            SetPricingSelectedIndexFromUri(location, requestedTab);

            var activeTab = authorizedTabs.ElementAtOrDefault(SelectedIndex);
            if (activeTab != null)
            {
                _ = LoadTabRepositoryAsync(activeTab.QueryIndex);
            }
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

        private void SetPricingSelectedIndexFromUri(string location, int? requestedTab)
        {
            var pricingTabs = AuthorizedPricingTabs;
            if (pricingTabs.Count == 0)
            {
                PricingSelectedIndex = 0;
                return;
            }

            var requestedPricingTab = GetRequestedPricingTabIndex(location, requestedTab);
            var selectedIndex = requestedPricingTab.HasValue
                ? pricingTabs.ToList().FindIndex(tab => tab == requestedPricingTab.Value)
                : 0;

            PricingSelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        }

        private int? GetRequestedPricingTabIndex(string location, int? requestedTab)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (query.TryGetValue("pricingTab", out var values))
            {
                return values.FirstOrDefault()?.ToLowerInvariant() switch
                {
                    "prices" => PriceTabIndex,
                    "price-lists" => PriceListTabIndex,
                    _ => null
                };
            }

            if (requestedTab == PriceTabIndex)
            {
                return PriceTabIndex;
            }

            if (requestedTab == PriceListTabIndex)
            {
                return PriceListTabIndex;
            }

            return null;
        }

        private bool IsLibraryUri(string location)
        {
            var uri = NavigationManager.ToAbsoluteUri(location);
            return uri.AbsolutePath.TrimEnd('/').EndsWith("/library", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanViewLibraryTab(params string[] permissions)
        {
            return permissions.Any(permission =>
                PermissionState.HasVisibleComponent(Config.Page_ComponentCode.PageCode.Library, permission));
        }

        private static int GetVisualTabOrder(LibraryTabDefinition tab)
        {
            return tab.QueryIndex switch
            {
                PricingTabIndex => 4,
                DepartmentTabIndex => 5,
                _ => tab.QueryIndex
            };
        }

        private string BuildPricingUrl()
        {
            var pricingTab = AuthorizedPricingTabs.ElementAtOrDefault(PricingSelectedIndex);
            var pricingTabQuery = pricingTab == PriceTabIndex ? "prices" : "price-lists";

            return $"/library?tab={PricingTabIndex}&pricingTab={pricingTabQuery}";
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
        private async Task LoadTabRepositoryAsync(int queryIndex)
        {
            try
            {
                if (queryIndex == 1) // Categories
                {
                    // Main Library grids load data through Radzen LoadData with skip/top/filter.
                    // Avoid preloading full tables here; it fights server-side paging and stale state.
                }
                else if (queryIndex == 2) // Operations / Items
                {
                    if (CategoryDropdownDatas == null || CategoryDropdownDatas.Count == 0)
                    {
                        glb.isBusyPage = true;
                        StateHasChanged();

                        var categoriesTask = _apiServices.GetFromApiAsync<List<L03_VPPCategoryResDTO>>($"{Config.LibraryApi.L03_Category}?showDeleted=true");
                        var uomListTask = _apiServices.GetFromApiAsync<List<L02_ClassDetailResDTO>>($"{Config.LibraryApi.L02_ClassDetail}?showDeleted=true");

                        await Task.WhenAll(categoriesTask, uomListTask);

                        var cats = await categoriesTask ?? new List<L03_VPPCategoryResDTO>();
                        var uoms = await uomListTask ?? new List<L02_ClassDetailResDTO>();

                        var formulaList = uoms.Select(x => x.ClassDetailValue).Where(v => v != null).Cast<string>().ToList();

                        CategoryDropdownDatas = new Dictionary<string, IList<DropdownModel>>()
                        {
                            {
                                nameof(L04_VPPResDTO.VPPCategoryId),
                                cats.Select(x => new DropdownModel { Code = x.Id.ToString(), Name = x.VPPCategoryName }).ToList()
                            },
                            {
                                nameof(L04_VPPResDTO.UOMId),
                                uoms.Select(x => new DropdownModel { Code = x.Id.ToString(), Name = x.ClassDetailValue }).ToList()
                            },
                            {
                                nameof(L04_VPPResDTO),
                                formulaList.Select(x => new DropdownModel { Code = x, Name = x }).ToList()
                            }
                        };
                    }
                }
                else if (queryIndex == 3) // Suppliers
                {
                    // Loaded server-side by Component_ShareGrid.
                }
                else if (queryIndex == 5) // Departments
                {
                    // Loaded server-side by Component_ShareGrid.
                }
            }
            catch (Exception ex)
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error loading library tab data: {ex.Message}");
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
            PermissionState.Changed -= OnPermissionStateChanged;
        }
    }
}
