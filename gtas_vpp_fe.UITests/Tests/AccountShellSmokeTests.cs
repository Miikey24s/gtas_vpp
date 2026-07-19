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
            "Account/Login",
            "Account/ForgotPassword",
            "Account/ResetPassword",
            "Account/Register",
            "Account/ConfirmEmail"
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
                await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
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

                audit[0].Should().Be(0, $"{route} must not overflow at {viewport.Width}px");
                audit[1].Should().Be(1, $"{route} should render one account card");
                audit[2].Should().Be(1, $"{route} should render one account title");
                audit[3].Should().Be(0, $"{route} should use VppIcon for functional icons");

                (await Page.Locator(".vpp-login-art").CountAsync()).Should().Be(0, $"{route} should use the centered account shell without hero artwork");
                (await Page.Locator(".vpp-account-page-compact").CountAsync()).Should()
                    .Be(1, $"{route} should use the centered compact shell");
                (await Page.Locator(".vpp-brand-mark svg").CountAsync()).Should().Be(1, $"{route} should use the shared vector brand mark");
                (await Page.Locator(".vpp-account-language-switch").CountAsync()).Should().Be(1, $"{route} should expose VI/EN switching");

                if (!string.IsNullOrWhiteSpace(evidenceDirectory) && viewport.Width == 1366)
                {
                    Directory.CreateDirectory(evidenceDirectory);
                    await Page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(
                            evidenceDirectory,
                            $"account-{route.Split('/').Last().ToLowerInvariant()}-1366x768.png"),
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

    [Fact]
    public async Task AccountForms_UseLocalizedValidationWithoutLayoutShiftOrInternalScroll()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FRegister");
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Đăng ký" }).WaitForAsync();

        (await Page.Locator("input[name=EmployeeCode]").CountAsync()).Should().Be(0);
        (await Page.GetByText("Email công ty", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);
        (await Page.Locator(".vpp-account-description").CountAsync()).Should().Be(0);

        var registerButton = await GetInteractiveButtonAsync(Page.Locator("body"), "Đăng ký");
        var before = await registerButton.BoundingBoxAsync();
        await registerButton.ClickAsync();
        await Page.GetByText("Vui lòng nhập tên đăng nhập.", new() { Exact = true }).WaitForAsync();
        var after = await registerButton.BoundingBoxAsync();
        Math.Abs((before?.Y ?? 0) - (after?.Y ?? 0)).Should().BeLessThan(1.5f, "reserved validation slots should prevent button movement");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "account-register-validation-vi-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        (await Page.Locator(".vpp-login-card").EvaluateAsync<bool>("element => element.scrollHeight <= element.clientHeight + 1"))
            .Should().BeTrue("the desktop registration card must not have an internal scrollbar");
        var viewportMetrics = await Page.EvaluateAsync<int[]>("""
            () => {
                const card = document.querySelector('.vpp-login-card');
                return [document.documentElement.scrollHeight, window.innerHeight, Math.round(card?.getBoundingClientRect().height ?? 0)];
            }
            """);
        viewportMetrics[0].Should().BeLessThanOrEqualTo(
            viewportMetrics[1] + 1,
            $"the desktop registration route should fit without page scrolling (page={viewportMetrics[0]}, viewport={viewportMetrics[1]}, card={viewportMetrics[2]})");

        await Page.Locator(".vpp-account-language-switch").ClickAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Register" }).WaitForAsync();
        var englishButton = await GetInteractiveButtonAsync(Page.Locator("body"), "Register");
        await englishButton.ClickAsync();
        await Page.GetByText("Please enter your username.", new() { Exact = true }).WaitForAsync();
        (await Page.GetByText("Company email", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);

        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin");
        var loginButton = await GetInteractiveButtonAsync(Page.Locator("body"), "Đăng nhập");
        var alignment = await Page.EvaluateAsync<double[]>("""
            () => {
                const username = document.querySelector('input[name="Username"]')?.getBoundingClientRect();
                const password = document.querySelector('.vpp-login-password-wrapper')?.getBoundingClientRect();
                const button = document.querySelector('.vpp-login-btn')?.getBoundingClientRect();
                const links = document.querySelector('.vpp-login-links')?.getBoundingClientRect();
                const separator = document.querySelector('.vpp-login-links > span')?.getBoundingClientRect();

                if (!username || !password || !button || !links || !separator) {
                    throw new Error('Unable to measure login alignment geometry.');
                }

                return [
                    username.left,
                    password.left,
                    button.left,
                    username.right,
                    password.right,
                    button.right,
                    separator.left + separator.width / 2,
                    button.left + button.width / 2,
                    links.left,
                    links.right
                ];
            }
            """);
        Math.Abs(alignment[0] - alignment[1]).Should().BeLessThan(1, "username and password underlines should start together");
        Math.Abs(alignment[0] - alignment[2]).Should().BeLessThan(1, "fields and primary action should share the same left edge");
        Math.Abs(alignment[3] - alignment[4]).Should().BeLessThan(1, "username and password underlines should end together");
        Math.Abs(alignment[3] - alignment[5]).Should().BeLessThan(1, "fields and primary action should share the same right edge");
        Math.Abs(alignment[2] - alignment[8]).Should().BeLessThan(1, "account links should share the primary action left edge");
        Math.Abs(alignment[5] - alignment[9]).Should().BeLessThan(1, "account links should share the primary action right edge");
        var linkLayout = await Page.EvaluateAsync<string>("""
            () => {
                const links = document.querySelector('.vpp-login-links');
                const style = links ? getComputedStyle(links) : null;
                return JSON.stringify({
                    display: style?.display,
                    columns: style?.gridTemplateColumns,
                    gap: style?.gap,
                    children: links ? [...links.children].map(element => {
                        const rect = element.getBoundingClientRect();
                        return { tag: element.tagName, text: element.textContent?.trim(), left: rect.left, right: rect.right };
                    }) : []
                });
            }
            """);
        Math.Abs(alignment[6] - alignment[7]).Should().BeLessThan(1, $"the separator between account links should sit on the card centerline; layout={linkLayout}");

        var loginBefore = await loginButton.BoundingBoxAsync();
        await loginButton.ClickAsync();
        await Page.GetByText("Vui lòng nhập tên đăng nhập.", new() { Exact = true }).WaitForAsync();
        var loginAfter = await loginButton.BoundingBoxAsync();
        Math.Abs((loginBefore?.Y ?? 0) - (loginAfter?.Y ?? 0)).Should().BeLessThan(1.5f, "login validation should not shift the action area");

        await Page.Locator("input[name=Username]").FillAsync("invalid-account-for-inline-feedback");
        await Page.Locator("input[name=Password]").FillAsync("Invalid-password-1!");
        await loginButton.ClickAsync();
        await Page.Locator(".vpp-login-form-error-slot [role=alert]").WaitForAsync();
        (await Page.Locator(".rz-notification:visible").CountAsync()).Should().Be(0, "credential failures should stay next to the form instead of opening a toast");
    }
}
