using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ResendConfirmation
{
    [Inject] public AccountApiClient AccountApi { get; set; } = default!;

    protected EmailConfirmationResendReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync(EmailConfirmationResendReqDTO request)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            _ = await AccountApi.ResendConfirmationAsync(request);
            SuccessMessage = Loc["ResendConfirmationAccepted"];
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
