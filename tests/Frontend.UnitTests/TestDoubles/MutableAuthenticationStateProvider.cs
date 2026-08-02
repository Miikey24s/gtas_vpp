using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace gtas_vpp_fe.Tests.TestDoubles;

internal sealed class MutableAuthenticationStateProvider(ClaimsPrincipal principal)
    : AuthenticationStateProvider
{
    private ClaimsPrincipal _principal = principal;

    public void SetPrincipal(ClaimsPrincipal principal)
    {
        _principal = principal;
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(_principal));
}
