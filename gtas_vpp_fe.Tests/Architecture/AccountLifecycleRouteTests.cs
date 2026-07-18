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
    [InlineData(typeof(Logout), "/logoutprocess")]
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
    [InlineData(typeof(Logout))]
    public void PublicAccountLifecyclePage_IsAnonymous(Type componentType)
    {
        Assert.NotEmpty(componentType.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true));
    }

    [Fact]
    public void ChangePasswordPage_RequiresAuthenticatedCookie()
    {
        Assert.NotEmpty(typeof(ChangePassword).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true));
    }

    [Fact]
    public void LogoutPage_IsInteractiveAndAlwaysContinuesToSessionRevocation()
    {
        var source = ReadSource("Components", "Pages", "Authen", "Logout.razor");

        Assert.Contains("@rendermode InteractiveServer", source, StringComparison.Ordinal);
        Assert.Contains("finally", source, StringComparison.Ordinal);
        Assert.Contains("NavigateTo(Config.PerformLogoutPath, true)", source, StringComparison.Ordinal);
    }

    private static string ReadSource(params string[] relativeSegments)
    {
        var root = FindRepositoryRoot();
        var segments = new[] { root, "gtas_vpp_fe", "gtas_vpp_fe", "gtas_vpp_fe" }
            .Concat(relativeSegments)
            .ToArray();
        return File.ReadAllText(Path.Combine(segments));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
