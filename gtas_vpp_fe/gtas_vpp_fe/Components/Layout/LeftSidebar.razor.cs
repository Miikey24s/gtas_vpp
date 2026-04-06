using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Helpers.DTOs.Res;
using gtas_vpp_fe.Helpers.DTOs.Share;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Radzen;
using System.Security.Claims;
using System.Text.Json;

namespace gtas_vpp_fe.Components.Layout
{
    public partial class LeftSidebar
    {
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        [Inject] public IAPIServices _apiServices { get; set; } = default!;
        [Inject] public ThemeService ThemeService { get; set; } = default!;
        public bool _sideBarExpanded { get; set; } = false;
        private string? currentUrl { get; set; }
        public const string QueryParameter = "theme";
        public string theme = "material3-base";
        public List<DropdownModel> dropdownDataModels_Company { get; set; } = new List<DropdownModel>();
        public DropdownModel selected_Company { get; set; } = default!;
        public sp_Authentication_GetPermissionSinglePage sp_Authentication_GetPermissionSinglePage { get; set; } = new sp_Authentication_GetPermissionSinglePage();
        private IEnumerable<Claim> claims = Enumerable.Empty<Claim>();
        public string State { get; set; } = "normal";

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadAuthenticationState();

            currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
            NavigationManager.LocationChanged += OnLocationChanged;
        }
        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
            StateHasChanged();
        }
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await LoadTheme();
                //await LoadCompany();
                await LoadStateAsync();
                StateHasChanged();
            }
        }
        protected async Task LoadStateAsync()
        {
            try
            {
                var result = await ProtectedLocalStore.GetAsync<GlobalStorageModel>("CostingSetting");
                if (result.Success && result.Value is not null)
                {
                    //State = result.Header?.FirstOrDefault(x => x.PageName == "0014")?.Fields?.FirstOrDefault(x => x.FieldName == "RequestPageViewType")?.FieldValue ?? "normal";
                    State = result.Value?.Header?.FirstOrDefault(x => x.PageName == "0001")?.Fields?.FirstOrDefault(x => x.FieldName == "RequestPageViewType")?.FieldValue ?? "normal";
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
        protected async Task ThemeOnChange()
        {
            LightTheme = !LightTheme;
            if (LightTheme)
            {
                ThemeService.SetTheme("material3");
                await ProtectedLocalStore.SetAsync("TransTheme", "material3");

            }
            else
            {
                ThemeService.SetTheme("material3-dark");
                await ProtectedLocalStore.SetAsync("TransTheme", "material3-dark");
            }
        }
        protected async Task LoadAuthenticationState()
        {
            //_apiServices.SetBaseUrl(NavigationManager.BaseUri);
            var authState = await AuthenticationStateProvider
            .GetAuthenticationStateAsync();
            var user = authState.User;
            if (user.Identity is not null && user.Identity.IsAuthenticated)
            {
                claims = authState.User.Claims;
                _ = int.TryParse(claims.FirstOrDefault(x => x.Type == "UserID")?.Value, out int userid);
                _ = bool.TryParse(claims.FirstOrDefault(x => x.Type == "IsAdmin")?.Value, out bool isadmin);
                _ = Guid.TryParse(claims.FirstOrDefault(x => x.Type == "GroupId")?.Value, out Guid id);

                glb.UserInfo.UserID = userid;
                glb.UserInfo.UserLogin = claims.FirstOrDefault(x => x.Type == "UserLogin")?.Value ?? "";
                glb.UserInfo.FullName = claims.FirstOrDefault(x => x.Type == "FullName")?.Value ?? "";
                glb.UserInfo.Email = claims.FirstOrDefault(x => x.Type == "Email")?.Value ?? "";
                glb.UserInfo.GoogleEmail = claims.FirstOrDefault(x => x.Type == "GoogleEmail")?.Value ?? "";
                glb.UserInfo.IsAdmin = isadmin;
                glb.UserInfo.GroupId = id;
                glb.UserInfo.GroupName = claims.FirstOrDefault(x => x.Type == "GroupName")?.Value ?? "";
                glb.UserInfo.MemberCompanyCode = claims.FirstOrDefault(x => x.Type == "MemberCompanyCode")?.Value ?? "";
                glb.UserInfo.MemberCompanyName = claims.FirstOrDefault(x => x.Type == "MemberCompanyName")?.Value ?? "";
                glb.UserInfo.MemberCompanyShortName = claims.FirstOrDefault(x => x.Type == "MemberCompanyShortName")?.Value ?? "";
                glb.UserInfo.DepartmentName = claims.FirstOrDefault(x => x.Type == "DepartmentName")?.Value ?? "";
                glb.UserInfo.DepartmentCode = claims.FirstOrDefault(x => x.Type == "DepartmentCode")?.Value ?? "";
                //glb.UserInfo.List_PagePermission = !string.IsNullOrEmpty(claims.FirstOrDefault(x => x.Type == "PermissionJson")?.Value) ? JsonConvert.DeserializeObject<List<sp_Authentication_GetPermissionSinglePage>>(claims.FirstOrDefault(x => x.Type == "PermissionJson")?.Value) : new List<sp_Authentication_GetPermissionSinglePage>();
                glb.Server = claims.FirstOrDefault(x => x.Type == "Server")?.Value ?? "";

                //sp_Authentication_GetPermissionSinglePage = (await _bussinessService.SPServiceRead<sp_Authentication_GetPermissionSinglePage>(
                //                                                                            Config.SPENUM_ResType.Single,
                //                                                                            nameof(Config.sp_AuthenClass.sp_Authen.sp_Authen),
                //                                                                            nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage),
                //                                                                            new { userId = glb.UserInfo?.UserID ?? userid, pageCode = "0001" }))?.FirstOrDefault() ?? new sp_Authentication_GetPermissionSinglePage();

                sp_Authentication_GetPermissionSinglePage = new sp_Authentication_GetPermissionSinglePage();
                string sptype = nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_GetPermissionSinglePage);
                var body = new { userId = glb.UserInfo?.UserID ?? userid, pageCode = "0001" };
                var apiResult = await _apiServices.aPIFrom_sp_Authen(sptype, body);
                if (apiResult?.IsSuccess == true && !string.IsNullOrWhiteSpace(apiResult.ResData))
                {
                    try
                    {
                        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var parsedData = JsonSerializer.Deserialize<sp_Authentication_GetPermissionSinglePage>(apiResult.ResData, jsonOptions);

                        if (parsedData != null)
                        {
                            sp_Authentication_GetPermissionSinglePage = parsedData;
                        }
                    }
                    catch (JsonException ex)
                    {
                    }
                    //catch (Exception ex)
                }
                if (sp_Authentication_GetPermissionSinglePage.List_Component.Count == 0)
                {
                    NavigationManager.NavigateTo("Home", true);
                }
            }
            else
            {
                NavigationManager.NavigateTo("logoutprocess", true);
            }
        }
        protected async Task LoadTheme()
        {
            try
            {
                var theme = await ProtectedLocalStore.GetAsync<string>("CostingTheme");

                if (theme.Success)
                {
                    ThemeService.SetTheme(theme.Value);
                    if (theme.Value == "material3")
                    {
                        LightTheme = true;
                    }
                    else
                    {
                        LightTheme = false;
                    }
                }
                else
                {
                    ThemeService.SetTheme("material3");
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
