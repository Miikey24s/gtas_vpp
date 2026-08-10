using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Auth
{
    [Collection(ReadOnlyE2ECollection.Name)]
    public class LoginTests : TestBase, IAuthenticatedUiTest
    {
        [Fact]
        public async Task Login_Voi_Tai_Khoan_Hop_Le_Thanh_Cong()
        {
            // Arrange
            var loginPage = new LoginPage(Page);
            await loginPage.GotoAsync(BaseUrl);

            // Act
            await loginPage.LoginAsync(TestUsername, TestPassword);
            await loginPage.WaitForDashboardAsync();

            // Assert
            Page.Url.Should().NotContain("Login");
        }

        [Fact]
        public async Task StaleSessionLogout_DoesNotRemoveANewerSuccessfulLogin()
        {
            await LoginAsDefaultUserAsync();
            var staleFingerprint = new string('A', 64);

            await Page.GotoAsync(
                $"{BaseUrl}logoutprocess?expectedSession={staleFingerprint}",
                new() { WaitUntil = Microsoft.Playwright.WaitUntilState.DOMContentLoaded });

            await Page.WaitForURLAsync("**/dashboard**");
            await Page.Locator("#main-content").WaitForAsync();
            Page.Url.Should().NotContain("Account/Login");

            await Page.GotoAsync(
                $"{BaseUrl}perform-logout?expectedSession={staleFingerprint}",
                new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Page.WaitForURLAsync("**/dashboard**");
            await Page.Locator("#main-content").WaitForAsync();
            Page.Url.Should().NotContain("Account/Login");
        }

        [Theory]
        [InlineData(390, 844)]
        [InlineData(1366, 768)]
        public async Task LoginVisual_PasswordEyeClearsUnderlineAndPrimaryHoverRemainsActive(
            int width,
            int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            var loginPage = new LoginPage(Page);
            await loginPage.GotoAsync(BaseUrl);

            var toggle = Page.Locator(".vpp-password-toggle-btn");
            var primaryAction = Page.Locator(".vpp-login-btn");
            await toggle.WaitForAsync();
            await primaryAction.WaitForAsync();

            var geometry = await Page.EvaluateAsync<string>("""
                () => {
                    const wrapper = document.querySelector('.vpp-login-password-wrapper');
                    const toggle = document.querySelector('.vpp-password-toggle-btn');
                    const icon = toggle?.querySelector('.vpp-icon');
                    if (!wrapper || !toggle || !icon) return 'missing';
                    const wrapperRect = wrapper.getBoundingClientRect();
                    const toggleRect = toggle.getBoundingClientRect();
                    const iconRect = icon.getBoundingClientRect();
                    const iconSize = parseFloat(getComputedStyle(icon).fontSize);
                    const ok = toggleRect.bottom <= wrapperRect.bottom - 4
                        && iconRect.bottom <= wrapperRect.bottom - 6
                        && Math.abs(toggleRect.width - 28) <= 1
                        && Math.abs(toggleRect.height - 28) <= 1
                        && Math.abs(iconSize - 16) <= 1;
                    return `${ok}|wrapperBottom=${wrapperRect.bottom}`
                        + `|toggle=${toggleRect.top}-${toggleRect.bottom}`
                        + `|iconBottom=${iconRect.bottom}|icon=${iconSize}`;
                }
                """);
            geometry.Should().StartWith(
                "true",
                "the password eye must remain compact and visibly clear of the underline");

            await primaryAction.HoverAsync();
            await Page.WaitForFunctionAsync("""
                () => {
                    const button = document.querySelector('.vpp-login-btn');
                    return button && getComputedStyle(button).transform !== 'none';
                }
                """);
            var hoverChrome = await primaryAction.EvaluateAsync<string>("""
                button => {
                    const style = getComputedStyle(button);
                    return `${style.transform !== 'none' && style.boxShadow !== 'none'}`
                        + `|transform=${style.transform}|shadow=${style.boxShadow}`;
                }
                """);
            hoverChrome.Should().StartWith(
                "true",
                "the account CTA has an intentional hover response outside the flat action-button bridge");

            var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Directory.CreateDirectory(evidenceDirectory);
                await Page.Mouse.MoveAsync(0, 0);
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(evidenceDirectory, $"login-password-eye-{width}x{height}.png"),
                    FullPage = false,
                    Animations = ScreenshotAnimations.Disabled,
                    Caret = ScreenshotCaret.Hide,
                    Scale = ScreenshotScale.Css
                });
            }
        }
    }
}
