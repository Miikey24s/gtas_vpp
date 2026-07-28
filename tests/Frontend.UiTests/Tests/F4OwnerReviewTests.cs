using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class F4OwnerReviewTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task CatalogUsersAndOrderCreate_UseSharedFiltersWithoutDocumentScroll()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsDefaultUserAsync();

        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2");
        await WaitForRowsAsync(".vpp-catalog-grid");
        await AssertBoundedPageAndSharedFilterAsync("catalog", ".vpp-catalog-workspace");
        await CaptureAsync("f4-review-catalog-1920x1080.png");

        await Page.GotoAsync($"{BaseUrl}permission?tab=0");
        await WaitForRowsAsync(".permission-user-grid");
        await Page.Locator(".vpp-permission-user-header").WaitForAsync();
        (await Page.GetByRole(AriaRole.Button, new() { Name = "Tải lại" }).CountAsync()).Should().Be(0);
        var columnPicker = Page.Locator(".vpp-column-picker-trigger");
        await columnPicker.WaitForAsync();
        await columnPicker.ClickAsync();
        var columnPopover = Page.Locator(".vpp-column-picker-popover:popover-open");
        await columnPopover.WaitForAsync();
        (await columnPopover.Locator(".vpp-column-picker-option").CountAsync()).Should().BeGreaterThan(5);
        await AssertTransientMotionAsync(columnPopover);
        await CaptureAsync("f4-review-users-columns-1920x1080.png");
        await Page.Keyboard.PressAsync("Escape");
        await AssertBoundedPageAndSharedFilterAsync("users", ".vpp-permission-user-page");

        await Page.GotoAsync($"{BaseUrl}dashboard/order-create");
        await WaitForRowsAsync(".vpp-order-builder-grid");
        await Page.Locator(".vpp-header-tab.is-active .vpp-header-breadcrumb-ancestor").WaitForAsync();
        (await Page.Locator(".vpp-order-flow-steps li").CountAsync()).Should().Be(2);
        await AssertBoundedPageAndSharedFilterAsync("order-create", ".vpp-order-create-page");
        var visibleRowNumbers = await Page.Locator(".vpp-order-builder-grid tbody tr td:first-child")
            .AllTextContentsAsync();
        visibleRowNumbers.Take(3).Should().Equal("1", "2", "3");
        await CaptureAsync("f4-review-order-create-1920x1080.png");
    }

    [Fact]
    public async Task HistoryDetailSearch_MatchesTheOrderListSearchMotif()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");

        var listSearch = Page.Locator(".vpp-history-orders-card .vpp-history-search");
        var detailSearch = Page.Locator(".vpp-history-detail-toolbar .vpp-filter-search");
        await listSearch.WaitForAsync(new() { Timeout = 60_000 });
        await detailSearch.WaitForAsync(new() { Timeout = 60_000 });

        var parity = await Page.EvaluateAsync<double[][]>("""
            () => {
                const list = document.querySelector('.vpp-history-orders-card .vpp-history-search');
                const detail = document.querySelector('.vpp-history-detail-toolbar .vpp-filter-search');
                const values = element => {
                    const rect = element.getBoundingClientRect();
                    const style = getComputedStyle(element);
                    return [rect.height, Number.parseFloat(style.borderRadius)];
                };
                return [values(list), values(detail)];
            }
            """);
        parity[1][0].Should().BeApproximately(parity[0][0], 0.5);
        parity[1][1].Should().BeApproximately(parity[0][1], 0.5);

        var visualParity = await Page.EvaluateAsync<string[]>("""
            () => ['.vpp-history-orders-card .vpp-history-search', '.vpp-history-detail-toolbar .vpp-filter-search']
                .map(selector => {
                    const style = getComputedStyle(document.querySelector(selector));
                    return `${style.backgroundColor}|${style.boxShadow}`;
                })
            """);
        visualParity[1].Should().Be(visualParity[0]);
        await CaptureAsync("f4-review-history-search-parity-1920x1080.png");
    }

    private async Task AssertBoundedPageAndSharedFilterAsync(string route, string rootSelector)
    {
        var root = Page.Locator(rootSelector);
        await root.WaitForAsync();
        var search = root.Locator(".vpp-filter-search").First;
        var select = root.Locator(".vpp-filter-select-trigger").First;
        await search.WaitForAsync();
        await select.WaitForAsync();

        var geometry = await Page.EvaluateAsync<string>(
            $$"""
            () => {
                const root = document.querySelector('{{rootSelector}}');
                const search = root?.querySelector('.vpp-filter-search');
                const select = root?.querySelector('.vpp-filter-select-trigger');
                if (!root || !search || !select) return 'missing';
                const documentDoesNotScroll = document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1;
                const sameHeight = Math.abs(search.getBoundingClientRect().height - select.getBoundingClientRect().height) <= .5;
                const nativeScrollbar = [...root.querySelectorAll('*')]
                    .filter(element => ['auto', 'scroll'].includes(getComputedStyle(element).overflowY))
                    .every(element => {
                        const style = getComputedStyle(element);
                        return style.scrollbarGutter === 'auto' && (!style.scrollbarWidth || style.scrollbarWidth === 'auto');
                    });
                return `${documentDoesNotScroll && sameHeight && nativeScrollbar}|document=${documentDoesNotScroll}|height=${sameHeight}|native=${nativeScrollbar}`;
            }
            """);
        geometry.Should().StartWith("true", $"{route} must stay within the viewport and use native scroll regions");

        await select.ClickAsync();
        var popup = root.Locator(".vpp-filter-select-popover:popover-open").First;
        await popup.WaitForAsync();
        await AssertTransientMotionAsync(popup);
        await Page.Keyboard.PressAsync("Escape");
        await popup.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await root.Locator(".rz-datatable-loading").WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });
        await WaitForRenderSettleAsync();
    }

    private static async Task AssertTransientMotionAsync(ILocator popup)
    {
        var contract = await popup.EvaluateAsync<string>("""
            element => {
                const style = getComputedStyle(element);
                const geometryKeyframes = (element.getAnimations()[0]?.effect.getKeyframes() ?? [])
                    .some(frame => ['transform', 'translate', 'scale']
                        .some(property => frame[property] && frame[property] !== 'none'));
                return `${element.classList.contains('vpp-transient-surface') && !geometryKeyframes}|${style.animationName}|${style.animationDuration}`;
            }
            """);
        contract.Should().StartWith("true|vpp-transient-enter-");
        contract.Should().EndWith("|0.2s");
    }

    private async Task WaitForRowsAsync(string gridSelector)
    {
        await Page.WaitForFunctionAsync(
            $"() => document.querySelectorAll('{gridSelector} tbody tr td').length > 1",
            null,
            new() { Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            "() => !document.querySelector('.rz-datatable-loading')",
            null,
            new() { Timeout = 60_000 });
    }

    private async Task CaptureAsync(string fileName)
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(evidenceDirectory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(evidenceDirectory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
