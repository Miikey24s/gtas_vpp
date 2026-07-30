using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class Ds3WorkflowTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PeriodSettlement_UsesItemAndDepartmentViewsWithInlineSupplierSelection()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=review");

        var surface = Page.Locator("[data-testid='period-settlement-data-surface']:visible");
        await surface.WaitForAsync(new() { Timeout = 60_000 });
        await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });

        var customPeriodButton = Page.GetByRole(AriaRole.Button, new() { Name = "Tùy chọn", Exact = true });
        await customPeriodButton.ClickAsync();
        var periodPicker = Page.Locator(".vpp-settlement-period-scope-host .vpp-period-picker-popover");
        await periodPicker.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await WaitForRenderSettleAsync();
        await CaptureAsync("ds3-period-picker-open-1366x768.png");
        var periodPickerGeometry = await periodPicker.EvaluateAsync<double[]>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                const center = document.elementFromPoint(rect.left + rect.width / 2, rect.top + rect.height / 2);
                return [rect.width, rect.height, center && (center === element || element.contains(center)) ? 1 : 0];
            }
            """);
        periodPickerGeometry[0].Should().BeGreaterThan(240);
        periodPickerGeometry[1].Should().BeGreaterThan(120);
        periodPickerGeometry[2].Should().Be(1,
            "the settlement period picker must stay above the supplier decision strip");
        if (await surface.Locator(".vpp-content-state:visible").CountAsync() > 0)
        {
            await periodPicker.Locator("input[type='month']").FillAsync("2026-06");
            await periodPicker.GetByRole(AriaRole.Button, new() { Name = "Áp dụng", Exact = true }).ClickAsync();
            await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60_000
            });
        }
        else
        {
            await periodPicker.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).ClickAsync();
            await periodPicker.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        (await Page.Locator(".vpp-workflow-stepper:visible").CountAsync()).Should().Be(0);
        (await Page.Locator(".vpp-settlement-selector-row .vpp-segmented-selector:visible").CountAsync()).Should().Be(2);
        (await Page.Locator(".vpp-settlement-decision-strip:visible").CountAsync()).Should().Be(1);
        (await Page.GetByText("Phương án chốt", new() { Exact = true }).CountAsync()).Should().Be(0);

        var toolbar = surface.Locator(".vpp-settlement-data-toolbar:visible");
        (await toolbar.Locator(".vpp-filter-search").CountAsync()).Should().Be(1);
        (await toolbar.Locator(".vpp-filter-select").CountAsync()).Should().Be(3);
        (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("client-snapshot-paged");
        var viewSelector = Page.Locator(".vpp-settlement-selector-row");
        (await viewSelector.GetByRole(AriaRole.Button, new() { Name = "Phòng ban", Exact = true }).CountAsync()).Should().Be(1);
        (await Page.Locator(".vpp-settlement-decision-strip .vpp-filter-select").CountAsync()).Should().Be(2);
        (await Page.Locator(".vpp-settlement-decision-action .rz-button").CountAsync()).Should().Be(1);
        (await surface.Locator(".vpp-settlement-footer").CountAsync()).Should().Be(0);
        (await Page.GetByRole(AriaRole.Button, new() { Name = "Xem bản xem trước" }).CountAsync()).Should().Be(0);

        await AssertUnifiedWorkspaceRowsAsync(requireDesktopFilterRow: true);

        if (await surface.Locator(".rz-data-grid:visible").CountAsync() > 0)
        {
            (await surface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
            await CaptureAsync("ds3-period-settlement-items-1366x768.png");
        }

        await viewSelector.GetByRole(AriaRole.Button, new() { Name = "Mặt hàng", Exact = true }).ClickAsync();
        await Page.Locator(".vpp-period-filters.is-item-view").WaitForAsync();
        (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("client-snapshot-paged");
        (await toolbar.Locator(".vpp-filter-select").CountAsync()).Should().Be(2);

        var viewportContract = await Page.EvaluateAsync<string>("""
            () => `${document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1}`
                + `|${document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1}`
            """);
        viewportContract.Should().Be("true|true");
        await CaptureAsync("ds3-period-settlement-unified-1366x768.png");
        await AssertSurfaceFillsContentHeightAsync(surface);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1024, 768)]
    [InlineData(768, 1024)]
    [InlineData(390, 844)]
    public async Task UnifiedPeriodSettlement_RemainsContainedAcrossResponsiveViewports(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsDefaultUserAsync();

        foreach (var legacyStep in new[] { "review", "demand", "supply", "settle" })
        {
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab={legacyStep}");
            var surface = Page.Locator("[data-testid='period-settlement-data-surface']:visible");
            await surface.WaitForAsync(new() { Timeout = 60_000 });
            await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60_000
            });

            (await Page.Locator(".vpp-workflow-stepper:visible").CountAsync()).Should().Be(0);
            var containment = await Page.EvaluateAsync<string>("""
                () => {
                    const root = document.documentElement;
                    const main = document.querySelector('#main-content');
                    const noDocumentOverflow = root.scrollWidth <= root.clientWidth + 1;
                    const mainContained = !main || main.getBoundingClientRect().right <= root.clientWidth + 1;
                    return `${noDocumentOverflow}|${mainContained}`;
                }
                """);
            containment.Should().Be("true|true");

            await AssertUnifiedWorkspaceRowsAsync(requireDesktopFilterRow: false);
            await CaptureAsync($"ds3-period-settlement-{legacyStep}-{width}x{height}.png");
        }
    }

    private async Task AssertUnifiedWorkspaceRowsAsync(bool requireDesktopFilterRow)
    {
        var layout = await Page.EvaluateAsync<double[]>("""
            () => {
                const root = document.querySelector('.vpp-period-settlement-root');
                const analytics = document.querySelector('.vpp-period-settlement-page > .vpp-analytics-workspace-pattern');
                const selectors = document.querySelector('.vpp-settlement-selector-row');
                const decision = document.querySelector('.vpp-settlement-decision-strip');
                const surface = document.querySelector('[data-testid="period-settlement-data-surface"]');
                const filters = document.querySelector('.vpp-period-filters');
                const filterChildren = filters ? [...filters.children] : [];
                if (!root || !analytics || !selectors || !decision || !surface || filterChildren.length === 0) {
                    throw new Error('Unified settlement layout is incomplete.');
                }

                const analyticsRect = analytics.getBoundingClientRect();
                const selectorRect = selectors.getBoundingClientRect();
                const decisionRect = decision.getBoundingClientRect();
                const surfaceRect = surface.getBoundingClientRect();
                const filterTops = filterChildren.map(element => element.getBoundingClientRect().top);
                return [
                    Math.abs(analyticsRect.width - selectorRect.width),
                    Math.abs(analyticsRect.width - decisionRect.width),
                    Math.abs(analyticsRect.width - surfaceRect.width),
                    decisionRect.top - selectorRect.bottom,
                    surfaceRect.top - decisionRect.bottom,
                    Math.max(...filterTops) - Math.min(...filterTops)
                ];
            }
            """);

        layout[0].Should().BeLessThanOrEqualTo(2, "the period selector owns a full workspace row");
        layout[1].Should().BeLessThanOrEqualTo(2, "the supplier decision strip owns a full workspace row");
        layout[2].Should().BeLessThanOrEqualTo(2, "the data surface cannot collapse into the left analytics column");
        layout[3].Should().BeGreaterThanOrEqualTo(0, "the decision strip must render below the period selector");
        layout[4].Should().BeGreaterThanOrEqualTo(0, "the data surface must render below the decision strip");
        if (requireDesktopFilterRow)
        {
            layout[5].Should().BeLessThanOrEqualTo(2, "desktop filters stay on one aligned toolbar row");
        }
    }

    private async Task AssertSurfaceFillsContentHeightAsync(ILocator surface)
    {
        var bottomGap = await surface.EvaluateAsync<double>("""
            element => {
                const main = document.querySelector('#main-content');
                if (!main) return Number.POSITIVE_INFINITY;
                return Math.abs(main.getBoundingClientRect().bottom - element.getBoundingClientRect().bottom);
            }
            """);

        bottomGap.Should().BeLessThanOrEqualTo(2);
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
