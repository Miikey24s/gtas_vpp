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

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
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
