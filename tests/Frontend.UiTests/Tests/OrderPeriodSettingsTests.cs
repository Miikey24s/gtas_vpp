using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class OrderPeriodSettingsTests : TestBase, IAuthenticatedUiTest
{
    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public async Task SystemAdmin_CanManageVersionedOrderingDefaults(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}permission?tab=3", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var workspace = Page.Locator(".vpp-order-period-settings-workspace");
        await workspace.WaitForAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Cấu hình đặt hàng", Exact = true })
            .WaitForAsync();

        (await workspace.Locator(".vpp-order-period-settings-form-grid").CountAsync()).Should().Be(1);
        (await workspace.Locator(".rz-numeric").CountAsync()).Should().BeGreaterThanOrEqualTo(7);
        (await workspace.GetByText("Chỉ áp dụng cho kỳ được tạo sau này", new() { Exact = true }).CountAsync())
            .Should().Be(1);
        (await workspace.GetByRole(AriaRole.Button, new() { Name = "Lưu phiên bản mới", Exact = true }).CountAsync())
            .Should().Be(1);
        (await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1"))
            .Should().BeFalse($"the settings route must remain bounded at {width}x{height}");

        if (width == 390)
        {
            var layoutBody = Page.Locator(".vpp-layout-body");
            var canScroll = await layoutBody.EvaluateAsync<bool>(
                "element => element.scrollHeight > element.clientHeight + 2 && ['auto', 'scroll'].includes(getComputedStyle(element).overflowY)");
            canScroll.Should().BeTrue("mobile settings form must own a vertical scroll range");
            var saveButton = workspace.GetByRole(AriaRole.Button, new() { Name = "Lưu phiên bản mới", Exact = true });
            await saveButton.ScrollIntoViewIfNeededAsync();
            await Assertions.Expect(saveButton).ToBeVisibleAsync();
        }

        var directory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            await WaitForRenderSettleAsync();
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, $"order-period-settings-{width}x{height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }
    }
}
