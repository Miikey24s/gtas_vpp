using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AdminPermissionManagementTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PermissionAdministration_UsesFullWidthGroupTableAndAdaptiveBatchEditor()
    {
        var consoleErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            var expectedNavigationAbort = message.Text.Contains(
                "Failed to complete negotiation with the server",
                StringComparison.OrdinalIgnoreCase);
            if (message.Type == "error" && !expectedNavigationAbort) consoleErrors.Add(message.Text);
        };

        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await GotoPermissionAsync();

        var groupSurface = Page.Locator("[data-testid='permission-groups-data-surface']");
        await groupSurface.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.permission-group-grid .permission-group-identity').length > 0");
        (await Page.Locator(".vpp-permission-detail-shell").CountAsync()).Should().Be(0);
        (await Page.Locator(".permission-group-grid .rz-switch").CountAsync()).Should().Be(0);
        await AssertNoDocumentOverflowAsync("desktop permission group table");
        await CaptureIfRequestedAsync("aa6-permissions-1366x768.png");

        var configureButton = Page.GetByRole(AriaRole.Button, new()
        {
            Name = "Cấu hình quyền UI",
            Exact = true
        }).First;
        await configureButton.WaitForAsync();
        await configureButton.ClickAsync();

        var editor = Page.Locator("[data-testid='permission-ui-batch-editor']");
        await editor.WaitForAsync();
        await editor.GetByText(
                "Chỉ trạng thái hiển thị UI có thể sửa; các quyền API, mục bắt buộc và mục ngoài phạm vi vai trò được khóa theo RBAC chuẩn.",
                new() { Exact = true })
            .WaitForAsync();
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
    public async Task PermissionAdministration_FitsMobileAndKeepsGroupTableUsable()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await LoginAsDefaultUserAsync();
        await GotoPermissionAsync();

        await Page.Locator("[data-testid='permission-groups-data-surface']").WaitForAsync();
        await Page.WaitForFunctionAsync(
            """
            () => {
                const grid = document.querySelector('.permission-group-grid');
                return grid
                    && !grid.classList.contains('rz-datatable-loading')
                    && document.querySelectorAll('.permission-group-grid .permission-group-identity').length > 0;
            }
            """);
        (await Page.GetByRole(AriaRole.Button, new() { Name = "Cấu hình quyền UI", Exact = true }).CountAsync())
            .Should().BeGreaterThan(0);
        await AssertNoDocumentOverflowAsync("mobile permission group table");
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
