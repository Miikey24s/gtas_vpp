using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class Register
{
    [Inject] public AccountApiClient AccountApi { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    protected AccountRegistrationReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync(AccountRegistrationReqDTO request)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            _ = await AccountApi.RegisterAsync(request);
            SuccessMessage = Loc["RegistrationAccepted"];
            Model = new AccountRegistrationReqDTO();
        }
        catch (ApiRequestException exception)
        {
            ErrorMessage = AccountLifecycleUiMapper.GetMessage(
                exception,
                Loc,
                "RegistrationInvalid");
        }
        catch (HttpRequestException)
        {
            ErrorMessage = Loc["RecoveryConnectionFailed"];
        }
        finally
        {
            IsLoading = false;
        }
    }

}
