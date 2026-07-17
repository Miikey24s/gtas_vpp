using gtas_vpp_fe.Helpers;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class AccountLoginRedirectPolicyTests
{
    [Fact]
    public void TemporaryPassword_OverridesRequestedBusinessReturnUrl()
    {
        var result = AccountLoginRedirectPolicy.Resolve(
            mustChangePassword: true,
            requestedReturnUrl: "/report");

        Assert.Equal(AccountLoginRedirectPolicy.RequiredPasswordChangeRoute, result);
    }

    [Fact]
    public void NormalPassword_PreservesRequestedReturnUrl()
    {
        var result = AccountLoginRedirectPolicy.Resolve(
            mustChangePassword: false,
            requestedReturnUrl: "/report");

        Assert.Equal("/report", result);
    }
}
