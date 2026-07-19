using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ResetPassword
{
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;
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

    private async Task SubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            using var response = await client.PostAsJsonAsync(
                Config.ApiAccountResetPasswordEndpoint,
                Model);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await AccountLifecycleUiMapper.ReadErrorMessageAsync(
                    response,
                    Loc,
                    "ResetLinkInvalid");
                return;
            }

            SuccessMessage = Loc["ResetPasswordSuccess"];
            await Task.Delay(1200);
            Navigation.NavigateTo(Config.LoginPagePath, forceLoad: true);
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
