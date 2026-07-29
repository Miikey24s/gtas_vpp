using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AdminUserManagementTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task UserAdministration_UsesFullWidthCollectionAndBoundedResponsiveLayout()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}permission?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var surface = Page.Locator("[data-testid='permission-users-data-surface']");
        await surface.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-user-grid tbody tr').length > 1");

        (await Page.Locator(".vpp-record-inspector").CountAsync()).Should().Be(0);
        (await Page.Locator(".permission-user-grid tbody .rz-dropdown").CountAsync()).Should().Be(0);
        (await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"))
            .Should().BeFalse("the grid owns horizontal overflow instead of the document");

        var invitationButton = Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Thêm người dùng",
            Exact = true
        });
        await invitationButton.WaitForAsync();
        if (!await invitationButton.IsEnabledAsync())
        {
            (await invitationButton.GetAttributeAsync("title")).Should().NotBeNullOrWhiteSpace();
        }

        await CaptureIfRequestedAsync("aa5-users-1366x768.png");

        var membershipButton = Page.Locator(
            ".permission-user-grid .vpp-admin-actions > button:first-child:not([disabled])").First;
        await membershipButton.WaitForAsync();
        await membershipButton.ClickAsync();
        var editor = Page.Locator("[data-testid='user-membership-editor']");
        await editor.WaitForAsync();
        var dialogRect = await editor.BoundingBoxAsync();
        dialogRect.Should().NotBeNull();
        dialogRect!.Width.Should().BeLessThan(1366);
        dialogRect.Height.Should().BeLessThan(768);
        await CaptureIfRequestedAsync("aa5-users-membership-1366x768.png");
        await Page.Keyboard.PressAsync("Escape");
        await editor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync($"{BaseUrl}permission?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await surface.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-user-grid tbody tr').length > 1");
        await Page.WaitForFunctionAsync(
            "() => !document.querySelector('.permission-user-grid')?.classList.contains('rz-datatable-loading')");
        (await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"))
            .Should().BeFalse("the user administration route must remain bounded on mobile");
        await CaptureIfRequestedAsync("aa5-users-mobile-390x844.png");
    }

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        await WaitForRenderSettleAsync();
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
