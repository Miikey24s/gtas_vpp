using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using Radzen;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog
{
    public partial class Dialog_AddNewClass
    {
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = default!;
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Parameter] public L01_ClassResDTO selected_l01 { get; set; }
        [Parameter] public bool isCreateNew { get; set; }
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        [Inject] public ICustomNotificationService _customNotificationService { get; set; } = default!;
        public L01_ClassResDTO l01editmodel { get; set; }
        public bool IsClose_Dialog { get; set; } = false;
        public bool IsBusy_Dialog { get; set; } = false;
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            var authState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                if (selected_l01 is not null)
                {
                    l01editmodel = selected_l01;
                }
                else
                {
                    l01editmodel = new L01_ClassResDTO() { ClassModul = "" };
                }
                await LoadBaseData();
            }
            else
            {
                UriHelper.NavigateTo("Home", true);
            }
        }
        protected async Task LoadBaseData()
        {

        }
        public async Task SubmitLocationInlineDialog(L01_ClassResDTO args)
        {
            glb.isBusyPage = true;
            IsBusy_Dialog = true;
            L01_ClassResDTO? l01 = null;
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int UserId);
                args.UpdateDate = DateTime.Now;
                args.ClassCode = "";
                args.UpdateUserId = UserId == 0 ? glb.UserInfo.UserID : UserId;
                if (isCreateNew)
                {
                    args.IsDeleted = false;
                    args.CreateDate = DateTime.Now;
                    args.CreateUserId = UserId == 0 ? glb.UserInfo.UserID : UserId;
                    args.Id = Guid.NewGuid();
                    try
                    {
                        //l01 = await _bussinessService.BaseService<L01_Class>(Config.EF_BASEMETHOD.EF_Create, null, null, null, new List<L01_Class>() { args }).ContinueWith(t => t.Result?.FirstOrDefault());
                        l01 = await _apiServices.PostFromApiAsync<L01_ClassResDTO>(Config.LibraryApi.L01_Class, args);
                    }
                    catch (Exception ex)
                    {
                        //_bussinessService.WriteLog(ex, "EF create L01", new Dictionary<string, object> { { "Param", JsonConvert.SerializeObject(args) } });
                        _customNotificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when call EF create L01");
                    }
                }
                else
                {
                    try
                    {
                        //l01 = await _bussinessService.BaseService<L01_Class>(Config.EF_BASEMETHOD.EF_Update, null, null, null, new List<L01_Class>() { args }).ContinueWith(t => t.Result?.FirstOrDefault());
                        l01 = await _apiServices.PutFromApiAsync<L01_ClassResDTO>($"api/library/l01/{args.Id}", args);
                    }
                    catch (Exception ex)
                    {
                        //_bussinessService.WriteLog(ex, "EF create L01", new Dictionary<string, object> { { "Param", JsonConvert.SerializeObject(args) } });
                        _customNotificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when call EF create L01");
                    }
                }
                if (l01 is not null)
                {
                    //if (hubConnection is not null)
                    //{
                    //    await hubConnection.HubConnection.SendAsync("ClassDetail_SendMessage");
                    //}
                    if (isCreateNew)
                    {
                        l01editmodel = new L01_ClassResDTO() { ClassModul = "" };
                    }
                    NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Success, Summary = "Saved", Duration = 5000 });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage() { Severity = NotificationSeverity.Error, Summary = "Error", Detail = "Error when call EF: " + ex.Message, Duration = 10000 });
                throw;
            }
            finally
            {
                glb.isBusyPage = false;
                IsBusy_Dialog = false;
                l01?.Dispose();
                StateHasChanged();
            }
            if (IsClose_Dialog)
            {
                DialogService.Close();
            }
        }
    }
}
