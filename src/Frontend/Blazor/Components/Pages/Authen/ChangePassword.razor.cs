using gtas_vpp_fe.Features.IdentityAccess.Api;
using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen;

public partial class ChangePassword
{
    [Inject] public AccountApiClient AccountApi { get; set; } = default!;
    [Inject] public NavigationManager Navigation { get; set; } = default!;
    [Inject] public Microsoft.Extensions.Localization.IStringLocalizer<App> Localizer { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "required")]
    public string? RequiredValue { get; set; }

    protected PasswordChangeReqDTO Model { get; set; } = new();
    protected bool IsLoading { get; set; }
    protected bool Required => string.Equals(RequiredValue, "1", StringComparison.Ordinal);
    protected string? ErrorMessage { get; set; }
    protected string? SuccessMessage { get; set; }

    private async Task SubmitAsync(PasswordChangeReqDTO request)
    {
        IsLoading = true;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            await AccountApi.ChangePasswordAsync(request);
            SuccessMessage = Localizer["ChangePasswordSuccess"];
            await Task.Delay(1200);
            Navigation.NavigateTo("/perform-logout", forceLoad: true);
        }
        catch (Exception exception)
        {
            ErrorMessage = AccountLifecycleUiMapper.GetMessage(
                exception,
                Localizer,
                "PasswordChangeInvalid");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
