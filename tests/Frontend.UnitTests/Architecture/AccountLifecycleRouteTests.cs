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
    [InlineData(typeof(ResendConfirmation), "/Account/ResendConfirmation")]
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
    [InlineData(typeof(ResendConfirmation))]
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

    [Theory]
    [InlineData("Register.razor.cs")]
    [InlineData("ForgotPassword.razor.cs")]
    [InlineData("ResendConfirmation.razor.cs")]
    [InlineData("ResetPassword.razor.cs")]
    [InlineData("ConfirmEmail.razor.cs")]
    [InlineData("ChangePassword.razor.cs")]
    [InlineData("LoginPage.razor.cs")]
    public void AccountPage_DoesNotOwnLowLevelHttpTransport(string fileName)
    {
        var source = ReadSource("Components", "Pages", "Authen", fileName);

        Assert.DoesNotContain("IHttpClientFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAPIServices", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthenticatedUserMenu_ExposesVoluntaryPasswordChangeInRuntimeAndAtlas()
    {
        var menu = ReadSource("Components", "Layout", "UserMenu.razor");
        var atlas = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "docs", "design", "atlas", "atlas.js"));

        Assert.Contains("href=\"/Account/ChangePassword\"", menu, StringComparison.Ordinal);
        Assert.Contains("screenTarget(\"change-password\")", atlas, StringComparison.Ordinal);
        Assert.Contains("số và ký tự đặc biệt", atlas, StringComparison.Ordinal);
        Assert.Contains("chờ quản trị viên phê duyệt", atlas, StringComparison.Ordinal);
    }

    [Fact]
    public void ChangePasswordPage_UsesTypedClientThenForcesSessionRevocation()
    {
        var source = ReadSource("Components", "Pages", "Authen", "ChangePassword.razor.cs");

        Assert.Contains("AccountApi.ChangePasswordAsync(request)", source, StringComparison.Ordinal);
        Assert.Contains("NavigateTo(\"/perform-logout\", forceLoad: true)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LogoutPage_AlwaysContinuesToSessionRevocation()
    {
        var source = ReadSource("Components", "Pages", "Authen", "Logout.razor");

        Assert.DoesNotContain("@rendermode", source, StringComparison.Ordinal);
        Assert.Contains("finally", source, StringComparison.Ordinal);
        Assert.Contains("AuthSessionFingerprint.MatchesOrIsUnspecified", source, StringComparison.Ordinal);
        Assert.Contains("var logoutPath = Config.PerformLogoutPath;", source, StringComparison.Ordinal);
        Assert.Contains("NavigateTo(logoutPath, true)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginPage_UsesOneFormSubmitPathAndGuardsRapidResubmission()
    {
        var markup = ReadSource("Components", "Pages", "Authen", "LoginPage.razor");
        var code = ReadSource("Components", "Pages", "Authen", "LoginPage.razor.cs");

        Assert.Contains("Submit=\"LoginSubmit\"", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("@onkeyup", markup, StringComparison.Ordinal);
        Assert.Contains("if (isLoading)", code, StringComparison.Ordinal);
        Assert.Contains("var isRedirecting = false;", code, StringComparison.Ordinal);
        Assert.Contains("if (!isRedirecting)", code, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("VppAccountTextField.razor")]
    [InlineData("VppLanguageSwitch.razor")]
    [InlineData("VppPasswordField.razor")]
    public void AccountOnlyComponents_AreOwnedByIdentityAccess(string fileName)
    {
        var frontendRoot = Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");

        Assert.True(File.Exists(Path.Combine(
            frontendRoot,
            "Features",
            "IdentityAccess",
            "Components",
            fileName)));
        Assert.False(File.Exists(Path.Combine(
            frontendRoot,
            "Components",
            "Shared",
            fileName)));
    }

    [Fact]
    public void ProjectRootImports_CoverComponentsAndFeatureRazorFiles()
    {
        var frontendRoot = Path.Combine(FindRepositoryRoot(), "src", "Frontend", "Blazor");
        var imports = File.ReadAllText(Path.Combine(frontendRoot, "_Imports.razor"));

        Assert.False(File.Exists(Path.Combine(frontendRoot, "Components", "_Imports.razor")));
        Assert.Contains("gtas_vpp_fe.Features.IdentityAccess.Components", imports, StringComparison.Ordinal);
        Assert.DoesNotContain("gtas_vpp_fe.Components.Shared", imports, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Register.razor")]
    [InlineData("ForgotPassword.razor")]
    [InlineData("ResendConfirmation.razor")]
    [InlineData("LoginPage.razor")]
    public void AccountTextForms_UseSharedIdentityField(string fileName)
    {
        var source = ReadSource("Components", "Pages", "Authen", fileName);

        Assert.Contains("<VppAccountTextField", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Style=\"position: absolute\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AccountMotif_UsesSharedSurfaceAndControlTokens()
    {
        var css = ReadSource("wwwroot", "css", "vpp-login.css");

        Assert.Contains("background: var(--vpp-surface-canvas);", css, StringComparison.Ordinal);
        Assert.Contains("background: var(--vpp-surface-raised);", css, StringComparison.Ordinal);
        Assert.Contains("min-height: var(--vpp-button-height);", css, StringComparison.Ordinal);
        Assert.DoesNotContain("--vpp-text-primary: #", css, StringComparison.Ordinal);
        Assert.DoesNotContain("transform: translateY(-1px)", css, StringComparison.Ordinal);
        Assert.DoesNotContain("height: 48px", css, StringComparison.Ordinal);
    }

    private static string ReadSource(params string[] relativeSegments)
    {
        var root = FindRepositoryRoot();
        var segments = new[] { root, "src", "Frontend", "Blazor" }
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
            if (File.Exists(Path.Combine(directory.FullName, "gtas_vpp.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the GTAS VPP repository root.");
    }
}
