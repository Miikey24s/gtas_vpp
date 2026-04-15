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
        [Parameter] public string Lib { get; set; }
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; }
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }
        [Inject] NavigationManager? NavigationManager { get; set; }
        TabPosition tabPosition = TabPosition.Top;
        int SelectedIndex = 0;
        List<string> libStrings = new List<string> { "class", "operationcat", "operation" };

        public List<L03_VPPCategoryResDTO> operationCategories = new List<L03_VPPCategoryResDTO>();
        public List<L04_VPPResDTO> operations = new List<L04_VPPResDTO>();
        Dictionary<string, IList<DropdownModel>> CategoryDropdownDatas { get; set; }
        protected override async Task OnInitializedAsync()
        {
            SelectedIndex = ResolveTabIndex(Lib);
            NavigationManager.LocationChanged += OnLocationChanged;
            await GetLibraries();
        }
        private int ResolveTabIndex(string? tab)
        {
            var index = libStrings.IndexOf(tab?.ToLower() ?? "class");
            return index < 0 ? 0 : index;
        }
        public void OnLocationChanged(object sender, LocationChangedEventArgs args)
        {
            var uri = new Uri(args.Location);
            if (uri.AbsolutePath.Contains("library"))
            {
                string currtab = uri.AbsolutePath.Split('/')[uri.AbsolutePath.Split('/').Length - 1];
                SelectedIndex = ResolveTabIndex(currtab);
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
                    result.Add(item.ClassDetailValue);
                }
            }
            catch (Exception ex)
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
                var FomulaTask = await GetFormular();

                CategoryDropdownDatas ??= new Dictionary<string, IList<DropdownModel>>()
                {
                    {
                    nameof(L04_VPPResDTO.VPPCategory),
                    operationCategories.Select(x => new DropdownModel { Code = x.Id.ToString(), Name = x.VPPCategoryName }).ToList()
                    },
                    {
                        nameof(L04_VPPResDTO),
                        FomulaTask.Select(x => new DropdownModel { Code = x, Name = x }).ToList()
                    }
                };
            }
            catch (Exception ex)
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
                //T result = await _bussinessService.BaseService<T>(
                //                                        Config.EF_BASEMETHOD.EF_Create,
                //                                        null,
                //                                        null,
                //                                        null,
                //                                        new List<T> { data}
                //                                    ).ContinueWith(x=>x.Result?.FirstOrDefault()) ?? new T();
                string tableCode = typeof(T).Name.Substring(0, 3).ToLower();
                string endpoint = $"{Config.ApiLibraryBase}/{tableCode}";
                T result = await _apiServices.PostFromApiAsync<T>(endpoint, data) ?? new T();
                if (result.Id != Guid.Empty)
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", "Record added successfully");
                else
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when adding record");
                return result;
            }
            catch
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when adding record");
                return null;
            }
        }
        async Task<T> ApiUpdateAsync<T>(T data) where T : BaseResDTO, new()
        {
            try
            {
                //T result = await _bussinessService.BaseService<T>(
                //                                        Config.EF_BASEMETHOD.EF_Update,
                //                                        null,
                //                                        null,
                //                                        null,
                //                                        new List<T> { data }
                //                                    ).ContinueWith(x => x.Result?.FirstOrDefault()) ?? new T();
                string tableCode = typeof(T).Name.Substring(0, 3).ToLower();
                string endpoint = $"{Config.ApiLibraryBase}/{tableCode}";
                T result = await _apiServices.PatchFromApiAsync<T>(endpoint, data) ?? new T();
                if (result != null)
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", "Record updated successfully");
                else
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when updating record");
                return result;
            }
            catch
            {
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when updating record");
                return null;
            }
        }
        async Task<bool> ApiDeleteAsync<T>(T data) where T : BaseResDTO, new()
        {
            try
            {
                //var result = _bussinessService.BaseService<T>(
                //                                    Config.EF_BASEMETHOD.EF_DeleteAsync,
                //                                    null,
                //                                    null,
                //                                    null,
                //                                    null,
                //                                    data.Id
                //                                );
                string tableCode = typeof(T).Name.Substring(0, 3).ToLower();
                string endpoint = $"{Config.ApiLibraryBase}/{tableCode}";
                var result = await _apiServices.DeleteFromApiAsync(endpoint);

                if (result is true)
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success");
                else
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error");
                return result;
            }
            catch(Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Delete EF error", new Dictionary<string, object> { { "Param", data.Id} });
                return false;
            }
        }
    }
}
