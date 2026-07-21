using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class UserMenuVisualTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task AccountMenu_ShowsDepartmentOnceAndKeepsLogoutNeutralUntilInteraction()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();

        (await Page.Locator(".vpp-brand-mark svg").CountAsync()).Should().Be(1, "the authenticated shell should use the shared vector brand mark");

        var notificationButton = Page.Locator(".vpp-header-notification-button");
        var themeButton = Page.Locator(".vpp-theme-switch");
        await notificationButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await themeButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var controlGeometry = await Page.EvaluateAsync<double[]>("""
            () => {
                const notification = document.querySelector('.vpp-header-notification-button')?.getBoundingClientRect();
                const theme = document.querySelector('.vpp-theme-switch')?.getBoundingClientRect();
                if (!notification || !theme) {
                    throw new Error('Unable to measure header icon controls.');
                }

                return [
                    notification.width,
                    notification.height,
                    theme.width,
                    theme.height,
                    Math.abs((notification.top + notification.height / 2) - (theme.top + theme.height / 2))
                ];
            }
            """);
        controlGeometry[0].Should().BeApproximately(controlGeometry[2], 1, "notification and theme controls should share one width");
        controlGeometry[1].Should().BeApproximately(controlGeometry[3], 1, "notification and theme controls should share one height");
        controlGeometry[4].Should().BeLessThan(1, "notification and theme controls should share one centerline");

        var notificationBaseStyle = await ReadHeaderControlStyleAsync(notificationButton);
        var themeBaseStyle = await ReadHeaderControlStyleAsync(themeButton);
        notificationBaseStyle.Should().Equal(themeBaseStyle, "notification and theme controls should share border, surface, icon color, radius and shadow tokens");

        await notificationButton.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        var notificationHoverStyle = await ReadHeaderControlStyleAsync(notificationButton);
        await themeButton.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        var themeHoverStyle = await ReadHeaderControlStyleAsync(themeButton);
        notificationHoverStyle.Should().Equal(themeHoverStyle, "notification and theme controls should share hover treatment");

        await themeButton.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.documentElement.classList.contains('rz-theme-dark')");
        await Page.WaitForFunctionAsync("() => !document.querySelector('.vpp-theme-switch')?.classList.contains('is-switching')");
        await Page.Mouse.MoveAsync(4, 4);
        await Page.WaitForFunctionAsync("""
            () => {
                const notificationIcon = document.querySelector('.vpp-header-notification-button .vpp-icon');
                const themeIcon = [...document.querySelectorAll('.vpp-theme-switch .vpp-icon')]
                    .find(candidate => Number.parseFloat(getComputedStyle(candidate).opacity || '1') > 0.5);
                return notificationIcon
                    && themeIcon
                    && getComputedStyle(notificationIcon).color === getComputedStyle(themeIcon).color;
            }
            """);
        var notificationDarkStyle = await ReadHeaderControlStyleAsync(notificationButton);
        var themeDarkStyle = await ReadHeaderControlStyleAsync(themeButton);
        notificationDarkStyle.Should().Equal(themeDarkStyle, "notification and theme controls should share the dark-mode surface treatment");

        await themeButton.ClickAsync();
        await Page.WaitForFunctionAsync("() => !document.documentElement.classList.contains('rz-theme-dark')");

        await notificationButton.ClickAsync();
        var notificationPanel = Page.Locator("#vpp-notification-panel");
        await notificationPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.WaitForFunctionAsync("""
            () => document.querySelector('.vpp-notification-empty .vpp-icon')
                || document.querySelector('.vpp-notification-item')
                || document.querySelector('.vpp-notification-inline-status .vpp-icon')
            """);
        (await notificationPanel.Locator(".rzi").CountAsync()).Should().Be(0, "the notification panel should not rely on missing Radzen icon glyphs");
        (await notificationPanel.Locator(".vpp-icon").CountAsync()).Should().BeGreaterThan(1, "notification states and actions should use visible VppIcon glyphs");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "account-notification-panel-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await notificationButton.ClickAsync();

        var trigger = Page.Locator(".user-menu-trigger");
        await trigger.ClickAsync();

        var dropdown = Page.Locator("#user-menu-dropdown");
        await dropdown.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        (await dropdown.GetAttributeAsync("role")).Should().Be("dialog", "the account summary contains static context plus one session action");
        (await Page.Locator(".user-dropdown-login strong").InnerTextAsync()).Trim().Should().NotBeNullOrWhiteSpace();

        var department = Page.Locator(".user-dropdown-context");
        (await department.CountAsync()).Should().Be(1, "the department should appear in one contextual information block");
        var departmentName = (await department.Locator(".user-dropdown-context-value").InnerTextAsync()).Trim();
        departmentName.Should().NotBeNullOrWhiteSpace();
        var departmentCode = (await department.Locator(".user-dropdown-code").InnerTextAsync()).Trim();
        departmentCode.Should().NotBeNullOrWhiteSpace();
        (await Page.Locator(".user-dropdown-header").InnerTextAsync()).Should().NotContain(departmentName);

        var logout = Page.Locator(".user-dropdown-logout");
        var appearance = await logout.EvaluateAsync<string[]>("""
            element => {
                const style = getComputedStyle(element);
                return [style.backgroundColor, style.color];
            }
            """);
        appearance[0].Should().Be("rgba(0, 0, 0, 0)", "logout should be visually neutral before hover or focus");
        appearance[1].Should().NotBe("rgb(255, 255, 255)", "logout must remain readable on the white menu surface");

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "account-user-menu-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await trigger.FocusAsync();
        await trigger.PressAsync("Escape");
        await dropdown.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached
        });
    }

    private static Task<string[]> ReadHeaderControlStyleAsync(ILocator control)
        => control.EvaluateAsync<string[]>("""
            element => {
                const style = getComputedStyle(element);
                const icon = [...element.querySelectorAll('.vpp-icon')]
                    .find(candidate => Number.parseFloat(getComputedStyle(candidate).opacity || '1') > 0.5);
                return [
                    style.borderTopWidth,
                    style.borderTopColor,
                    style.borderRadius,
                    style.backgroundColor,
                    style.boxShadow,
                    style.transform,
                    icon ? getComputedStyle(icon).color : ''
                ];
            }
            """);
}
