using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class AccountShellSmokeTests : TestBase
{
    [Fact]
    public async Task AnonymousAccountRoutes_ShareTheResponsiveAccountShell()
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);
        Page.RequestFailed += (_, request) =>
        {
            var isExpectedCircuitDisconnect = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && uri.AbsolutePath.Equals("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase)
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        var routes = new[]
        {
            new AccountRoute("Account/Login", Artwork: true, Compact: false),
            new AccountRoute("Account/ForgotPassword", Artwork: false, Compact: true),
            new AccountRoute("Account/ResetPassword", Artwork: false, Compact: true),
            new AccountRoute("Account/Register", Artwork: true, Compact: false),
            new AccountRoute("Account/ConfirmEmail", Artwork: false, Compact: true)
        };

        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin");

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            foreach (var route in routes)
            {
                await Page.GotoAsync($"{BaseUrl}{route.Path}", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
                await Page.Locator(".vpp-login-card").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible
                });
                await Page.WaitForTimeoutAsync(250);

                var audit = await Page.EvaluateAsync<int[]>("""
                    () => [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        document.querySelectorAll('.vpp-login-card').length,
                        document.querySelectorAll('.vpp-login-title').length,
                        document.querySelectorAll('.material-symbols-outlined:not(.vpp-icon)').length
                    ]
                    """);

                audit[0].Should().Be(0, $"{route.Path} must not overflow at {viewport.Width}px");
                audit[1].Should().Be(1, $"{route.Path} should render one account card");
                audit[2].Should().Be(1, $"{route.Path} should render one account title");
                audit[3].Should().Be(0, $"{route.Path} should use VppIcon for functional icons");

                (await Page.Locator(".vpp-login-art").CountAsync()).Should()
                    .Be(route.Artwork ? 1 : 0, $"{route.Path} artwork policy should be explicit");
                (await Page.Locator(".vpp-account-page-compact").CountAsync()).Should()
                    .Be(route.Compact ? 1 : 0, $"{route.Path} compact policy should be explicit");

                if (!string.IsNullOrWhiteSpace(evidenceDirectory) && viewport.Width == 1366)
                {
                    Directory.CreateDirectory(evidenceDirectory);
                    await Page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(
                            evidenceDirectory,
                            $"account-{route.Path.Split('/').Last().ToLowerInvariant()}-1366x768.png"),
                        FullPage = true,
                        Animations = ScreenshotAnimations.Disabled,
                        Caret = ScreenshotCaret.Hide,
                        Scale = ScreenshotScale.Css
                    });
                }
            }
        }

        await Page.GotoAsync($"{BaseUrl}Account/ResetPassword");
        (await Page.Locator(".vpp-inline-alert-error").InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        (await Page.Locator("form").CountAsync()).Should().Be(0, "an invalid reset link must not show a usable reset form");

        await Page.GotoAsync($"{BaseUrl}Account/Login");
        (await Page.Locator(".vpp-login-links").InnerTextAsync()).Should().Contain("Quên mật khẩu?");

        await Page.GotoAsync($"{BaseUrl}set-language?culture=en&returnUrl=%2FAccount%2FForgotPassword");
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Recover password" }).WaitForAsync();
        (await Page.Locator(".vpp-account-back-link").InnerTextAsync()).Should().Contain("Back to sign in");

        browserErrors.Should().BeEmpty("account routes should not emit browser errors");
        requestFailures.Should().BeEmpty("account routes should not issue failed requests");
    }

    private sealed record AccountRoute(string Path, bool Artwork, bool Compact);
}
