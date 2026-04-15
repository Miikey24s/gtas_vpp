
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

namespace gtas_vpp_fe.Components.Pages.Lib.Tabs.Dialog
{
    public partial class Dialog_AddNewClassDetail
    {
        [Parameter] public IEnumerable<Claim>? claims { get; set; }
        [Parameter] public sp_Authentication_GetPermissionSinglePage? sp_Authentication_GetPermissionSinglePage { get; set; }
        [Parameter] public L02_ClassDetailResDTO selected_l02 { get; set; }
        [Parameter] public bool isCreateNew { get; set; }
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        [Inject] public ICustomNotificationService _customNotificationService { get; set; } = default!;
        public L02_ClassDetailResDTO l02editmodel { get; set; }
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

                if (selected_l02 is not null)
                {
                    l02editmodel = selected_l02;
                }
                else
                {
                    l02editmodel = new L02_ClassDetailResDTO() { ClassDetailCode = "", ClassDetailValue = "" };
                }
            }
            else
            {
                UriHelper.NavigateTo("Home", true);
            }
        }
        public async Task SubmitLocationInlineDialog(L02_ClassDetailResDTO args)
        {
            IsBusy_Dialog = true;
            L02_ClassDetailResDTO? l02 = null;
            try
            {
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int UserId);
                args.UpdateDate = DateTime.Now;
                args.UpdateUserId = UserId == 0 ? glb.UserInfo.UserID : UserId;
                if (isCreateNew)
                {
                    args.IsDeleted = false;
                    args.CreateDate = DateTime.Now;
                    args.CreateUserId = UserId == 0 ? glb.UserInfo.UserID : UserId;
                    args.Id = Guid.NewGuid();
                    try
                    {
                        //l02 = await _bussinessService.BaseService<L02_ClassDetail>(Config.EF_BASEMETHOD.EF_Create, null, null, null, new List<L02_ClassDetail>() { args }).ContinueWith(t => t.Result?.FirstOrDefault());
                        l02 = await _apiServices.PostFromApiAsync<L02_ClassDetailResDTO>(Config.LibraryApi.L01_Class, args);
                    }
                    catch (Exception ex)
                    {
                        //_bussinessService.WriteLog(ex, "EF create L02", new Dictionary<string, object> { { "Param", JsonConvert.SerializeObject(args) } });
                        _customNotificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when call EF create L02");
                    }
                }
                else
                {
                    try
                    {
                        //l02 = await _bussinessService.BaseService<L02_ClassDetail>(Config.EF_BASEMETHOD.EF_Create, null, null, null, new List<L02_ClassDetail>() { args }).ContinueWith(t => t.Result?.FirstOrDefault());
                        l02 = await _apiServices.PutFromApiAsync<L02_ClassDetailResDTO>($"api/library/l02/{args.Id}", args);
                    }
                    catch (Exception ex)
                    {
                        //_bussinessService.WriteLog(ex, "EF create L02", new Dictionary<string, object> { { "Param", JsonConvert.SerializeObject(args) } });
                        _customNotificationService.CustomContentNotification(NotificationSeverity.Error, "Error", "Error when call EF create L02");
                    }
                }
                if (l02 is not null)
                {
                    if (isCreateNew)
                    {
                        l02editmodel.ClassDetailCode = "";
                        l02editmodel.ClassDetailValue = "";
                        l02editmodel.Description = "";
                        l02editmodel.ExtraField1 = "";
                        l02editmodel.ExtraField2 = "";
                        l02editmodel.ExtraField3 = "";
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
                IsBusy_Dialog = false;
                l02?.Dispose();
                StateHasChanged();
            }
            if (IsClose_Dialog)
            {
                DialogService.Close();
            }
        }
    }
}
