// PAGE LOGIC: Authen/ResetPassword.razor.cs *
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ResetPassword
{
    [Inject] public AccountApiClient AccountApi { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public int UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    protected PasswordResetReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected bool HasValidResetLink { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    protected override void OnParametersSet()
    {
        HasValidResetLink = UserId > 0 && !string.IsNullOrWhiteSpace(Token);
        if (!HasValidResetLink)
        {
            ErrorMessage = Loc["ResetLinkInvalid"];
            return;
        }

        Model.UserId = UserId;
        Model.Token = Token ?? string.Empty;
    }

    private async Task SubmitAsync(PasswordResetReqDTO request)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            _ = await AccountApi.ResetPasswordAsync(request);
            SuccessMessage = Loc["ResetPasswordSuccess"];
            await Task.Delay(1200);
            Navigation.NavigateTo(Config.LoginPagePath, forceLoad: true);
        }
        catch (ApiRequestException exception)
        {
            ErrorMessage = AccountLifecycleUiMapper.GetMessage(
                exception,
                Loc,
                "ResetLinkInvalid");
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
