using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AdminPermissionManagementTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PermissionAdministration_UsesCanonicalListDetailAndAdaptiveBatchEditor()
    {
        var consoleErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            if (message.Type == "error") consoleErrors.Add(message.Text);
        };

        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await GotoPermissionAsync();

        var groupSurface = Page.Locator("[data-testid='permission-groups-data-surface']");
        await groupSurface.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-group-grid tbody tr').length > 0");
        await Page.Locator(".vpp-permission-detail-shell").WaitForAsync();
        (await Page.Locator(".permission-group-grid .rz-switch").CountAsync()).Should().Be(0);
        await AssertNoDocumentOverflowAsync("desktop permission list-detail");
        await CaptureIfRequestedAsync("aa6-permissions-1366x768.png");

        var configureButton = Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Cấu hình quyền UI",
            Exact = true
        });
        await configureButton.WaitForAsync();
        await configureButton.ClickAsync();

        var editor = Page.Locator("[data-testid='permission-ui-batch-editor']");
        await editor.WaitForAsync();
        (await editor.Locator(".vpp-permission-access-select").CountAsync()).Should().BeGreaterThan(0);
        var dialogRect = await editor.BoundingBoxAsync();
        dialogRect.Should().NotBeNull();
        dialogRect!.Width.Should().BeLessThan(1366);
        dialogRect.Height.Should().BeLessThan(768);
        await CaptureIfRequestedAsync("aa6-permission-editor-1366x768.png");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).Last.ClickAsync();
        await editor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        await Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Quyền API (tham chiếu)",
            Exact = true
        }).ClickAsync();
        await Page.Locator(".vpp-permission-matrix").WaitForAsync();
        (await Page.Locator(".vpp-permission-matrix tbody tr").CountAsync()).Should().Be(18);
        await CaptureIfRequestedAsync("aa6-api-reference-1366x768.png");

        consoleErrors.Should().BeEmpty();
    }

    [Fact]
    public async Task PermissionAdministration_FitsMobileAndKeepsBothPanesUsable()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await LoginAsDefaultUserAsync();
        await GotoPermissionAsync();

        await Page.Locator("[data-testid='permission-groups-data-surface']").WaitForAsync();
        await Page.WaitForFunctionAsync(
            """
            () => {
                const grid = document.querySelector('.permission-group-grid');
                const summary = document.querySelector('.vpp-permission-group-summary');
                return grid
                    && !grid.classList.contains('rz-datatable-loading')
                    && document.querySelectorAll('.permission-group-grid tbody tr').length > 0
                    && summary
                    && !summary.textContent.trim().startsWith('0 ');
            }
            """);
        try
        {
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const detail = document.querySelector('.vpp-permission-detail-shell');
                    return detail
                        && detail.getAttribute('aria-busy') !== 'true'
                        && document.querySelectorAll('.permission-component-grid tbody tr').length > 0;
                }
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 15_000 });
        }
        catch (TimeoutException exception)
        {
            await CaptureIfRequestedAsync("aa6-permissions-mobile-stalled-390x844.png");
            var state = await Page.EvaluateAsync<string>(
                """
                () => JSON.stringify({
                    busy: document.querySelector('.vpp-permission-detail-shell')?.getAttribute('aria-busy'),
                    componentRows: document.querySelectorAll('.permission-component-grid tbody tr').length,
                    skeletons: document.querySelectorAll('.vpp-permission-detail-shell .rz-skeleton').length,
                    alerts: Array.from(document.querySelectorAll('.rz-notification, .rz-alert')).map(node => node.textContent.trim()),
                    text: document.querySelector('.vpp-permission-group-detail')?.innerText.slice(0, 1000)
                })
                """);
            throw new InvalidOperationException($"Mobile permission detail did not settle: {state}", exception);
        }
        await AssertNoDocumentOverflowAsync("mobile permission list-detail");
        await CaptureIfRequestedAsync("aa6-permissions-mobile-390x844.png");
    }

    private async Task GotoPermissionAsync()
    {
        await Page.GotoAsync($"{BaseUrl}permission?tab=1", new PageGotoOptions
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
