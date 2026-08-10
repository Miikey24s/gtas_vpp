// PAGE LOGIC: Authen/ForgotPassword.razor.cs *
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ForgotPassword
{
    [Inject] public AccountApiClient AccountApi { get; set; } = default!;

    protected PasswordRecoveryReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync(PasswordRecoveryReqDTO request)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            _ = await AccountApi.RequestPasswordRecoveryAsync(request);
            SuccessMessage = Loc["RecoveryRequestAccepted"];
        }
        catch (ApiRequestException exception)
        {
            ErrorMessage = AccountLifecycleUiMapper.GetMessage(
                exception,
                Loc,
                "RequestInvalid");
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
