using gtas_vpp_fe.Helpers;
using Microsoft.AspNetCore.Components;

namespace gtas_vpp_fe.Components.Pages.Authen
{
    public partial class Login
    {
        protected override Task OnInitializedAsync()
        {
            UriHelper.NavigateTo(Config.LoginPagePath, true);
            return Task.CompletedTask;
        }
    }
}
