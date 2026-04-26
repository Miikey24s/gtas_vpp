using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Share;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Radzen;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib
{
    public partial class Component_Library
    {
        [Parameter] public string? Lib { get; set; }
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;
        [Parameter] public IEnumerable<Claim> claims { get; set; } = Enumerable.Empty<Claim>();
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new();
        [Inject] NavigationManager NavigationManager { get; set; } = default!;
        TabPosition tabPosition = TabPosition.Top;
        int SelectedIndex = 0;
        List<string> libStrings = new List<string> { "class", "operationcat", "operation", "supplier", "department" };
        private const string LibraryDepartmentComponentCode = Config.Page_ComponentCode.ComponentCode.LibraryDepartment;
        private bool HasVisibleComponent(string componentCode)
            => sp_Authentication_GetPermissionSinglePage.List_Component.Any(x => x.ComponentCode == componentCode && x.IsVisible);

        private bool HasAnyVisibleLibraryTab => LibraryTabCodes.Any(HasVisibleComponent);

        private string[] LibraryTabCodes =>
        [
            Config.Page_ComponentCode.ComponentCode.LibraryClass,
            Config.Page_ComponentCode.ComponentCode.LibraryOperationCategory,
            Config.Page_ComponentCode.ComponentCode.LibraryOperation,
            Config.Page_ComponentCode.ComponentCode.LibrarySupplier,
            LibraryDepartmentComponentCode
        ];

        private int ResolveVisibleTabIndex(string? tab)
        {
            var index = libStrings.IndexOf(tab?.ToLower() ?? "class");
            if (index >= 0 && HasVisibleComponent(LibraryTabCodes[index]))
            {
                return index;
            }

            for (var i = 0; i < LibraryTabCodes.Length; i++)
            {
                if (HasVisibleComponent(LibraryTabCodes[i]))
                {
                    return i;
                }
            }

            return 0;
        }

        public List<L03_VPPCategoryResDTO> operationCategories = new List<L03_VPPCategoryResDTO>();
        public List<L04_VPPResDTO> operations = new List<L04_VPPResDTO>();
        public List<L05_VPPSupplierResDTO> suppliers = new List<L05_VPPSupplierResDTO>();
        public List<LEX02_CompanyDepartmentLocationResDTO> departments = new List<LEX02_CompanyDepartmentLocationResDTO>();
        Dictionary<string, IList<DropdownModel>> CategoryDropdownDatas { get; set; } = new();
        protected override async Task OnInitializedAsync()
        {
            SelectedIndex = ResolveVisibleTabIndex(Lib);
            NavigationManager.LocationChanged += OnLocationChanged;
            await GetLibraries();
        }
        public void OnLocationChanged(object? sender, LocationChangedEventArgs args)
        {
            var uri = new Uri(args.Location);
            if (uri.AbsolutePath.Contains("library"))
            {
                string currtab = uri.AbsolutePath.Split('/')[uri.AbsolutePath.Split('/').Length - 1];
                SelectedIndex = ResolveVisibleTabIndex(currtab);
                StateHasChanged();
            }
        }
        void TabOnChange(int index)
        {
            NavigationManager.NavigateTo($"/library/{libStrings[index]}");
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
        void OnChange(int index)
        {
            NavigationManager.NavigateTo($"/Library/{libStrings[index]}");
        }
        async Task<T> ApiAddAsync<T>(T data) where T : BaseResDTO, new()
        {
            try
            {
                //T result = await _businessService.BaseService<T>(
                //                                        Config.EF_BASEMETHOD.EF_Create,
                //                                        null,
                //                                        null,
                //                                        null,
                //                                        new List<T> { data}
                //                                    ).ContinueWith(x=>x.Result?.FirstOrDefault()) ?? new T();
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
                //T result = await _businessService.BaseService<T>(
                //                                        Config.EF_BASEMETHOD.EF_Update,
                //                                        null,
                //                                        null,
                //                                        null,
                //                                        new List<T> { data }
                //                                    ).ContinueWith(x => x.Result?.FirstOrDefault()) ?? new T();
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
                //var result = _businessService.BaseService<T>(
                //                                    Config.EF_BASEMETHOD.EF_DeleteAsync,
                //                                    null,
                //                                    null,
                //                                    null,
                //                                    null,
                //                                    data.Id
                //                                );
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
                //_businessService.WriteLog(ex, "Delete EF error", new Dictionary<string, object> { { "Param", data.Id} });
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", $"Error when deleting record: {ex.Message}");
                return false;
            }
        }
    }
}
