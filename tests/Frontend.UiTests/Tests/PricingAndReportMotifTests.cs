using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class PricingAndReportMotifTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PriceListColumnPicker_ShowsBusinessColumnsAndOptionalSystemId()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists", new()
        {
            WaitUntil = WaitUntilState.Load
        });

        var surface = Page.GetByTestId("price-lists-data-surface");
        await WaitForSurfaceAsync(surface);
        var trigger = surface.Locator(".vpp-column-picker-trigger");
        await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await trigger.Locator(".vpp-column-picker-count").InnerTextAsync())
            .Should().MatchRegex("^\\d+$");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "column-picker-toolbar-trigger.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await trigger.ClickAsync();
        var popover = Page.Locator(".vpp-column-picker-popover:popover-open");
        await popover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await popover.Locator(".vpp-column-picker-option").First
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var labels = await popover.Locator(".vpp-column-picker-label").AllInnerTextsAsync();
        labels.Should().Contain("Tên bảng giá");
        labels.Should().Contain("Mã bảng giá");
        labels.Should().Contain("Nhà cung cấp");
        labels.Should().Contain("Nguồn dữ liệu");
        labels.Should().NotContain("Nhập gần nhất");
        labels.Should().ContainSingle(label => label == "ID");
        labels.Should().NotContain(label => label == "#"
            || label == "Thao tác"
            || label.Contains("RowVersion", StringComparison.OrdinalIgnoreCase));

        var sourceOption = popover.Locator(".vpp-column-picker-option")
            .Filter(new() { HasText = "Nguồn dữ liệu" });
        await sourceOption.ScrollIntoViewIfNeededAsync();

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "price-list-data-source-picker.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        var selectedBackground = await popover.Locator(".vpp-column-picker-option.is-selected").First
            .EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
        selectedBackground.Should().Be("rgba(0, 0, 0, 0)");

        await sourceOption.ClickAsync();
        await surface.Locator("thead th").Filter(new() { HasText = "Nguồn dữ liệu" })
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Assertions.Expect(surface.Locator("tbody .vpp-category-chip").Filter(new() { HasText = "Mặc định" }))
            .ToHaveCountAsync(1);

        if (await popover.IsVisibleAsync())
        {
            await Page.Keyboard.PressAsync("Escape");
            await popover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
    }

    [Fact]
    public async Task PricingAndReports_KeepCanonicalContractsAcrossResponsiveViewports()
    {
        await LoginAsDefaultUserAsync();

        foreach (var viewport in new[]
                 {
                     (Width: 390, Height: 844),
                     (Width: 768, Height: 1024),
                     (Width: 1366, Height: 768),
                     (Width: 1920, Height: 1080)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists", new()
            {
                WaitUntil = WaitUntilState.Load
            });
            var priceLists = Page.GetByTestId("price-lists-data-surface");
            await WaitForSurfaceAsync(priceLists);
            await Page.WaitForFunctionAsync(
                """() => document.querySelectorAll('[data-testid="price-lists-data-surface"] tbody > tr').length > 0""",
                null,
                new() { Timeout = 60_000 });
            (await priceLists.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
            (await priceLists.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
            (await priceLists.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(1);
            (await priceLists.GetByTestId("price-list-lifecycle-menu").CountAsync()).Should().BeGreaterThan(0);
            await AssertNoDocumentOverflowAsync(viewport.Width, "price lists");
            await CaptureAsync($"pricing-price-lists-{viewport.Width}x{viewport.Height}.png");

            await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=prices", new()
            {
                WaitUntil = WaitUntilState.Load
            });
            var prices = Page.GetByTestId("prices-data-surface");
            await WaitForSurfaceAsync(prices);
            var context = Page.GetByTestId("price-context");
            await context.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const context = document.querySelector('[data-testid="price-context"]');
                    const trigger = context?.querySelector('.vpp-filter-select-trigger');
                    return !!trigger
                        && !trigger.hasAttribute('disabled')
                        && !/chọn bảng giá|select price list/i.test(trigger.textContent || '');
                }
                """,
                null,
                new() { Timeout = 60_000 });
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const grid = document.querySelector('[data-testid="prices-data-surface"] .vpp-price-grid');
                    const loadingDone = [...document.querySelectorAll('.rz-datatable-loading')].every(element => {
                        const style = getComputedStyle(element);
                        const rect = element.getBoundingClientRect();
                        return style.display === 'none'
                            || style.visibility === 'hidden'
                            || Number.parseFloat(style.opacity || '1') === 0
                            || rect.width === 0
                            || rect.height === 0;
                    });
                    return loadingDone && (grid?.querySelectorAll('tbody > tr > td').length || 0) > 1;
                }
                """,
                null,
                new() { Timeout = 60_000 });
            await WaitForRenderSettleAsync();
            (await context.Locator(".vpp-filter-select").CountAsync()).Should().Be(1,
                "bảng giá là context selector duy nhất; nhà cung cấp được suy ra từ bảng giá");
            (await prices.Locator(".vpp-data-toolbar .vpp-filter-select").CountAsync()).Should().Be(2,
                "toolbar chỉ chứa category và mapping status query filters");
            (await context.Locator("dd").CountAsync()).Should().Be(2);
            (await prices.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
            await AssertNoDocumentOverflowAsync(viewport.Width, "item prices");
            await CaptureAsync($"pricing-prices-{viewport.Width}x{viewport.Height}.png");

            await AssertReportContractsAsync(viewport.Width, viewport.Height);
        }
    }

    [Fact]
    public async Task Reports_KeepCanonicalContractsAcrossResponsiveViewports()
    {
        await LoginAsDefaultUserAsync();

        foreach (var viewport in new[]
                 {
                     (Width: 390, Height: 844),
                     (Width: 768, Height: 1024),
                     (Width: 1366, Height: 768),
                     (Width: 1920, Height: 1080)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await AssertReportContractsAsync(viewport.Width, viewport.Height);
        }
    }

    [Fact]
    public async Task PriceListLifecycleMenu_LightDismisses_AndReportHasEnglishContract()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists", new()
        {
            WaitUntil = WaitUntilState.Load
        });

        var priceLists = Page.GetByTestId("price-lists-data-surface");
        await WaitForSurfaceAsync(priceLists);
        var menuButton = priceLists.GetByTestId("price-list-lifecycle-menu").First;
        await menuButton.ClickAsync();
        var menu = Page.Locator(".rz-context-menu:visible");
        await menu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await menu.Locator("li, [role='menuitem']").CountAsync()).Should().BeGreaterThanOrEqualTo(5);
        await Page.Locator(".vpp-layout-header").ClickAsync(new() { Position = new() { X = 12, Y = 12 } });
        await menu.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        await Page.GotoAsync($"{BaseUrl}set-language?culture=en&returnUrl=%2Freport", new()
        {
            WaitUntil = WaitUntilState.Load
        });
        await Page.Locator("[data-vpp-workspace-pattern='analytics']").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Page.GetByTestId("report-departments-data-surface").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        (await Page.Locator("h1.vpp-report-heading").InnerTextAsync()).Should().Be("Reports");
        (await Page.GetByTestId("report-departments-data-surface").CountAsync()).Should().Be(1);
        (await Page.GetByTestId("report-products-data-surface").CountAsync()).Should().Be(1);
    }

    private async Task WaitForSurfaceAsync(ILocator surface)
    {
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.rz-datatable-loading')].every(element => {
                const style = getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return style.display === 'none'
                    || style.visibility === 'hidden'
                    || Number.parseFloat(style.opacity || '1') === 0
                    || rect.width === 0
                    || rect.height === 0;
            })
            """,
            null,
            new() { Timeout = 60_000 });
        await WaitForRenderSettleAsync();
    }

    private async Task AssertReportContractsAsync(int viewportWidth, int viewportHeight)
    {
        await Page.GotoAsync($"{BaseUrl}report", new() { WaitUntil = WaitUntilState.Load });
        var analytics = Page.Locator("[data-vpp-workspace-pattern='analytics']");
        await analytics.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.vpp-report-page .vpp-skeleton-page').length === 0",
            null,
            new() { Timeout = 60_000 });
        var reportSurfaces = analytics.Locator("[data-vpp-data-surface='true']");
        (await reportSurfaces.CountAsync()).Should().Be(2);
        for (var index = 0; index < 2; index++)
        {
            var surface = reportSurfaces.Nth(index);
            (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("static");
            (await surface.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
            (await surface.Locator(".vpp-data-summary-footer").CountAsync()).Should().Be(1);
            (await surface.Locator(".vpp-data-grid").CountAsync()).Should().Be(1,
                "report evidence surfaces keep their column frame even when filtered empty");
            (await surface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().Be(0,
                "evidence tables are bounded static collections, not fake paged grids");
        }

        var departmentSurface = Page.GetByTestId("report-departments-data-surface");
        var departmentSearch = Page.Locator(".vpp-report-filter-surface .vpp-filter-search input");
        await departmentSearch.FillAsync("__gtas_no_report_department__");
        await Assertions.Expect(departmentSurface.Locator(".vpp-data-grid-empty-state")).ToBeVisibleAsync();
        (await departmentSurface.GetAttributeAsync("data-vpp-surface-state")).Should().Be("filtered-empty");
        (await departmentSurface.Locator("thead").CountAsync()).Should().Be(1);
        await departmentSearch.FillAsync(string.Empty);

        (await analytics.Locator(".vpp-report-chart-grid .vpp-report-card").CountAsync()).Should().Be(2);
        await AssertNoDocumentOverflowAsync(viewportWidth, "reports");
        await CaptureAsync($"report-analytics-{viewportWidth}x{viewportHeight}.png");
    }

    private async Task AssertNoDocumentOverflowAsync(int viewportWidth, string route)
    {
        var geometry = await Page.EvaluateAsync<double[]>(
            "() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        geometry[0].Should().BeLessThanOrEqualTo(geometry[1] + 1,
            $"{route} must keep wide data inside its own surface at {viewportWidth}px");
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
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }
}
