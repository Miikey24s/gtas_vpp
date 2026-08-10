// PAGE LOGIC: Authen/ConfirmEmail.razor.cs *
using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_fe.Services;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ConfirmEmail
{
    [Inject] public AccountApiClient AccountApi { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "userId")]
    public int UserId { get; set; }

    [SupplyParameterFromQuery(Name = "token")]
    public string? Token { get; set; }

    protected bool IsLoading { get; private set; } = true;
    protected string? ErrorMessage { get; private set; }
    protected string? SuccessMessage { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        // Xác nhận làm đổi state server. Không được gửi một lần khi prerender và
        // lần thứ hai khi interactive circuit bắt đầu.
        if (!RendererInfo.IsInteractive)
        {
            return;
        }

        if (UserId <= 0 || string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = Loc["ConfirmationLinkInvalid"];
            IsLoading = false;
            return;
        }

        try
        {
            _ = await AccountApi.ConfirmEmailAsync(UserId, Token);
            SuccessMessage = Loc["EmailConfirmed"];
        }
        catch (ApiRequestException exception)
        {
            ErrorMessage = AccountLifecycleUiMapper.GetMessage(
                exception,
                Loc,
                "ConfirmationLinkInvalid");
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
