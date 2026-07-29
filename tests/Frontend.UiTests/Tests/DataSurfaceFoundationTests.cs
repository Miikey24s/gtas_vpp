using System.Collections.Concurrent;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class DataSurfaceFoundationTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PagedAndVirtualizedConsumers_ExposeTypedFoundationWithoutScrollRequests()
    {
        await LoginAsAsync(TestAccounts.Employee);

        foreach (var viewport in new[]
                 {
                     (Width: 1920, Height: 1080),
                     (Width: 1366, Height: 768),
                     (Width: 768, Height: 1024),
                     (Width: 390, Height: 844)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");
            await WaitForGridRowsAsync(".vpp-history-grid");

            var pagedSurface = Page.GetByTestId("history-orders-data-surface");
            await pagedSurface.WaitForAsync();
            (await pagedSurface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
            (await pagedSurface.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
            (await pagedSurface.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(1,
                "DS2 moves the canonical History toolbar into the shared data frame");
            (await pagedSurface.Locator(".vpp-filter-search").CountAsync()).Should().Be(1);
            (await pagedSurface.Locator(".vpp-filter-select").CountAsync()).Should().Be(2);
            (await pagedSurface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
            await AssertNoDocumentOverflowAsync(viewport.Width);
            await CaptureAsync($"ds2-history-paged-{viewport.Width}x{viewport.Height}.png");

            if (viewport.Width >= 1000)
            {
                await AssertDesktopRhythmAsync();
                var filterTrigger = pagedSurface.Locator(".vpp-filter-select-trigger").First;
                await filterTrigger.ClickAsync();
                var filterPanel = pagedSurface.Locator(".vpp-filter-select-popover:popover-open").First;
                await filterPanel.WaitForAsync();
                await CaptureAsync($"ds2-history-filter-popup-{viewport.Width}x{viewport.Height}.png");
                await Page.Keyboard.PressAsync("Escape");

                var codeTrigger = pagedSurface.Locator(".vpp-cell-value-popover-code .vpp-cell-value-popover-trigger").First;
                await codeTrigger.ClickAsync();
                var panel = pagedSurface.Locator(".vpp-cell-value-popover-panel").First;
                await panel.WaitForAsync();
                (await panel.GetAttributeAsync("class")).Should().Contain("vpp-transient-surface");
                await CaptureAsync($"ds2-history-cell-popover-{viewport.Width}x{viewport.Height}.png");
                await Page.Keyboard.PressAsync("Escape");
            }

            if (viewport.Width < 1000)
            {
                continue;
            }

            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
            var virtualizedSurface = Page.GetByTestId("current-order-panel-data-surface");
            await virtualizedSurface.WaitForAsync(new() { Timeout = 60_000 });
            await WaitForGridRowsAsync(".vpp-order-items-grid");
            (await virtualizedSurface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("client-snapshot-virtualized");
            (await virtualizedSurface.GetAttributeAsync("data-vpp-data-density")).Should().Be("rich-two-line");
            (await virtualizedSurface.Locator("[data-vpp-data-footer-mode='virtualized']").CountAsync()).Should().Be(1);
            (await virtualizedSurface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().Be(0);

            var renderedRows = await virtualizedSurface.Locator("tbody tr").CountAsync();
            renderedRows.Should().BeLessThanOrEqualTo(40, "virtualization must keep the mounted DOM bounded");

            var apiRequests = new ConcurrentQueue<string>();
            void ObserveRequest(object? _, IRequest request)
            {
                if (request.Url.Contains("/api/VPPRequest/", StringComparison.OrdinalIgnoreCase))
                {
                    apiRequests.Enqueue(request.Url);
                }
            }

            Page.Request += ObserveRequest;
            await virtualizedSurface.Locator(".rz-data-grid-data").EvaluateAsync("element => element.scrollTop = element.scrollHeight");
            await WaitForRenderSettleAsync();
            Page.Request -= ObserveRequest;

            apiRequests.Should().BeEmpty("client snapshot virtualization must not fetch another window while scrolling");
            await AssertNoDocumentOverflowAsync(viewport.Width);
            await CaptureAsync($"ds1-order-items-virtualized-{viewport.Width}x{viewport.Height}.png");

            if (viewport.Width == 1366)
            {
                await SetDarkModeAsync(true);
                await CaptureAsync("ds1-order-items-virtualized-dark-1366x768.png");
                await SetDarkModeAsync(false);
            }
        }
    }

    [Fact]
    public async Task Ds4AdminRoutes_ExposeOneCanonicalToolbarAndServerPagedSurface()
    {
        await LoginAsDefaultUserAsync();
        var routes = new[]
        {
            (Path: "library?tab=0", MinimumSurfaces: 2, Label: "lookup"),
            (Path: "library?tab=1", MinimumSurfaces: 1, Label: "categories"),
            (Path: "library?tab=2", MinimumSurfaces: 1, Label: "items"),
            (Path: "library?tab=3", MinimumSurfaces: 1, Label: "suppliers"),
            (Path: "library?tab=5", MinimumSurfaces: 1, Label: "departments"),
            (Path: "library?tab=6&pricingTab=price-lists", MinimumSurfaces: 1, Label: "price-lists"),
            (Path: "library?tab=6&pricingTab=prices", MinimumSurfaces: 1, Label: "prices"),
            (Path: "permission?tab=0", MinimumSurfaces: 1, Label: "users"),
            (Path: "permission?tab=1", MinimumSurfaces: 1, Label: "permissions")
        };

        foreach (var viewport in new[]
                 {
                     (Width: 1920, Height: 1080),
                     (Width: 768, Height: 1024)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            foreach (var route in routes)
            {
                await Page.GotoAsync($"{BaseUrl}{route.Path}", new() { WaitUntil = WaitUntilState.Load });
                await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new()
                {
                    State = WaitForSelectorState.Attached
                });
                var surfaces = Page.Locator("[data-vpp-data-surface='true']");
                await Page.WaitForFunctionAsync(
                    $"() => document.querySelectorAll('[data-vpp-data-surface=\"true\"]').length >= {route.MinimumSurfaces}",
                    null,
                    new() { Timeout = 60_000 });
                await WaitForDataGridsToSettleAsync();

                (await surfaces.CountAsync()).Should().BeGreaterThanOrEqualTo(route.MinimumSurfaces);
                for (var index = 0; index < route.MinimumSurfaces; index++)
                {
                    var surface = surfaces.Nth(index);
                    (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
                    (await surface.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
                    (await surface.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(1);
                    (await surface.Locator(".vpp-data-grid").CountAsync()).Should().BeGreaterThanOrEqualTo(1);
                }

                if (viewport.Width == 768 && route.Label is "categories" or "price-lists" or "users")
                {
                    var responsiveWorkspace = Page.Locator(".vpp-list-detail-workspace").First;
                    var columnCount = await responsiveWorkspace.EvaluateAsync<int>(
                        "element => getComputedStyle(element).gridTemplateColumns.trim().split(/\\s+/).length");
                    columnCount.Should().Be(1, "list/detail workspaces stack at tablet width instead of clipping either pane");
                }

                await AssertNoDocumentOverflowAsync(viewport.Width);
                await CaptureAsync($"ds4-{route.Label}-{viewport.Width}x{viewport.Height}.png");
            }
        }

        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}report", new() { WaitUntil = WaitUntilState.Load });
        await Page.Locator(".vpp-report-page:not([aria-busy='true'])").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Page.Locator(".vpp-report-header").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Page.WaitForFunctionAsync(
            "() => document.querySelectorAll('.vpp-report-page .vpp-skeleton-page').length === 0",
            null,
            new() { Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.vpp-report-card .rz-chart path')].some(path => {
                const fill = getComputedStyle(path).fill;
                const box = path.getBBox();
                return box.width > 0 && box.height > 0
                    && fill !== 'none'
                    && fill !== 'rgba(0, 0, 0, 0)';
            })
            """,
            null,
            new() { Timeout = 60_000 });
        await WaitForRenderSettleAsync();
        var reportGrids = Page.Locator(".vpp-report-page .rz-data-grid");
        var canonicalReportGrids = Page.Locator(".vpp-report-page .vpp-data-grid.vpp-data-density-compact");
        (await canonicalReportGrids.CountAsync()).Should().Be(await reportGrids.CountAsync(),
            "every rendered static report grid uses the compact contract without pretending to be a paged surface");
        await AssertNoDocumentOverflowAsync(1366);
        await CaptureAsync("ds4-report-static-1366x768.png");
    }

    private async Task AssertDesktopRhythmAsync()
    {
        var rhythm = await Page.EvaluateAsync<double[]>("""
            () => {
                const toolbar = document.querySelector('.vpp-history-orders-toolbar');
                const header = document.querySelector('.vpp-history-grid thead th');
                const row = document.querySelector('.vpp-history-grid tbody tr');
                const footer = document.querySelector('.vpp-history-grid .rz-paginator, .vpp-history-grid .rz-pager');
                return [toolbar, header, row, footer].map(element => element?.getBoundingClientRect().height ?? 0);
            }
            """);

        rhythm[0].Should().BeApproximately(42, 1);
        rhythm[1].Should().BeApproximately(40, 1);
        rhythm[2].Should().BeApproximately(40, 1);
        rhythm[3].Should().BeApproximately(42, 1);
    }

    private async Task AssertNoDocumentOverflowAsync(int viewportWidth)
    {
        var geometry = await Page.EvaluateAsync<double[]>("""
            () => [document.documentElement.scrollWidth, document.documentElement.clientWidth]
            """);
        geometry[0].Should().BeLessThanOrEqualTo(geometry[1] + 1,
            $"the {viewportWidth}px viewport must not gain document-level horizontal scroll");
    }

    private async Task WaitForGridRowsAsync(string selector)
    {
        await Page.WaitForFunctionAsync(
            $"() => document.querySelectorAll('{selector} tbody tr td').length > 1",
            null,
            new() { Timeout = 60_000 });
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
    }

    private async Task WaitForDataGridsToSettleAsync()
    {
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

    private async Task SetDarkModeAsync(bool darkMode)
    {
        var currentMode = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('rz-theme-dark')");
        if (currentMode == darkMode)
        {
            return;
        }

        var dropdown = Page.Locator("#user-menu-dropdown");
        if (!await dropdown.IsVisibleAsync())
        {
            await Page.Locator(".user-menu-trigger").First.ClickAsync();
        }
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            darkMode
                ? "() => document.documentElement.classList.contains('rz-theme-dark')"
                : "() => !document.documentElement.classList.contains('rz-theme-dark')");
        if (await dropdown.IsVisibleAsync())
        {
            await Page.Locator(".user-dropdown-backdrop").ClickAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await WaitForRenderSettleAsync();
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
