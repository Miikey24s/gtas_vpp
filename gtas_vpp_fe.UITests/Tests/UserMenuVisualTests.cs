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

        var department = Page.Locator(".user-dropdown-info > span:not(.vpp-icon)");
        (await department.CountAsync()).Should().Be(1, "the department should appear in one dedicated information row");
        var departmentText = (await department.InnerTextAsync()).Trim();
        departmentText.Should().NotBeNullOrWhiteSpace();
        (await Page.Locator(".user-dropdown-header").InnerTextAsync()).Should().NotContain(departmentText);

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
}
