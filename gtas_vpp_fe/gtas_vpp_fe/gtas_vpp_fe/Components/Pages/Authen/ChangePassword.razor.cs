using gtas_vpp_fe.Services;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ChangePassword
{
    [Inject] public IAPIServices Api { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public Microsoft.Extensions.Localization.IStringLocalizer<App> Localizer { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "required")]
    public string? RequiredValue { get; set; }

    protected PasswordChangeReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected bool Required => string.Equals(RequiredValue, "1", StringComparison.Ordinal);
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var result = await Api.PostFromApiAsync<AccountLifecycleResDTO>(
                Helpers.Config.ApiAccountChangePasswordEndpoint,
                Model);
            SuccessMessage = result?.Message
                ?? "Đổi mật khẩu thành công. Đang yêu cầu đăng nhập lại...";
            await Task.Delay(500);
            Navigation.NavigateTo("/perform-logout", forceLoad: true);
        }
        catch (Exception exception)
        {
            ErrorMessage = UiErrorMapper.GetMessage(exception, Localizer);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
