using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class DashboardMyOrdersVisualTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task MyOrders_UsesOneResponsiveDataStoryWithoutRepeatedActions()
    {
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        Page.Console += (_, message) =>
        {
            var isExpectedCircuitTransitionError = message.Text.Contains(
                    "Failed to complete negotiation with the server",
                    StringComparison.OrdinalIgnoreCase)
                && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase)
                && !isExpectedCircuitTransitionError)
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);
        Page.RequestFailed += (_, request) =>
        {
            var isExpectedCircuitDisconnect = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && (uri.AbsolutePath.Equals("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/negotiate", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            var isExpectedNavigationAssetAbort = uri is not null
                && (uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/initializers", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect && !isExpectedNavigationAssetAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2Fdashboard%3Ftab%3D0");

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await Page.Locator(".vpp-orders-story").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible
            });

            var audit = await Page.EvaluateAsync<int[]>("""
                () => {
                    const visible = element => {
                        const style = getComputedStyle(element);
                        return style.display !== 'none' && style.visibility !== 'hidden';
                    };
                    const orderPage = document.querySelector('.order-page');
                    const createActions = [...document.querySelectorAll('.order-page button')]
                        .filter(visible)
                        .filter(button => /Tạo đơn mới|Create Order/i.test(button.textContent ?? ''));
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        document.querySelectorAll('.vpp-orders-story').length,
                        document.querySelectorAll('.vpp-orders-summary-grid article').length,
                        document.querySelectorAll('.vpp-orders-view-tabs').length,
                        [...document.querySelectorAll('.vpp-order-view-panel')].filter(visible).length,
                        orderPage?.querySelectorAll('.rzi').length ?? -1,
                        document.querySelectorAll('.order-page .kpi-grid').length,
                        document.querySelectorAll('.vpp-orders-empty .vpp-empty-state-actions').length,
                        createActions.length,
                        document.querySelectorAll('[data-testid$="coming-soon"]:disabled').length,
                        document.querySelectorAll('.vpp-orders-summary-grid [role="radio"]').length,
                        document.querySelectorAll('.vpp-orders-state').length,
                        document.querySelectorAll('.vpp-order-view-meta .vpp-order-card-kind').length
                    ];
                }
                """);

            audit[0].Should().Be(0, $"My Orders must not overflow at {viewport.Width}px");
            audit[1].Should().Be(1, "the period takeaway should appear exactly once");
            audit[2].Should().Be(3, "the order workspace should summarize regular, supplement and previous-cycle orders");
            audit[3].Should().Be(0, "the summary cards should be the only order-view selector");
            audit[4].Should().Be(1, "only the selected reusable order panel should render");
            audit[5].Should().Be(0, "My Orders should use VppIcon instead of legacy rzi markup");
            audit[6].Should().Be(0, "the legacy four-card KPI strip should be removed");
            audit[7].Should().Be(0, "empty state must not repeat actions already shown in the story header");
            audit[8].Should().BeLessThanOrEqualTo(1, "the create-order action should have one source of truth");
            audit[9].Should().Be(2, "PDF and Excel roadmap actions should be visible but disabled");
            audit[10].Should().Be(3, "the order selector should expose exactly three radio-style summary cards");
            audit[11].Should().Be(0, "the current-cycle page should not repeat an open-period badge");
            audit[12].Should().Be(0, "the selected tab should replace the repeated order-type heading above the grid");

            if (viewport.Width >= 1366)
            {
                var desktopGeometry = await Page.EvaluateAsync<double[]>("""
                    () => {
                        const workspace = document.querySelector('.vpp-orders-workspace');
                        const workspaceRect = workspace?.getBoundingClientRect();
                        const contentRect = workspace?.parentElement?.getBoundingClientRect();
                        const storyTitle = document.querySelector('.vpp-orders-story-heading h2');
                        const summaryLabel = document.querySelector('.vpp-orders-summary-label');
                        const summaryCard = document.querySelector('.vpp-orders-summary-grid article');
                        const disabledExport = document.querySelector('.vpp-orders-export-button:disabled');
                        return [
                            workspaceRect?.bottom ?? Number.MAX_VALUE,
                            workspaceRect?.width ?? Number.MAX_VALUE,
                            workspaceRect && contentRect
                                ? Math.abs((workspaceRect.left + workspaceRect.width / 2) - (contentRect.left + contentRect.width / 2))
                                : Number.MAX_VALUE,
                            storyTitle ? parseFloat(getComputedStyle(storyTitle).fontSize) : 0,
                            summaryLabel ? parseFloat(getComputedStyle(summaryLabel).fontSize) : Number.MAX_VALUE,
                            summaryCard ? parseFloat(getComputedStyle(summaryCard).borderTopWidth) : 0,
                            disabledExport ? parseFloat(getComputedStyle(disabledExport).opacity) : 0,
                            document.documentElement.scrollHeight - window.innerHeight
                        ];
                    }
                    """);
                desktopGeometry[0].Should().BeLessThanOrEqualTo(viewport.Height + 1, "the short-order workspace should fit inside one desktop viewport");
                desktopGeometry[1].Should().BeLessThanOrEqualTo(1760.5, "wide layouts should stay bounded without visually detaching from the sidebar");
                desktopGeometry[2].Should().BeLessThanOrEqualTo(8, "the My Orders workspace should remain centered in its content region");
                desktopGeometry[3].Should().BeGreaterThan(desktopGeometry[4] + 8, "the period title must clearly outrank summary labels");
                desktopGeometry[5].Should().BeGreaterThanOrEqualTo(1, "summary items should read as bordered cards");
                desktopGeometry[6].Should().BeGreaterThanOrEqualTo(0.75, "disabled export labels must remain legible");
                desktopGeometry[7].Should().BeLessThanOrEqualTo(2, "short orders should not create document-level vertical scrolling");

                var shellColors = await Page.EvaluateAsync<string[]>("""
                    () => {
                        const resolveToken = token => {
                            const probe = document.createElement('span');
                            probe.style.color = `var(${token})`;
                            document.body.append(probe);
                            const color = getComputedStyle(probe).color;
                            probe.remove();
                            return color;
                        };
                        return [
                            getComputedStyle(document.querySelector('.vpp-layout-body')).backgroundColor,
                            getComputedStyle(document.querySelector('.vpp-sidebar')).backgroundColor,
                            getComputedStyle(document.querySelector('.vpp-admin-tabs > .rz-tabview-nav-container, .vpp-admin-tabs > .rz-tabview-nav')).backgroundColor,
                            getComputedStyle(document.querySelector('.vpp-orders-story')).backgroundColor,
                            getComputedStyle(document.querySelector('.vpp-order-view-panel')).backgroundColor,
                            resolveToken('--vpp-bg-base'),
                            resolveToken('--vpp-bg-elevated')
                        ];
                    }
                    """);
                shellColors[0].Should().Be(shellColors[5], "the app content canvas should use the semantic base-background token");
                shellColors[1].Should().Be(shellColors[6], "the sidebar should use the semantic elevated-background token");
                shellColors[2].Should().Be(shellColors[6], "the primary top header should use the semantic elevated-background token");
                shellColors[3].Should().Be(shellColors[6], "the period card should remain elevated above the content canvas");
                shellColors[4].Should().Be(shellColors[6], "the order grid should remain elevated above the content canvas");
            }
        }

        await Page.SetViewportSizeAsync(1366, 900);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
        await Page.Locator("[data-testid='current-order-panel']:visible").WaitForAsync();

        var scrollContract = await Page.EvaluateAsync<string[]>("""
            () => {
                const grid = document.querySelector('.vpp-order-grid-scrollable');
                const scrollHost = grid?.querySelector('.rz-data-grid-data, .rz-datatable-scrollable-body, .rz-datatable-tablewrapper');
                const header = grid?.querySelector('thead th');
                return [
                    scrollHost ? getComputedStyle(scrollHost).overflowY : '',
                    header ? getComputedStyle(header).position : '',
                    grid?.getAttribute('style') ?? ''
                ];
            }
            """);
        new[] { "auto", "scroll" }.Should().Contain(scrollContract[0], "the grid body should own vertical scrolling");
        scrollContract[1].Should().Be("sticky", "column headers should remain visible while the grid body scrolls");
        scrollContract[2].Should().Contain("height: 100%", "the virtualized grid needs a bounded viewport");

        if (int.TryParse(Environment.GetEnvironmentVariable("GTAS_E2E_LONG_ORDER_LINES"), out var longOrderLineCount)
            && longOrderLineCount >= 500)
        {
            await Page.WaitForFunctionAsync(
                """
                () => [...document.querySelectorAll('.vpp-orders-summary-count')]
                    .some(element => element.textContent?.trim() === '500')
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const grid = document.querySelector('.vpp-order-grid-scrollable');
                    return grid && [grid, ...grid.querySelectorAll('*')]
                        .some(element => ['auto', 'scroll'].includes(getComputedStyle(element).overflowY)
                            && element.scrollHeight > element.clientHeight * 4);
                }
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });
            var longListContract = await Page.EvaluateAsync<double[]>("""
                () => {
                    const grid = document.querySelector('.vpp-order-grid-scrollable');
                    const candidates = grid
                        ? [grid, ...grid.querySelectorAll('*')]
                            .filter(element => ['auto', 'scroll'].includes(getComputedStyle(element).overflowY))
                        : [];
                    const scrollHost = candidates.sort(
                        (left, right) => (right.scrollHeight - right.clientHeight) - (left.scrollHeight - left.clientHeight))[0];
                    const rows = grid?.querySelectorAll('tbody tr').length ?? 0;
                    const selectedTabCount = Math.max(
                        ...[...document.querySelectorAll('.vpp-orders-summary-count')]
                            .map(element => Number.parseInt(element.textContent ?? '0', 10)));
                    return [
                        rows,
                        scrollHost?.clientHeight ?? 0,
                        scrollHost?.scrollHeight ?? 0,
                        document.documentElement.scrollHeight - window.innerHeight,
                        selectedTabCount
                    ];
                }
                """);
            var longListDiagnostics = await Page.EvaluateAsync<string>("""
                () => {
                    const grid = document.querySelector('.vpp-order-grid-scrollable');
                    return JSON.stringify({
                        scrollCandidates: [...grid.querySelectorAll('*')]
                        .map(element => ({
                            tag: element.tagName,
                            className: element.className,
                            overflowY: getComputedStyle(element).overflowY,
                            clientHeight: element.clientHeight,
                            scrollHeight: element.scrollHeight,
                            height: getComputedStyle(element).height
                        }))
                        .filter(item => item.scrollHeight > item.clientHeight || ['auto', 'scroll'].includes(item.overflowY)),
                        bodyChildren: [...(grid.querySelector('tbody')?.children ?? [])].map(element => ({
                            tag: element.tagName,
                            className: element.className,
                            style: element.getAttribute('style'),
                            clientHeight: element.clientHeight,
                            scrollHeight: element.scrollHeight
                        }))
                    });
                }
                """);
            longListContract[4].Should().Be(500, "the long-list fixture must reach the requested 500 order lines");
            longListContract[0].Should().BeLessThan(100,
                "virtualization should keep the DOM row count bounded for a 500-line order");
            longListContract[1].Should().BeGreaterThan(0, "the internal grid viewport must have a measurable height");
            longListContract[2].Should().BeGreaterThan(longListContract[1] * 4,
                $"the 500-line order should scroll inside the grid rather than extending the page. DOM: {longListDiagnostics}");
            longListContract[3].Should().BeLessThanOrEqualTo(2,
                "a 500-line order must not introduce document-level vertical scrolling");
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("GTAS_E2E_SOAK_SECONDS"), out var soakSeconds)
            && soakSeconds >= 60)
        {
            var soakDeadline = DateTime.UtcNow.AddSeconds(soakSeconds);
            while (DateTime.UtcNow < soakDeadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                var soakContract = await Page.EvaluateAsync<int[]>("""
                    () => [
                        document.documentElement.scrollHeight - window.innerHeight,
                        document.querySelectorAll('.vpp-order-grid-scrollable tbody tr').length,
                        document.querySelectorAll('.vpp-tab-shared-indicator').length
                    ]
                    """);
                soakContract[0].Should().BeLessThanOrEqualTo(2, "the document must remain viewport-locked during the soak test");
                soakContract[1].Should().BeLessThan(100, "virtualized rows must remain bounded during the soak test");
                soakContract[2].Should().BeLessThanOrEqualTo(1, "only the primary navigation tab indicator should remain mounted");
            }
        }

        await Page.Locator(".vpp-orders-summary-grid article").Nth(1).Locator("button").ClickAsync();
        await WaitForUrlMatchAsync(new Regex(".*[?&]orderView=supplement(?:&.*)?$", RegexOptions.IgnoreCase));
        await Page.Locator("[data-testid='supplement-order-panel']:visible").WaitForAsync();
        await Page.Locator("[data-testid='supplement-order-panel'] .vpp-order-grid-empty").WaitForAsync();
        (await Page.Locator("[data-testid='supplement-order-panel-empty-action']").CountAsync())
            .Should().BeLessThanOrEqualTo(1, "an available supplement flow should expose one empty-state CTA");

        await Page.Locator(".vpp-orders-summary-grid article").Nth(2).Locator("button").ClickAsync();
        await WaitForUrlMatchAsync(new Regex(".*[?&]orderView=previous(?:&.*)?$", RegexOptions.IgnoreCase));
        await Page.ReloadAsync();
        await Page.Locator("[data-testid='previous-order-panel']:visible").WaitForAsync();
        (await Page.Locator(".vpp-orders-summary-grid article.is-selected [role='radio']").InnerTextAsync())
            .Should().Contain("Kỳ trước", "reload should preserve the selected order summary from the URL");

        await Page.Locator(".vpp-orders-summary-grid article").Nth(0).Locator("button").ClickAsync();
        await WaitForUrlMatchAsync(new Regex(".*[?&]orderView=current(?:&.*)?$", RegexOptions.IgnoreCase));

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            foreach (var evidenceViewport in new[]
            {
                new ViewportSize { Width = 1366, Height = 768 },
                new ViewportSize { Width = 1920, Height = 1080 }
            })
            {
                await Page.SetViewportSizeAsync(evidenceViewport.Width, evidenceViewport.Height);
                await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
                await Page.Locator(".vpp-orders-story").WaitForAsync();
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(evidenceDirectory, $"w1-dashboard-my-orders-{evidenceViewport.Width}x{evidenceViewport.Height}.png"),
                    FullPage = false,
                    Animations = ScreenshotAnimations.Disabled,
                    Caret = ScreenshotCaret.Hide,
                    Scale = ScreenshotScale.Css
                });
            }
        }

        await Page.GotoAsync($"{BaseUrl}set-language?culture=en&returnUrl=%2Fdashboard%3Ftab%3D0");
        await Page.Locator(".vpp-orders-story").WaitForAsync();
        (await Page.GetByText("Current Order Cycle", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);
        (await Page.GetByText("Current cycle order", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);

        browserErrors.Should().BeEmpty();
        requestFailures.Should().BeEmpty();
    }

    private async Task WaitForUrlMatchAsync(Regex expectedUrl)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline)
        {
            if (expectedUrl.IsMatch(Page.Url))
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException($"Timed out waiting for URL '{expectedUrl}'. Last URL: {Page.Url}");
    }
}
