using System.Net.Http.Json;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using gtas_vpp_shared.DTOs.Res.Account;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ResendConfirmation
{
    [Inject] public IHttpClientFactory HttpClientFactory { get; set; } = default!;

    protected EmailConfirmationResendReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync(EmailConfirmationResendReqDTO _submittedModel)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            var client = HttpClientFactory.CreateClient(Config.HttpClientName);
            using var response = await client.PostAsJsonAsync(
                Config.ApiAccountResendConfirmationEndpoint,
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
            SuccessMessage = Loc["ResendConfirmationAccepted"];
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
