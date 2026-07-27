using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class GlobalRenderFlowTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task NotFoundAction_IsInteractiveAndReturnsAuthenticatedUserToDashboard()
    {
        await LoginAsDefaultUserAsync();

        var browserErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            // Full-page navigations in this flow (login redirect, /not-found) abort
            // in-flight resource loads and Chromium logs them as console errors even
            // though the circuit is healthy. Only ERR_ABORTED noise is expected —
            // every other console error still fails the test (same policy as the
            // RequestFailed allowlists in AccountShellSmokeTests).
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase)
                && !message.Text.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase))
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);

        // Hàng Thông báo nằm trong popup tài khoản (Atlas "Menu tài khoản") —
        // phải mở menu từ footer sidebar trước khi bấm chuông.
        var userMenuTrigger = Page.Locator(".user-menu-trigger");
        await userMenuTrigger.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await userMenuTrigger.ClickAsync();
        var notificationButton = Page.Locator(".vpp-header-notification-button");
        await notificationButton.ClickAsync();
        await Page.Locator("#vpp-notification-panel").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await notificationButton.ClickAsync();
        await userMenuTrigger.PressAsync("Escape");

        await Page.GotoAsync(
            $"{BaseUrl}not-found",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // /not-found is a full page load: the prerendered button is visible before
        // the fresh circuit becomes interactive, so a raw click can land on dead
        // HTML. GetInteractiveButtonAsync waits for the Blazor binding marker.
        var dashboardButton = await GetInteractiveButtonAsync(
            Page.Locator("body"),
            "Về bảng điều khiển");
        await dashboardButton.ClickAsync();

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
