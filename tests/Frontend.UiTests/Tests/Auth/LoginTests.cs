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
        public async Task LoginVisual_UsesSharedPasswordFieldAndFlatPrimaryAction(
            int width,
            int height)
        {
            await Page.SetViewportSizeAsync(width, height);
            var loginPage = new LoginPage(Page);
            await loginPage.GotoAsync(BaseUrl);

            var toggle = Page.Locator(".vpp-password-toggle-btn");
            var primaryAction = Page.Locator(".vpp-login-btn");
            var username = Page.Locator("input[name='Username']");
            var languageSwitch = Page.Locator(".vpp-account-language-switch");
            await toggle.WaitForAsync();
            await primaryAction.WaitForAsync();

            await username.FocusAsync();
            var focusChrome = await username.EvaluateAsync<string>("""
                input => {
                    const style = getComputedStyle(input);
                    return `${style.outlineStyle}|${style.boxShadow}|${style.borderTopWidth}|${style.borderTopColor}`;
                }
                """);
            focusChrome.Should().StartWith(
                "none|none|1px|",
                "an account field must expose one border-owned focus indicator without a stacked outline or halo");

            var languageOverflow = await languageSwitch.EvaluateAsync<string>("""
                element => {
                    const style = getComputedStyle(element);
                    const button = element.querySelector('button');
                    const buttonStyle = button ? getComputedStyle(button) : null;
                    const buttonRect = button?.getBoundingClientRect();
                    return `${style.overflowY}|${element.scrollHeight}|${element.clientHeight}`
                        + `|buttonHeight=${buttonRect?.height}|buttonMinHeight=${buttonStyle?.minHeight}`
                        + `|padding=${style.paddingTop},${style.paddingBottom}`;
                }
                """);
            var languageMetrics = languageOverflow.Split('|');
            languageMetrics[0].Should().Be("hidden", "the VI/EN segmented selector must never own vertical scrolling");
            int.Parse(languageMetrics[1]).Should().BeLessThanOrEqualTo(
                int.Parse(languageMetrics[2]) + 1,
                $"the VI/EN selector content must fit its compact height; metrics={languageOverflow}");

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
                "the password eye must remain compact and centered inside the shared field");

            var idleBackground = await primaryAction.EvaluateAsync<string>(
                "button => getComputedStyle(button).backgroundColor");
            await primaryAction.HoverAsync();
            await Page.WaitForTimeoutAsync(180);
            var hoverChrome = await primaryAction.EvaluateAsync<string>("""
                button => {
                    const style = getComputedStyle(button);
                    return `${style.transform === 'none' && style.boxShadow === 'none'}`
                        + `|background=${style.backgroundColor}`
                        + `|transform=${style.transform}|shadow=${style.boxShadow}`;
                }
                """);
            hoverChrome.Should().StartWith(
                "true",
                "the account CTA must use the same flat button bridge as the rest of the design system");
            hoverChrome.Should().NotContain(
                $"background={idleBackground}",
                "the flat CTA still needs a clear hover color response");

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

            await primaryAction.ClickAsync();
            await Page.GetByText("Vui lòng nhập tên đăng nhập.", new() { Exact = true }).WaitForAsync();
            await username.FocusAsync();
            var invalidFocusChrome = await Page.EvaluateAsync<string>("""
                () => {
                    const username = document.querySelector("input[name='Username']");
                    const password = document.querySelector('.vpp-login-password-wrapper');
                    if (!username || !password) return 'missing';
                    const usernameStyle = getComputedStyle(username);
                    const passwordStyle = getComputedStyle(password);
                    return `${usernameStyle.outlineStyle}|${usernameStyle.boxShadow}`
                        + `|${usernameStyle.borderTopWidth}|${usernameStyle.borderTopColor}`
                        + `|${passwordStyle.borderTopColor}`;
                }
                """);
            var invalidFocusMetrics = invalidFocusChrome.Split('|');
            invalidFocusMetrics[0].Should().Be("none");
            invalidFocusMetrics[1].Should().Be("none");
            invalidFocusMetrics[2].Should().Be(
                "1px",
                $"validation and focus must still resolve to one owned border; chrome={invalidFocusChrome}");

            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(evidenceDirectory, $"login-validation-focus-{width}x{height}.png"),
                    FullPage = false,
                    Animations = ScreenshotAnimations.Disabled,
                    Caret = ScreenshotCaret.Hide,
                    Scale = ScreenshotScale.Css
                });
            }
        }
    }
}
