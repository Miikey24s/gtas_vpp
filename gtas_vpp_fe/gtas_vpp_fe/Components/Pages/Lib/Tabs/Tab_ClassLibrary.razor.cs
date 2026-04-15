using gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;
using System.Collections.ObjectModel;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs
{
    public partial class Tab_ClassLibrary
    {
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = default!;
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ICustomNotificationService _notificationService { get; set; } = default!;

        public List<L01_ClassResDTO> classList { get; set; } = new List<L01_ClassResDTO>();
        public IList<L01_ClassResDTO> selectedClasses { get; set; } = default!;
        public RadzenDataGrid<L01_ClassResDTO> classGrid { get; set; } = default!;

        public ObservableCollection<L02_ClassDetailResDTO> classDetailList { get; set; } = new ObservableCollection<L02_ClassDetailResDTO>();
        public IList<L02_ClassDetailResDTO> selectedClassDetails { get; set; } = default!;
        public RadzenDataGrid<L02_ClassDetailResDTO> classDetailGrid { get; set; } = default!;

        private bool isCreateNewL01 { get; set; } = false;
        private bool isBusyGrid01 { get; set; } = false;

        private bool isCreateNewL02 { get; set; } = false;
        private bool isBusyGrid02 { get; set; } = false;

        public string? SearchTextL01 { get; set; } = string.Empty;
        public string? SearchTextL02 { get; set; } = string.Empty;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var authState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                await LoadBaseDataL01();
            }
            else
            {
                UriHelper.NavigateTo("Home", true);
            }
        }
        public async Task LoadBaseDataL01()
        {
            glb.isBusyPage = true;
            isBusyGrid01 = true;
            StateHasChanged();
            try
            {
                //classList = await _bussinessService.BaseService<L01_ClassResDTO>(Config.EF_BASEMETHOD.EF_GetTAsync, true) ?? [];
                classList = await _apiServices.GetFromApiAsync<List<L01_ClassResDTO>>(Config.LibraryApi.L01_Class) ?? [];
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Load EF L01_ClassResDTO");
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when load L01_ClassResDTO data: " + ex.Message, 15000, true);
            }
            finally
            {
                isBusyGrid01 = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        public async Task LoadBaseDataL02(L01_ClassResDTO data)
        {
            isBusyGrid02 = true;
            glb.isBusyPage = true;
            StateHasChanged();
            try
            {
                //var rs = await _bussinessService.BaseService<L02_ClassDetailResDTO>(
                //                                         Config.EF_BASEMETHOD.EF_GetTAsync,
                //                                         true,
                //                                         x => x.ClassId == data.Id
                //                                    );
                var rs = await _apiServices.GetFromApiAsync<L02_ClassDetailResDTO>($"{Config.LibraryApi.L02_ClassDetail}/{data.Id}");

                classDetailList = new ObservableCollection<L02_ClassDetailResDTO>();
                if (rs != null)
                {
                    classDetailList.Add(rs);
                }
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Load EF L02_ClassDetailResDTO", new Dictionary<string, object>() { { "ClassId", data.Id.ToString() } });
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when load L02_ClassDetailResDTO data: " + ex.Message, 15000, true);
            }
            finally
            {
                isBusyGrid02 = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task GridL01Selected()
        {
            isCreateNewL01 = false;
            await classGrid.Reload();
        }
        protected async Task SelectedL01OnChange(IList<L01_ClassResDTO> args)
        {
            selectedClasses = args;
            //await LoadL02ByL01();
            await classGrid.Reload();
            StateHasChanged();
        }
        protected async Task ButtonOnClick_AddNewOrUpdateDialog_L01(bool isCreatenew, DataGridRowMouseEventArgs<L01_ClassResDTO> arg)
        {
            isBusyGrid01 = true;
            glb.isBusyPage = true;
            Dictionary<string, object> param = default!;
            try
            {
                L01_ClassResDTO l01 = arg?.Data ?? new L01_ClassResDTO();
                param = new Dictionary<string, object>()
                {
                    { "claims", claims },
                    { "sp_Authentication_GetPermissionSinglePage", sp_Authentication_GetPermissionSinglePage },
                    { "selected_l01", isCreatenew ? new L01_ClassResDTO() : l01 },
                    { "isCreateNew", isCreatenew}
                };
                await DialogService.OpenAsync<Dialog_AddNewClass>(isCreatenew ? "Add New Class" : "Update Class", param, new DialogOptions() { Width = "40%", Resizable = true, Draggable = true, ShowClose = false });
                await LoadBaseDataL01();
                StateHasChanged();
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Open dialog add new class", param);
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when open dialog add new class: " + ex.Message, 15000, true);
                throw;
            }
            finally
            {
                isBusyGrid01 = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task ButtonOnClick_AddNewOrUpdateDialog_L02(bool isCreatenew, DataGridRowMouseEventArgs<L02_ClassDetailResDTO> arg)
        {
            isBusyGrid01 = true;
            isBusyGrid02 = true;
            glb.isBusyPage = true;
            Dictionary<string, object> param = default!;
            try
            {
                L02_ClassDetailResDTO l02 = arg?.Data ?? new L02_ClassDetailResDTO() { ClassDetailCode = string.Empty, ClassDetailValue = string.Empty };
                param = new Dictionary<string, object>()
                {
                    { "claims", claims },
                    { "sp_Authentication_GetPermissionSinglePage", sp_Authentication_GetPermissionSinglePage },
                    { "selected_l02", isCreatenew ? new L02_ClassDetailResDTO(){ ClassDetailCode = "", ClassDetailValue = "", ClassId = selectedClasses?.FirstOrDefault()?.Id ?? Guid.Empty } : l02 },
                    { "isCreateNew", isCreatenew}
                };
                await DialogService.OpenAsync<Dialog_AddNewClassDetail>(isCreatenew ? "Add New Class Detail" : "Update Class Detail", param, new DialogOptions() { Width = "40%", Resizable = true, Draggable = true, ShowClose = false, CloseDialogOnOverlayClick = false, CloseDialogOnEsc = false });
                await LoadBaseDataL02(selectedClasses?.FirstOrDefault() ?? new L01_ClassResDTO());
                StateHasChanged();
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Open dialog add new class detail", param);
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when open dialog add new class detail: " + ex.Message, 15000, true);
                throw;
            }
            finally
            {
                isBusyGrid01 = false;
                isBusyGrid02 = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task ButtonOnClick_LinkDetail(L01_ClassResDTO data)
        {
            // load grid class detail dựa vào class
            if (selectedClasses is not null)
            {
                selectedClasses.Clear();
                selectedClasses.Add(data);
            }
            else
            {
                selectedClasses = new List<L01_ClassResDTO>() { data };
            }
            await LoadBaseDataL02(data);
        }
        protected async Task ButtonOnClick_FilterOffL02()
        {
            SearchTextL02 = string.Empty;
            await LoadBaseDataL02(selectedClasses.FirstOrDefault() ?? new L01_ClassResDTO());
        }
        protected async Task ButtonOnClick_FilterOffL01()
        {
            SearchTextL02 = string.Empty;
            await LoadBaseDataL01();
        }
        protected async Task ButtonOnClick_SearchL01()
        {
            isBusyGrid01 = true;
            glb.isBusyPage = true;
            try
            {
                _ = Guid.TryParse(SearchTextL01, out Guid l01id);
                //classList = await _bussinessService.BaseService<L01_ClassResDTO>(
                //                                            Config.EF_BASEMETHOD.EF_GetTAsync,
                //                                            true,
                //                                            x => x.Id == l01id
                //                                                || x.ClassName.Contains(SearchTextL01 ?? "")
                //                                                || x.ClassCode!.Contains(SearchTextL01 ?? "")
                //                                                || x.Description.Contains(SearchTextL01 ?? "")
                //                                        ) ?? new List<L01_ClassResDTO>();

                string safeSearchText = Uri.EscapeDataString(SearchTextL01 ?? "");
                string apiUrl = $"{Config.LibraryApi.L01_Class}?id={l01id}&searchText={safeSearchText}";
                classList = await _apiServices.GetFromApiAsync<List<L01_ClassResDTO>>(apiUrl) ?? [];
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Search L01_ClassResDTO", new Dictionary<string, object>() { { "SearchTextL01", SearchTextL01 ?? "" } });
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when search L01_ClassResDTO data: " + ex.Message, 15000, true);
            }
            finally
            {
                isBusyGrid01 = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task ButtonOnClick_SearchL02()
        {
            isBusyGrid02 = true;
            glb.isBusyPage = true;
            try
            {
                _ = Guid.TryParse(SearchTextL02, out Guid l02id);
                //List<L02_ClassDetailResDTO> res =
                //    await _bussinessService.BaseService<L02_ClassDetailResDTO>(
                //                                    Config.EF_BASEMETHOD.EF_GetTAsync,
                //                                    true,
                //                                    x => x.Id == l02id
                //                                        || x.ClassDetailCode.Contains(SearchTextL02 ?? "")
                //                                        || x.ClassDetailValue!.Contains(SearchTextL02 ?? "")
                //                                        || x.Description.Contains(SearchTextL02 ?? "")
                //                                ) ?? new List<L02_ClassDetailResDTO>();
                string safeSearchText = Uri.EscapeDataString(SearchTextL02 ?? "");
                string apiUrl = $"{Config.LibraryApi.L02_ClassDetail}?id={l02id}&searchText={safeSearchText}";
                var rs = await _apiServices.GetFromApiAsync<List<L02_ClassDetailResDTO>>(apiUrl);
                classDetailList = new ObservableCollection<L02_ClassDetailResDTO>();
                if (rs != null)
                {
                    foreach (var item in rs)
                    {
                        classDetailList.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                //_bussinessService.WriteLog(ex, "Search L02_ClassDetailResDTO", new Dictionary<string, object>() { { "SearchTextL02", SearchTextL02 ?? "" } });
                _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when search L02_ClassDetailResDTO data: " + ex.Message, 15000, true);
            }
            finally
            {
                isBusyGrid02 = false;
                glb.isBusyPage = false;
                StateHasChanged();
            }
        }
        protected async Task SwitchOnChange_IsDeleteL01(L01_ClassResDTO data)
        {
            isBusyGrid01 = true;
            glb.isBusyPage = true;
            _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int UserId);
            L01_ClassResDTO? l01 = null;
            try
            {
                data.UpdateDate = DateTime.Now;
                data.UpdateUserId = UserId == 0 ? glb.UserInfo.UserID : UserId;
                //l01 = await _bussinessService.BaseService<L01_ClassResDTO>(Config.EF_BASEMETHOD.EF_Update, null, null, null, new List<L01_ClassResDTO>() { data }).ContinueWith(x => x.Result?.FirstOrDefault());
                l01 = await _apiServices.PutFromApiAsync<L01_ClassResDTO>($"api/library/l01/{data.Id}", data);

                if (l01 is not null)
                {
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", data.IsDeleted ? "Delete success" : "Un-Delete success", 5000, false);
                }
                else
                {
                    data.IsDeleted = !data.IsDeleted;
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", data.IsDeleted ? "Delete failed" : "Un-Delete failed", 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = !data.IsDeleted;
                //_bussinessService.WriteLog(ex, "Update IsDeleted L01_ClassResDTO", new Dictionary<string, object>() { { "L01_ClassResDTOId", data.Id.ToString() } });
                throw;
            }
            finally
            {
                isBusyGrid01 = false;
                glb.isBusyPage = false;
                l01?.Dispose();
                StateHasChanged();
            }
        }
        protected async Task SwitchOnChange_IsDeleteL02(L02_ClassDetailResDTO data)
        {
            _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int UserId);
            L02_ClassDetailResDTO? l02 = null;
            try
            {
                data.UpdateDate = DateTime.Now;
                data.UpdateUserId = UserId == 0 ? glb.UserInfo.UserID : UserId;
                //l02 = await _bussinessService.BaseService<L02_ClassDetailResDTO>(Config.EF_BASEMETHOD.EF_Update, null, null, null, new List<L02_ClassDetailResDTO>() { data }).ContinueWith(x => x.Result?.FirstOrDefault());
                l02 = await _apiServices.PutFromApiAsync<L02_ClassDetailResDTO>($"api/library/l02/{data.Id}", data);
                if (l02 is not null)
                {
                    _notificationService.CustomContentNotification(NotificationSeverity.Success, "Success", data.IsDeleted ? "Delete success" : "Un-Delete success", 5000, false);
                }
                else
                {
                    data.IsDeleted = !data.IsDeleted;
                    _notificationService.CustomContentNotification(NotificationSeverity.Error, "Error", data.IsDeleted ? "Delete failed" : "Un-Delete failed", 5000, true);
                }
            }
            catch (Exception ex)
            {
                data.IsDeleted = !data.IsDeleted;
                throw;
            }
            finally
            {
                l02?.Dispose();
                StateHasChanged();
            }
        }
        #region reoder row
        L02_ClassDetailResDTO draggedItem;
        protected void RowRenderL02(RowRenderEventArgs<L02_ClassDetailResDTO> args)
        {
            args.Attributes.Add("title", "Drag row to reorder");
            args.Attributes.Add("style", "cursor:grab");
            args.Attributes.Add("draggable", "true");
            args.Attributes.Add("ondragover", "event.preventDefault();event.target.closest('.rz-data-row').classList.add('my-class')");
            args.Attributes.Add("ondragleave", "event.target.closest('.rz-data-row').classList.remove('my-class')");
            args.Attributes.Add("ondragstart", EventCallback.Factory.Create<DragEventArgs>(this, () => draggedItem = args.Data));
            args.Attributes.Add("ondrop", EventCallback.Factory.Create<DragEventArgs>(this, () =>
            {
                var draggedIndex = classDetailList.IndexOf(draggedItem);
                var droppedIndex = classDetailList.IndexOf(args.Data);
                classDetailList.Remove(draggedItem);
                classDetailList.Insert(draggedIndex <= droppedIndex ? droppedIndex++ : droppedIndex, draggedItem);

                js.InvokeVoidAsync("eval", $"document.querySelector('.my-class').classList.remove('my-class')");
            }));
            foreach (var item in classDetailList)
            {
                item.Sort = classDetailList.IndexOf(item) + 1;
            }
        }
        #endregion
    }
}