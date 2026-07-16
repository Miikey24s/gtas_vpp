using gtas_vpp_fe.Components.Pages.Authen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace gtas_vpp_fe.Tests.Architecture;

public sealed class AccountLifecycleRouteTests
{
    [Theory]
    [InlineData(typeof(Register), "/Account/Register")]
    [InlineData(typeof(ForgotPassword), "/Account/ForgotPassword")]
    [InlineData(typeof(ResetPassword), "/Account/ResetPassword")]
    [InlineData(typeof(ConfirmEmail), "/Account/ConfirmEmail")]
    [InlineData(typeof(ChangePassword), "/Account/ChangePassword")]
    public void AccountLifecyclePage_ExposesExpectedRoute(Type componentType, string route)
    {
        var routes = componentType.GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template);

        Assert.Contains(route, routes);
    }

    [Theory]
    [InlineData(typeof(Register))]
    [InlineData(typeof(ForgotPassword))]
    [InlineData(typeof(ResetPassword))]
    [InlineData(typeof(ConfirmEmail))]
    public void PublicAccountLifecyclePage_IsAnonymous(Type componentType)
    {
        Assert.NotEmpty(componentType.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void ChangePasswordPage_RequiresAuthenticatedCookie()
    {
        Assert.NotEmpty(typeof(ChangePassword).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
    }
}
