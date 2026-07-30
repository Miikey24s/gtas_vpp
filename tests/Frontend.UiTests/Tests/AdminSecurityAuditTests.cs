using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AdminSecurityAuditTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task SecurityAudit_IsReadOnlyFilterableAndResponsive()
    {
        var consoleErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            var expectedNavigationAbort = message.Text.Contains(
                    "Failed to complete negotiation with the server",
                    StringComparison.OrdinalIgnoreCase)
                && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
            if (message.Type == "error" && !expectedNavigationAbort) consoleErrors.Add(message.Text);
        };

        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await GotoAuditAsync();

        var surface = Page.Locator("[data-testid='security-audit-data-surface']");
        await surface.WaitForAsync();
        await WaitForAuditRowsAsync();
        (await Page.Locator(".security-audit-grid .rz-switch").CountAsync()).Should().Be(0);
        (await surface.Locator("button[title='Thêm'], button[title='Sửa'], button[title='Xóa']").CountAsync())
            .Should().Be(0, "security audit is immutable from the UI");
        await AssertNoDocumentOverflowAsync("desktop audit route");
        await CaptureIfRequestedAsync("aa7-security-audit-1366x768.png");

        var actionFilter = Page.Locator(".vpp-security-audit-filters .vpp-filter-select-trigger").First;
        await actionFilter.ClickAsync();
        var actionPopover = Page.Locator(".vpp-filter-select-popover:popover-open");
        await actionPopover.WaitForAsync();
        (await actionPopover.GetByRole(AriaRole.Option).CountAsync()).Should().BeGreaterThan(1);
        await CaptureIfRequestedAsync("aa7-security-audit-filter-1366x768.png");
        await Page.Keyboard.PressAsync("Escape");

        var detailButton = Page.Locator(".security-audit-grid tbody tr button").First;
        await detailButton.ClickAsync();
        var detail = Page.Locator("[data-testid='security-audit-detail']");
        await detail.WaitForAsync();
        (await detail.Locator("dt").CountAsync()).Should().Be(6);
        await CaptureIfRequestedAsync("aa7-security-audit-detail-1366x768.png");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Đóng", Exact = true }).Last.ClickAsync();
        await detail.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        consoleErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task SecurityAudit_FitsMobileWithoutDocumentOverflow()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await LoginAsDefaultUserAsync();
        await GotoAuditAsync();
        await Page.Locator("[data-testid='security-audit-data-surface']").WaitForAsync();
        await WaitForAuditRowsAsync();
        await AssertNoDocumentOverflowAsync("mobile audit route");
        await CaptureIfRequestedAsync("aa7-security-audit-mobile-390x844.png");
    }

    [Fact]
    public async Task PermissionAdministration_UsesEnglishResourcesWhenSelected()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync(
            $"{BaseUrl}set-language?culture=en&returnUrl=%2Fpermission%3Ftab%3D0",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.GetByRole(AriaRole.Button, new() { Name = "Add user", Exact = true }).WaitForAsync();
        (await Page.GetByText("Thêm người dùng", new PageGetByTextOptions { Exact = true }).CountAsync()).Should().Be(0);

        await Page.GotoAsync($"{BaseUrl}permission?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        var surface = Page.Locator("[data-testid='security-audit-data-surface']");
        await surface.WaitForAsync();
        var activeTab = Page.Locator(
            ".vpp-admin-tabs button[role='tab'][aria-selected='true'] .rz-tabview-title");
        await activeTab.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        (await activeTab.InnerTextAsync()).Should().Be("Security audit");
        await Page.GetByPlaceholder("Search users, actions, resources, or reasons").WaitForAsync();
        await WaitForAuditRowsAsync();
        await Page.GetByLabel("Section navigation")
            .GetByRole(AriaRole.Link, new() { Name = "Security audit", Exact = true })
            .WaitForAsync();
        await AssertNoDocumentOverflowAsync("English permission administration");
        await CaptureIfRequestedAsync("aa7-security-audit-en-1366x768.png");
    }

    private async Task GotoAuditAsync()
    {
        await Page.GotoAsync($"{BaseUrl}permission?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("#main-content").WaitForAsync();
    }

    private async Task AssertNoDocumentOverflowAsync(string because)
    {
        (await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"))
            .Should().BeFalse(because);
    }

    private Task WaitForAuditRowsAsync() => Page.WaitForFunctionAsync(
        """
        () => {
            const grid = document.querySelector('.security-audit-grid');
            return grid
                && !grid.classList.contains('rz-datatable-loading')
                && document.querySelectorAll('.security-audit-grid tbody tr.rz-data-row').length > 0;
        }
        """);

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;

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
