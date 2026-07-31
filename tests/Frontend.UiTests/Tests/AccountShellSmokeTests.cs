using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
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
            // Điều hướng giữa các trang account hủy negotiation SignalR đang dở —
            // nhiễu vô hại đã được allowlist ở các test dashboard.
            var isExpectedCircuitTransitionError = message.Text.Contains(
                    "Failed to complete negotiation with the server",
                    StringComparison.OrdinalIgnoreCase)
                && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase)
                && !isExpectedCircuitTransitionError)
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
            // Điều hướng nhanh giữa các trang account hủy static asset đang tải dở —
            // chỉ allowlist ERR_ABORTED, mọi failure khác vẫn đánh rớt test (đồng bộ
            // với allowlist của DashboardMyOrdersVisualTests).
            var isExpectedNavigationAssetAbort = uri is not null
                && (uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/initializers", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect && !isExpectedNavigationAssetAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        var routes = new[]
        {
            "Account/Login",
            "loginprocess",
            "Account/ForgotPassword",
            "Account/ResetPassword",
            "Account/Register",
            "Account/ResendConfirmation",
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
                // Card đã visible ở trên; chờ double-rAF cho render churn flush xong trước khi đo audit.
                await WaitForRenderSettleAsync();

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
                var brandMark = Page.Locator(".vpp-brand-mark img[src$='vpp-app-icon.svg']");
                await brandMark.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 30_000
                });
                (await brandMark.CountAsync()).Should().Be(1, $"{route} should use the shared SVG brand asset");
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
        (await Page.Locator(".vpp-account-back-link").InnerTextAsync()).Should().Contain("Back to Sign In");

        browserErrors.Should().BeEmpty("account routes should not emit browser errors");
        requestFailures.Should().BeEmpty("account routes should not issue failed requests");
    }

    [Fact]
    public async Task AccountForms_UseLocalizedValidationWithoutLayoutShiftOrInternalScroll()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FRegister");
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Tạo tài khoản" }).WaitForAsync();

        (await Page.Locator("input[name=EmployeeCode]").CountAsync()).Should().Be(0);
        (await Page.GetByText("Email công ty", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);
        (await Page.GetByText("Xác nhận mật khẩu", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);
        (await Page.Locator(".vpp-account-description").CountAsync()).Should().Be(1, "the account card shows the Atlas context description under the title");

        var registerButton = await GetInteractiveButtonAsync(Page.Locator("body"), "Tạo tài khoản");
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
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Create account" }).WaitForAsync();
        var englishButton = await GetInteractiveButtonAsync(Page.Locator("body"), "Create account");
        await englishButton.ClickAsync();
        await Page.GetByText("Please enter your username.", new() { Exact = true }).WaitForAsync();
        (await Page.GetByText("Company email", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);
        (await Page.GetByText("Confirm password", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);

        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin");
        var loginButton = await GetInteractiveButtonAsync(Page.Locator("body"), "Đăng nhập");
        var alignment = await Page.EvaluateAsync<double[]>("""
            () => {
                const username = document.querySelector('input[name="Username"]')?.getBoundingClientRect();
                const password = document.querySelector('.vpp-login-password-wrapper')?.getBoundingClientRect();
                const button = document.querySelector('.vpp-login-btn')?.getBoundingClientRect();
                const links = document.querySelector('.vpp-login-links')?.getBoundingClientRect();
                const firstLink = document.querySelector('.vpp-login-links > a:first-child')?.getBoundingClientRect();
                const secondLink = document.querySelector('.vpp-login-links > a:last-child')?.getBoundingClientRect();
                const passwordControl = document.querySelector('.vpp-login-password-wrapper .vpp-login-control');
                const passwordInput = document.querySelector('.vpp-login-password-wrapper input');

                if (!username || !password || !button || !links || !firstLink || !secondLink || !passwordControl || !passwordInput) {
                    throw new Error('Unable to measure login alignment geometry.');
                }

                return [
                    username.left,
                    password.left,
                    button.left,
                    username.right,
                    password.right,
                    button.right,
                    links.left + links.width / 2,
                    button.left + button.width / 2,
                    firstLink.width,
                    secondLink.width,
                    parseFloat(getComputedStyle(document.querySelector('input[name="Username"]')).borderBottomWidth),
                    parseFloat(getComputedStyle(document.querySelector('.vpp-login-password-wrapper')).borderBottomWidth),
                    parseFloat(getComputedStyle(passwordControl).borderBottomWidth),
                    parseFloat(getComputedStyle(passwordInput).borderBottomWidth)
                ];
            }
            """);
        var passwordLayout = await Page.EvaluateAsync<string>("""
            () => {
                const wrapper = document.querySelector('.vpp-login-password-wrapper');
                return JSON.stringify(wrapper ? [...wrapper.querySelectorAll('*')].map(element => {
                    const style = getComputedStyle(element);
                    return {
                        tag: element.tagName,
                        classes: element.className,
                        name: element.getAttribute('name'),
                        borderBottomWidth: style.borderBottomWidth,
                        borderBottomStyle: style.borderBottomStyle
                    };
                }) : []);
            }
            """);
        Math.Abs(alignment[0] - alignment[1]).Should().BeLessThan(1, "username and password underlines should start together");
        Math.Abs(alignment[0] - alignment[2]).Should().BeLessThan(1, "fields and primary action should share the same left edge");
        Math.Abs(alignment[3] - alignment[4]).Should().BeLessThan(1, "username and password underlines should end together");
        Math.Abs(alignment[3] - alignment[5]).Should().BeLessThan(1, "fields and primary action should share the same right edge");
        Math.Abs(alignment[6] - alignment[7]).Should().BeLessThan(1, "the secondary action row should sit on the primary action centerline");
        Math.Abs(alignment[8] - alignment[9]).Should().BeLessThan(1, "forgot-password and registration actions should have equal widths");
        alignment[10].Should().Be(1, "standard account inputs should use a one-pixel underline");
        alignment[11].Should().Be(1, "the password wrapper should own one one-pixel underline");
        alignment[12].Should().Be(0, $"the nested Radzen password control must not add another underline; layout={passwordLayout}");
        alignment[13].Should().Be(0, $"the nested password input must not add another underline; layout={passwordLayout}");
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
        linkLayout.Should().Contain("\"display\":\"grid\"", "the account actions should retain the balanced two-column layout");

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "account-login-actions-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        var loginBefore = await loginButton.BoundingBoxAsync();
        await loginButton.ClickAsync();
        await Page.GetByText("Vui lòng nhập tên đăng nhập.", new() { Exact = true }).WaitForAsync();
        var loginAfter = await loginButton.BoundingBoxAsync();
        Math.Abs((loginBefore?.Y ?? 0) - (loginAfter?.Y ?? 0)).Should().BeLessThan(1.5f, "login validation should not shift the action area");

        await Page.Locator("input[name=Username]").FillAsync("invalid-account-for-inline-feedback");
        await Page.Locator("input[name=Password]").FillAsync("Invalid-password-1!");
        await loginButton.ClickAsync();
        await Page.Locator(".vpp-login-password-wrapper + .vpp-validation-slot [role=alert]").WaitForAsync();
        (await Page.Locator(".rz-notification:visible").CountAsync()).Should().Be(0, "credential failures should stay next to the form instead of opening a toast");
    }

    [Fact]
    public async Task LoginRegisterAndRecovery_ShareOneVerticalRhythm()
    {
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin");

        var routes = new[]
        {
            "Account/Login",
            "Account/Register",
            "Account/ForgotPassword",
            "Account/ResendConfirmation"
        };

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            var measurements = new Dictionary<string, double[]>();

            foreach (var route in routes)
            {
                await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
                await Page.Locator(".vpp-login-card").WaitForAsync();

                measurements[route] = await Page.EvaluateAsync<double[]>("""
                    () => {
                        const topbar = document.querySelector('.vpp-account-topbar')?.getBoundingClientRect();
                        const brand = document.querySelector('.vpp-account-brand')?.getBoundingClientRect();
                        const language = document.querySelector('.vpp-account-language-switch')?.getBoundingClientRect();
                        const header = document.querySelector('.vpp-login-header')?.getBoundingClientRect();
                        const form = document.querySelector('.vpp-login-form');
                        const fields = [...document.querySelectorAll('.vpp-login-form > .vpp-login-field')]
                            .map(element => element.getBoundingClientRect());
                        const button = document.querySelector('.vpp-login-btn')?.getBoundingClientRect();
                        const links = document.querySelector('.vpp-login-links')?.getBoundingClientRect();

                        // The intro block is the last visible element before the form: the description-bearing
                        // header, or an Atlas static inline alert (e.g. the forgot-password safety note).
                        let intro = form?.previousElementSibling;
                        while (intro && intro.getBoundingClientRect().height === 0) {
                            intro = intro.previousElementSibling;
                        }
                        const introRect = intro?.getBoundingClientRect();

                        if (!topbar || !brand || !language || !header || !introRect || fields.length === 0 || !button || !links) {
                            throw new Error('Unable to measure the shared account rhythm.');
                        }

                        return [
                            Math.abs((brand.top + brand.height / 2) - (language.top + language.height / 2)),
                            header.top - topbar.bottom,
                            fields[0].top - introRect.bottom,
                            button.top - fields.at(-1).bottom,
                            links.top - button.bottom,
                            brand.height,
                            language.height,
                            button.height
                        ];
                    }
                    """);
            }

            foreach (var (route, rhythm) in measurements)
            {
                rhythm[0].Should().BeLessThan(1, $"brand and language controls must share one centerline on {route} at {viewport.Width}px");
                rhythm[5].Should().BeApproximately(rhythm[6], 1, $"topbar controls must share one height on {route} at {viewport.Width}px");
                rhythm[7].Should().BeApproximately(48, 1, $"primary actions must share one height on {route} at {viewport.Width}px");
            }

            var reference = measurements["Account/Login"];
            foreach (var route in routes.Skip(1))
            {
                var rhythm = measurements[route];
                rhythm[1].Should().BeApproximately(reference[1], 1, $"topbar-to-title spacing must match on {route} at {viewport.Width}px");
                rhythm[2].Should().BeApproximately(reference[2], 1, $"intro-block-to-first-field spacing must match on {route} at {viewport.Width}px");
                rhythm[3].Should().BeApproximately(reference[3], 1, $"last-field-to-button spacing must match on {route} at {viewport.Width}px");
                rhythm[4].Should().BeApproximately(reference[4], 1, $"button-to-secondary-action spacing must match on {route} at {viewport.Width}px");
            }
        }
    }
}
