using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class UiSelectorAndHistoryNavigationTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Selectors_StayEqualAndHistoryActionDeepLinksToSelectedDetail()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0&orderView=current");

        var orderSelector = Page.Locator(".vpp-orders-view-selector");
        await orderSelector.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var selectorWidths = await orderSelector.Locator(":scope > button").EvaluateAllAsync<double[]>(
            "buttons => buttons.map(button => button.getBoundingClientRect().width)");
        (selectorWidths.Max() - selectorWidths.Min()).Should().BeLessThanOrEqualTo(1,
            "all horizontal selector items must use the width of the longest label");

        var inactiveColor = await orderSelector.Locator(":scope > button:not(.is-active)").First.EvaluateAsync<string>(
            "button => getComputedStyle(button).color");
        inactiveColor.Should().NotBe("rgb(255, 255, 255)",
            "inactive selector text must remain readable even before the moving indicator synchronizes");

        var sourceCode = (await Page.Locator(".vpp-order-code-static").InnerTextAsync()).Trim();
        sourceCode.Should().NotBeNullOrWhiteSpace();
        var historyAction = Page.Locator(".vpp-order-view-meta button")
            .Filter(new() { HasText = "Lịch sử" })
            .First;
        await historyAction.ClickAsync();
        await Page.WaitForFunctionAsync(
            "() => new URLSearchParams(location.search).get('tab') === '1' && !!new URLSearchParams(location.search).get('orderId')");

        var detailCode = Page.Locator(".vpp-history-drawer-code h2");
        await detailCode.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await detailCode.InnerTextAsync()).Trim().Should().Be(sourceCode,
            "the History action must select and open the exact source order");

        var selectedRows = Page.Locator(
            ".vpp-history-grid tbody tr.rz-state-highlight, .vpp-history-grid tbody tr[aria-selected='true']");
        await selectedRows.First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15_000
        });
        (await selectedRows.CountAsync()).Should().BeGreaterThan(0,
            "the deep-linked order must be visibly selected in the order list");
        var drawerClose = Page.Locator(".vpp-history-drawer-close");
        (await drawerClose.IsVisibleAsync()).Should().BeTrue(
            "drawer chi tiết phải luôn có nút đóng rõ ràng theo dialog/drawer contract");
        await drawerClose.ClickAsync();
        await Assertions.Expect(Page.Locator(".vpp-history-drawer.is-open")).ToHaveCountAsync(0);

        var historySelector = Page.Locator(".vpp-history-scope-selector");
        var historyWidths = await historySelector.Locator(":scope > button").EvaluateAllAsync<double[]>(
            "buttons => buttons.map(button => button.getBoundingClientRect().width)");
        (historyWidths.Max() - historyWidths.Min()).Should().BeLessThanOrEqualTo(1);

        await historySelector.Locator(":scope > button").Last.ClickAsync();
        var periodPopover = Page.Locator(".vpp-period-picker-popover");
        await periodPopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await WaitForRenderSettleAsync();
        (await periodPopover.GetByRole(AriaRole.Button, new() { Name = "Áp dụng", Exact = true }).CountAsync())
            .Should().Be(1);
        var periodValues = await periodPopover.Locator(".vpp-decision-select-label").AllTextContentsAsync();
        periodValues.Should().HaveCount(4);
        periodValues.Should().OnlyContain(value => !string.Equals(value.Trim(), "Năm", StringComparison.OrdinalIgnoreCase),
            "picker phải giữ hiển thị năm đang chọn kể cả khi năm đó nằm ngoài biên dữ liệu tổng hợp mới nhất");
        var periodPopoverGeometry = await periodPopover.EvaluateAsync<double[]>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                const center = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2);
                return [
                    rect.width,
                    rect.height,
                    center && (center === element || element.contains(center)) ? 1 : 0
                ];
            }
            """);
        periodPopoverGeometry[0].Should().BeGreaterThan(300);
        periodPopoverGeometry[1].Should().BeGreaterThan(120);
        periodPopoverGeometry[2].Should().Be(1,
            "the period picker must render above KPI and chart surfaces instead of being visually covered");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "order-history-deep-link-and-period-picker.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await Page.Locator("#history-orders-title").ClickAsync();
        await periodPopover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        await historySelector.Locator(":scope > button").Last.ClickAsync();
        await periodPopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.Keyboard.PressAsync("Escape");
        await periodPopover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }
}
