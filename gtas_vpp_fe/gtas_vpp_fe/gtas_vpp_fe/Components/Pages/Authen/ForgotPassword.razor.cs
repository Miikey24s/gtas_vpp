using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ForgotPassword
{
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;

    protected PasswordRecoveryReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            using var response = await client.PostAsJsonAsync(
                Config.ApiAccountRecoveryEndpoint,
                Model);
            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await AccountLifecycleUiMapper.ReadErrorMessageAsync(
                    response,
                    Loc,
                    "RequestInvalid");
                return;
            }

            _ = await response.Content.ReadFromJsonAsync<AccountLifecycleResDTO>();
            SuccessMessage = Loc["RecoveryRequestAccepted"];
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
