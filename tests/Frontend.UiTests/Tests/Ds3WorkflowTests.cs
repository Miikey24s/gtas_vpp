using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class Ds3WorkflowTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PeriodSettlement_UsesUnifiedSelectorsSupplierDecisionAndPagedOrders()
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

        if (await surface.Locator(".vpp-content-state:visible").CountAsync() > 0)
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Tùy chọn" }).ClickAsync();
            await Page.Locator(".vpp-settlement-period-target .vpp-filter-select-trigger[aria-label='Tháng']").ClickAsync();
            await Page.Locator(".vpp-filter-select-popover:popover-open [role='option']", new() { HasText = "07" }).ClickAsync();
            await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60_000
            });
        }

        (await Page.Locator(".vpp-workflow-stepper:visible").CountAsync()).Should().Be(0);
        (await Page.Locator(".vpp-settlement-selector-row .vpp-segmented-selector:visible").CountAsync()).Should().Be(2);
        (await Page.Locator(".vpp-settlement-decision-strip:visible").CountAsync()).Should().Be(1);
        (await Page.GetByText("Phương án chốt", new() { Exact = true }).CountAsync()).Should().Be(0);

        var toolbar = surface.Locator(".vpp-settlement-data-toolbar:visible");
        (await toolbar.Locator(".vpp-filter-search").CountAsync()).Should().Be(1);
        (await toolbar.Locator(".vpp-filter-select").CountAsync()).Should().Be(3);
        (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");

        if (await surface.Locator(".rz-data-grid:visible").CountAsync() > 0)
        {
            (await surface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
            await CaptureAsync("ds3-period-settlement-orders-1366x768.png");
            var firstRow = surface.Locator(".vpp-settlement-order-grid .rz-data-row").First;
            if (await firstRow.CountAsync() > 0)
            {
                await firstRow.ClickAsync();
                await Page.Locator(".vpp-history-drawer.is-open:visible").WaitForAsync(new() { Timeout = 10_000 });
                await Page.Locator(".vpp-history-drawer.is-open .vpp-history-drawer-actions button[aria-label='Đóng']").ClickAsync();
            }
        }

        var compareButton = Page.GetByRole(AriaRole.Button, new() { Name = "So sánh phương án" });
        if (await compareButton.IsEnabledAsync())
        {
            await compareButton.ClickAsync();
            var dialog = Page.Locator(".vpp-settlement-supplier-dialog:visible");
            await dialog.WaitForAsync();
            (await dialog.GetAttributeAsync("class")).Should().Contain("vpp-transient-surface");
            await dialog.GetByRole(AriaRole.Button, new() { Name = "Đóng" }).ClickAsync();
        }

        await Page.GetByRole(AriaRole.Button, new() { Name = "Theo phòng ban" }).ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => document.querySelector("[data-testid='period-settlement-data-surface']")
                ?.getAttribute("data-vpp-data-source-mode") === "client-snapshot-paged"
            """);
        (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("client-snapshot-paged");

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
            await CaptureAsync($"ds3-period-settlement-{legacyStep}-{width}x{height}.png");
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
