using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class GlobalRenderFlowTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task NotFoundAction_IsInteractiveAndReturnsAuthenticatedUserToDashboard()
    {
        await LoginAsDefaultUserAsync();

        var browserErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);

        var notificationButton = Page.Locator(".vpp-header-notification-button");
        await notificationButton.ClickAsync();
        await Page.Locator("#vpp-notification-panel").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await notificationButton.ClickAsync();

        await Page.GotoAsync(
            $"{BaseUrl}not-found",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Về bảng điều khiển",
            Exact = true
        }).ClickAsync();

        await Page.WaitForURLAsync(
            new System.Text.RegularExpressions.Regex(".*/dashboard.*", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
            new PageWaitForURLOptions { Timeout = 30_000 });
        await Page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        browserErrors.Should().BeEmpty("global Interactive Server navigation should keep the circuit healthy");
    }
}
