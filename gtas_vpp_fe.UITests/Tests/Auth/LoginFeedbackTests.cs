using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests.Auth;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class LoginFeedbackTests : TestBase
{
    [Fact]
    public async Task InvalidLogin_ShowsBoundedLocalizedFeedbackAtThreeViewports()
    {
        var browserErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                var expectedNavigationAbort = message.Text.Contains(
                        "Failed to complete negotiation with the server",
                        StringComparison.OrdinalIgnoreCase)
                    && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
                if (expectedNavigationAbort)
                {
                    return;
                }

                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync(
                $"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            var loginPage = new LoginPage(Page);
            await loginPage.GotoAsync(BaseUrl);
            await loginPage.LoginAsync("invalid-login-user", "Invalid-Pass1!");

            var feedback = Page.Locator(".vpp-login-password-wrapper + .vpp-validation-slot [role=alert]");
            await feedback.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });

            var text = await feedback.InnerTextAsync();
            text.Should().Contain("Tên đăng nhập hoặc mật khẩu không hợp lệ.");
            text.Should().NotContain("{\"message\"");
            text.Should().NotContain("Login failed");

            var bounds = await feedback.BoundingBoxAsync();
            bounds.Should().NotBeNull();
            var passwordBounds = await Page.Locator(".vpp-login-password-wrapper").BoundingBoxAsync();
            passwordBounds.Should().NotBeNull();
            bounds!.X.Should().BeApproximately(passwordBounds!.X, 1, "credential feedback should align with the password field");
            bounds.Width.Should().BeLessThanOrEqualTo(passwordBounds.Width + 1);
            (await Page.Locator(".rz-notification:visible").CountAsync()).Should().Be(0);

            var overflows = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > window.innerWidth + 1");
            overflows.Should().BeFalse();

            var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Directory.CreateDirectory(evidenceDirectory);
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(
                        evidenceDirectory,
                        $"ui-login-feedback-{viewport.Width}x{viewport.Height}.png"),
                    FullPage = true
                });
            }
        }

        browserErrors.Should().BeEmpty("invalid login feedback should not break the Blazor circuit");
    }
}
