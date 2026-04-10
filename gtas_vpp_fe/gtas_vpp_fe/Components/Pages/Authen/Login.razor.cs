using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Helpers.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Radzen;
using Serilog;
using System.Net.Http.Json;
using System.Security.Claims;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class Login
    {
        [Parameter] public string? username { get; set; }
        [Parameter] public string? password { get; set; }
        [Parameter] public string? isremember { get; set; }
        [Parameter] public string? server { get; set; }
        //[Inject] public GlobalClass glb { get; set; } = default!;
        [Inject] public IHttpContextAccessor? HttpContextAccessor { get; set; }
        //[Inject] public IBussinessService _bussinessService { get; set; } = default!;
        [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [CascadingParameter] public HttpContext HttpContext { get; set; } = default!;
        bool isLoginSuccess { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            var rs = await LoginAccess();
            if (rs)
            {
                //if (!string.IsNullOrEmpty(transcom.CurrentLocation))
                //{
                //    var uri = transcom.CurrentLocation;
                //    transcom.CurrentLocation = string.Empty;
                //    UriHelper.NavigateTo(UriHelper.ToBaseRelativePath(uri), true);
                //}
                UriHelper.NavigateTo("/", true);
            }
            else
            {
                UriHelper.NavigateTo(Config.LoginPagePath, true);
            }
        }
        protected async Task<bool> LoginAccess()
        {
            bool isLogin = false;
            string errorMessage = string.Empty;
            if (!string.IsNullOrEmpty(username)
                && !string.IsNullOrEmpty(password)
                && !string.IsNullOrEmpty(isremember)
                && !string.IsNullOrEmpty(server))
            {
                //sp_ResDTO resDTO = new sp_ResDTO();
                try
                {
                    //sp_Authentication_Login sp_Authentication_Login
                    //    = await _bussinessService
                    //                .SPServiceRead<sp_Authentication_Login>(Config.SPENUM_ResType.Single
                    //                                                                            , nameof(Config.sp_AuthenClass.sp_Authen.sp_Authen)
                    //                                                                            , nameof(Config.sp_AuthenClass.sp_Authen_Type.sp_Authen_Login)
                    //                                                                            , new
                    //                                                                            {
                    //                                                                                UserLogin = Uri.UnescapeDataString(username),
                    //                                                                                PasswordChar = PasswordHelpers.Encrypt(Uri.UnescapeDataString(password), true),
                    //                                                                                Server = server
                    //                                                                            }
                    //                                                                          )
                    //                .ContinueWith(t => t.Result.FirstOrDefault() ?? new sp_Authentication_Login());
                    var client = HttpClientFactory.CreateClient(Config.HttpClientName);
                    var response = await client.PostAsJsonAsync(Config.ApiLoginEndpoint, new
                    {
                        Username = Uri.UnescapeDataString(username),
                        Password = Uri.UnescapeDataString(password),
                        Server = server
                    });

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorMsg = await response.Content.ReadAsStringAsync();
                        NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Lỗi đăng nhập", Detail = errorMsg, Duration = 5000 });
                        return isLogin = false;
                    }

                    var loginData = await response.Content.ReadFromJsonAsync<sp_Authentication_Login>();

                    if (loginData is not null)
                    {
                        isLogin = true;
                        var claims = new List<Claim>()
                            {
                                new Claim("UserLogin", loginData.UserLogin ?? ""),
                                new Claim("Password", loginData.PasswordChar ?? ""),
                                new Claim("UserID", loginData.UserID.ToString()),
                                new Claim("FullName", loginData.FullName ?? ""),
                                new Claim("Email", loginData.Email ?? ""),
                                new Claim("Gmail", loginData.GoogleEmail ?? ""),
                                new Claim("IsAdmin", loginData.IsAdmin.ToString() ?? ""),
                                new Claim("GroupId", loginData.GroupId.ToString() ?? ""),
                                new Claim("GroupName", loginData.GroupName ?? ""),
                                new Claim("MemberCompanyCode", loginData.MemberCompanyCode ?? ""),
                                new Claim("DepartmentName", loginData.DepartmentName ?? ""),
                                new Claim("DepartmentCode", loginData.DepartmentCode ?? ""),
                                new Claim("MemberCompanyName", loginData.MemberCompanyName ?? ""),
                                new Claim("MemberCompanyShortName", loginData.MemberCompanyShortName ?? ""),
                                new Claim("Expired", DateTime.UtcNow.AddHours(12).ToString()),
                                //new Claim("PermissionJson", JsonConvert.SerializeObject(userLoginData.List_PagePermission)),
                                new Claim("Server", server)
                            };
                        var claimsIdentity = new ClaimsIdentity(
                                                claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        var authProperties = new AuthenticationProperties();
                        authProperties.IsPersistent = true;
                        authProperties.RedirectUri = HttpContextAccessor!.HttpContext!.Request.Host.Value;
                        authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24);

                        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
                        //string str = JsonConvert.SerializeObject(claims);
                        //var encr = _apiService.auth_PermissionService.Encrypt(str, true);
                        await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        claimsPrincipal,
                        authProperties);

                    }
                    else
                    {
                        isLogin = false;
                    }
                }
                catch (Exception ex)
                {
                    isLogin = false;
                    errorMessage = ex.Message;
                    Log.Error(ex, "Lỗi khi login, gọi ef sp_Authentication_Login với {username}", username);
                }
                finally
                {
                    //resDTO.Dispose();
                }
            }
            else
            {
                isLogin = false;
            }
            if (!isLogin)
            {
                NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Login failed", Detail = errorMessage, Duration = 8000 });
            }
            return isLogin;
        }
    }
}