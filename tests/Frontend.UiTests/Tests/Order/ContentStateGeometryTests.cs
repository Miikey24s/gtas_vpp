using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ContentStateGeometryTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task EmptyOrderGrid_DoesNotApplyInteractiveRowHover()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync(
            $"{BaseUrl}dashboard?tab=0&orderView=current",
            new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        var periodPicker = Page.Locator(".vpp-orders-period-decision");
        var trigger = periodPicker.Locator(".vpp-decision-select-trigger");
        await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await trigger.ClickAsync();
        var periodOptions = periodPicker.GetByRole(AriaRole.Option);
        (await periodOptions.CountAsync()).Should().BeGreaterThan(1);
        await periodOptions.Last.ClickAsync();

        var emptyState = Page.Locator(
            "[data-testid='current-order-panel-items'] .vpp-order-grid-empty");
        await emptyState.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        var beforeHover = await MeasureEmptyRowPaintAsync(emptyState);

        await emptyState.HoverAsync();
        var afterHover = await MeasureEmptyRowPaintAsync(emptyState);

        afterHover.Should().Be(
            beforeHover,
            "empty state không phải data row nên hover không được đổi nền");
        await CaptureEvidenceAsync("my-orders-empty-no-row-hover-1920x1080.png");
    }

    [Fact]
    public async Task FilteredEmptyStates_FillCatalogAndHistoryDataRegions()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsAsync(TestAccounts.Employee);

        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var catalogSurface = Page.GetByTestId("catalog-data-surface");
        await catalogSurface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await catalogSurface.Locator(".vpp-filter-search input").FillAsync("__gtas_no_catalog_match__");
        var catalogState = catalogSurface.Locator(".vpp-catalog-state");
        await catalogState.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        (await MeasureFillGeometryAsync(catalogState)).Should().StartWith(
            "true|",
            "Catalog filtered-empty phải lấp đầy toàn bộ body còn lại của data surface.");

        await CaptureEvidenceAsync("catalog-filtered-empty-full-height-1920x1080.png");

        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var historySurface = Page.GetByTestId("history-orders-data-surface");
        await historySurface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await historySurface.Locator(".vpp-filter-search input").FillAsync("__gtas_no_history_match__");
        var historyState = historySurface.Locator(".vpp-history-grid-state");
        await historyState.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        (await MeasureFillGeometryAsync(historyState)).Should().StartWith(
            "true|",
            "History filtered-empty phải giữ nguyên chiều cao list region đến đáy workspace.");

        await CaptureEvidenceAsync("history-filtered-empty-full-height-1920x1080.png");
    }

    private static Task<string> MeasureFillGeometryAsync(ILocator state) =>
        state.EvaluateAsync<string>("""
            state => {
                const surface = state.closest('[data-vpp-data-surface="true"]');
                if (!(surface instanceof HTMLElement) || !(state instanceof HTMLElement)) return 'missing';
                const body = surface.querySelector('.vpp-data-surface-body');
                if (!(body instanceof HTMLElement)) return 'missing';
                const bodyRect = body.getBoundingClientRect();
                const stateRect = state.getBoundingClientRect();
                const stateStyle = getComputedStyle(state);
                const bottomDelta = Math.abs(bodyRect.bottom - stateRect.bottom);
                const heightDelta = Math.abs(bodyRect.height - stateRect.height);
                const ok = state.classList.contains('is-fill-available')
                    && bottomDelta <= 2
                    && heightDelta <= 2
                    && stateStyle.height !== 'auto';
                return `${ok}|bottom=${bottomDelta}|height=${heightDelta}`
                    + `|body=${bodyRect.height}|state=${stateRect.height}|css=${stateStyle.height}`;
            }
            """);

    private static Task<string> MeasureEmptyRowPaintAsync(ILocator state) =>
        state.EvaluateAsync<string>("""
            state => {
                const row = state.closest('tr');
                const cell = state.closest('td');
                if (!(row instanceof HTMLElement) || !(cell instanceof HTMLElement)) return 'missing';
                const rowStyle = getComputedStyle(row);
                const cellStyle = getComputedStyle(cell);
                return `${rowStyle.backgroundColor}|${rowStyle.backgroundImage}`
                    + `|${cellStyle.backgroundColor}|${cellStyle.backgroundImage}`;
            }
            """);

    private async Task CaptureEvidenceAsync(string fileName)
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
