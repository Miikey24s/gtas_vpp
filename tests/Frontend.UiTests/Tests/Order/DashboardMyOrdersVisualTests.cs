using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
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
                    || uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/initializers", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect && !isExpectedNavigationAssetAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2Fdashboard%3Ftab%3D0");

        await Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
        var refreshMotion = await Page.EvaluateAsync<string[]>("""
            () => {
                const sidebarHeader = document.querySelector('.vpp-sidebar-header');
                return [
                    document.documentElement.classList.contains('vpp-page-entering') ? 'true' : 'false',
                    sidebarHeader ? getComputedStyle(sidebarHeader).animationName : ''
                ];
            }
            """);
        refreshMotion[0].Should().Be("true", "a document refresh should start the coordinated shell reveal");
        refreshMotion[1].Should().Contain("vpp-shell-enter-inline", "the sidebar should use the shared refresh motion");
        await Page.WaitForFunctionAsync(
            "() => !document.documentElement.classList.contains('vpp-page-entering')",
            null,
            new PageWaitForFunctionOptions { Timeout = 2_000 });

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
                        .filter(button => /Tạo đơn kỳ này|Tạo đơn mới|Create Order/i.test(button.textContent ?? ''));
                    const exportActions = [...document.querySelectorAll('.order-page button')]
                        .filter(visible)
                        .filter(button => /Xuất PDF|Xuất Excel|Export PDF|Export Excel/i.test(button.textContent ?? ''));
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        document.querySelectorAll('.vpp-orders-story').length,
                        document.querySelectorAll('.vpp-orders-summary-grid article').length,
                        document.querySelectorAll('.vpp-orders-view-tabs').length,
                        [...document.querySelectorAll('.vpp-order-view-panel')].filter(visible).length,
                        orderPage
                            ? [...orderPage.querySelectorAll('.rzi')]
                                .filter(icon => !icon.closest('.rz-button, .rz-dropdown')).length
                            : -1,
                        document.querySelectorAll('.order-page .kpi-grid').length,
                        document.querySelectorAll('.vpp-orders-empty .vpp-empty-state-actions').length,
                        createActions.length,
                        document.querySelectorAll('[data-testid$="coming-soon"]:disabled').length,
                        document.querySelectorAll('.vpp-orders-summary-grid button[aria-pressed]').length,
                        document.querySelectorAll('.vpp-orders-state').length,
                        document.querySelectorAll('.vpp-order-view-meta .vpp-order-card-kind').length,
                        exportActions.filter(button => !button.disabled).length
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
            audit[9].Should().Be(0, "roadmap coming-soon placeholders were replaced by real export actions (W-C.0)");
            audit[10].Should().Be(3, "the order selector should expose exactly three pressed-state summary buttons");
            audit[11].Should().Be(0, "the current-cycle page should not repeat an open-period badge");
            audit[12].Should().Be(0, "the selected tab should replace the repeated order-type heading above the grid");
            if (viewport.Width >= 1366)
            {
                audit[13].Should().Be(2, "PDF and Excel export actions should be real enabled buttons on the seeded order");
            }

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
                        const exportAction = [...document.querySelectorAll('.order-page button')]
                            .find(button => /Xuất PDF|Xuất Excel|Export PDF|Export Excel/i.test(button.textContent ?? ''));
                        return [
                            workspaceRect?.bottom ?? Number.MAX_VALUE,
                            workspaceRect?.width ?? Number.MAX_VALUE,
                            workspaceRect && contentRect
                                ? Math.abs((workspaceRect.left + workspaceRect.width / 2) - (contentRect.left + contentRect.width / 2))
                                : Number.MAX_VALUE,
                            storyTitle ? parseFloat(getComputedStyle(storyTitle).fontSize) : 0,
                            summaryLabel ? parseFloat(getComputedStyle(summaryLabel).fontSize) : Number.MAX_VALUE,
                            summaryCard ? parseFloat(getComputedStyle(summaryCard).borderTopWidth) : 0,
                            exportAction ? parseFloat(getComputedStyle(exportAction).opacity) : 0,
                            document.documentElement.scrollHeight - window.innerHeight
                        ];
                    }
                    """);
                desktopGeometry[0].Should().BeLessThanOrEqualTo(viewport.Height + 1, "the short-order workspace should fit inside one desktop viewport");
                desktopGeometry[1].Should().BeLessThanOrEqualTo(1760.5, "wide layouts should stay bounded without visually detaching from the sidebar");
                desktopGeometry[2].Should().BeLessThanOrEqualTo(8, "the My Orders workspace should remain centered in its content region");
                desktopGeometry[3].Should().BeGreaterThan(desktopGeometry[4] + 8, "the period title must clearly outrank summary labels");
                desktopGeometry[5].Should().BeGreaterThanOrEqualTo(1, "summary items should read as bordered cards");
                desktopGeometry[6].Should().BeGreaterThanOrEqualTo(0.75, "export action labels must remain legible");
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
                            getComputedStyle(document.querySelector('.vpp-layout-header')).backgroundColor,
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

                var headerGeometry = await Page.EvaluateAsync<double[]>("""
                    () => {
                        const sidebar = document.querySelector('.vpp-sidebar');
                        const nav = document.querySelector('.vpp-layout-header');
                        const sidebarRect = sidebar?.getBoundingClientRect();
                        const navRect = nav?.getBoundingClientRect();
                        return [
                            sidebarRect?.right ?? Number.MAX_VALUE,
                            navRect?.left ?? Number.MAX_VALUE,
                            navRect?.right ?? 0,
                            window.innerWidth
                        ];
                    }
                    """);
                headerGeometry[1].Should().BeApproximately(headerGeometry[0], 1,
                    "the primary header should connect directly to the sidebar without an inset gap");
                headerGeometry[2].Should().BeApproximately(headerGeometry[3], 1,
                    "the primary header should bleed through the content inset to the viewport edge");

                var sidebarSeam = await Page.EvaluateAsync<string[]>("""
                    () => {
                        const sidebar = document.querySelector('.rz-layout.vpp-layout > .rz-sidebar.vpp-sidebar');
                        const style = sidebar ? getComputedStyle(sidebar) : null;
                        return [style?.borderRightWidth ?? '', style?.boxShadow ?? ''];
                    }
                    """);
                sidebarSeam[0].Should().Be("0px", "the logo/header row must not have a sidebar border seam");
                sidebarSeam[1].Should().Be("none", "the logo/header row must not have a Radzen edge shadow");

                // The active /dashboard route auto-expands its PanelMenu group, so the row
                // below the first root item can be a child row. Wait until the shell has
                // published its restored state and the expand animation has settled, then
                // measure the first two *adjacent visible* hover surfaces (any menu level):
                // the 4px rhythm contract applies between every pair of adjacent surfaces.
                await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync();
                const string visibleNavRowRectsProbe = """
                    () => {
                        const rows = [...document.querySelectorAll(
                            '.vpp-sidebar-nav .rz-navigation-item > .rz-navigation-item-wrapper')]
                            .filter(wrapper => {
                                const style = getComputedStyle(wrapper);
                                return style.display !== 'none'
                                    && style.visibility !== 'hidden'
                                    && wrapper.getBoundingClientRect().height > 0;
                            })
                            .map(wrapper => wrapper.getBoundingClientRect())
                            .sort((left, right) => left.top - right.top);
                        return rows.map(rect => `${rect.top.toFixed(1)}:${rect.bottom.toFixed(1)}`).join('|');
                    }
                    """;
                await Page.WaitForFunctionAsync($$"""
                    () => {
                        const measure = {{visibleNavRowRectsProbe}};
                        const current = measure();
                        const isStable = current !== ''
                            && current.split('|').length >= 2
                            && window.__vppSidebarRhythmSnapshot === current;
                        window.__vppSidebarRhythmSnapshot = current;
                        return isStable;
                    }
                    """);
                var sidebarRhythm = await Page.EvaluateAsync<double[]>($$"""
                    () => {
                        const header = document.querySelector('.vpp-sidebar-header');
                        const measure = {{visibleNavRowRectsProbe}};
                        const rows = measure().split('|')
                            .filter(entry => entry.length > 0)
                            .map(entry => entry.split(':').map(Number));
                        return [
                            header?.getBoundingClientRect().bottom ?? Number.MAX_VALUE,
                            rows[0]?.[0] ?? 0,
                            rows[0]?.[1] ?? Number.MAX_VALUE,
                            rows[1]?.[0] ?? 0
                        ];
                    }
                    """);
                var lineToFirstHoverGap = sidebarRhythm[1] - sidebarRhythm[0];
                var firstToSecondHoverGap = sidebarRhythm[3] - sidebarRhythm[2];
                lineToFirstHoverGap.Should().BeApproximately(firstToSecondHoverGap, 1.5,
                    "the toolbar-to-first-row gap should match the visual gap between hover surfaces");
            }
        }

        await Page.SetViewportSizeAsync(1366, 900);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
        var currentOrderPanel = Page.Locator("[data-testid='current-order-panel']:visible");
        await currentOrderPanel.WaitForAsync();

        var itemCategoryTrigger = currentOrderPanel.Locator(".vpp-history-detail-select .vpp-history-select-trigger").Nth(0);
        await itemCategoryTrigger.ClickAsync();
        var itemCategoryMenu = currentOrderPanel.Locator(".vpp-history-detail-select .vpp-history-select-menu").Nth(0);
        await itemCategoryMenu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var itemCategoryMenuMotion = await itemCategoryMenu.EvaluateAsync<string>("""
            menu => {
                const styles = getComputedStyle(menu);
                return `${menu.classList.contains('vpp-transient-surface')}|${styles.animationName}|${styles.animationDuration}|${styles.animationTimingFunction}`;
            }
        """);
        itemCategoryMenuMotion.Should().StartWith("true|vpp-transient-enter-",
            "shared order filters must use the global transient-surface motion language");
        itemCategoryMenuMotion.Should().Contain("|0.2s|cubic-bezier(0.32, 0.72, 0, 1)");
        var geometryAffectingKeyframes = await itemCategoryMenu.EvaluateAsync<string[]>("""
            menu => (menu.getAnimations()[0]?.effect.getKeyframes() ?? [])
                .flatMap(frame => ['transform', 'translate', 'scale']
                    .filter(property => frame[property] && frame[property] !== 'none')
                    .map(property => `${property}:${frame[property]}`))
            """);
        geometryAffectingKeyframes.Should().BeEmpty(
            "anchored transient surfaces must reveal without changing the rectangle used for viewport positioning");

        var motionFrames = await itemCategoryMenu.EvaluateAsync<double[][]>("""
            menu => new Promise(resolve => {
                const frames = [];
                const sample = () => {
                    const rect = menu.getBoundingClientRect();
                    frames.push([rect.left, rect.top, rect.width, rect.height]);
                    if (frames.length < 10) {
                        requestAnimationFrame(sample);
                        return;
                    }
                    resolve(frames);
                };
                requestAnimationFrame(sample);
            })
            """);
        (motionFrames.Max(frame => frame[0]) - motionFrames.Min(frame => frame[0])).Should().BeLessThan(0.75,
            "the popup left edge must remain visually anchored throughout the opening effect");
        (motionFrames.Max(frame => frame[2]) - motionFrames.Min(frame => frame[2])).Should().BeLessThan(0.75,
            "the popup width must not pulse while opening");
        var itemCategoryMenuGeometry = await itemCategoryMenu.EvaluateAsync<string>("""
            menu => {
                const rect = menu.getBoundingClientRect();
                const contained = rect.left >= 0 && rect.right <= window.innerWidth && rect.top >= 0 && rect.bottom <= window.innerHeight;
                return `${contained}|left=${Math.round(rect.left)}|right=${Math.round(rect.right)}|width=${Math.round(rect.width)}`;
            }
        """);
        itemCategoryMenuGeometry.Should().StartWith("true", "My Orders must reuse the bounded History filter popup");
        await itemCategoryMenu.GetByRole(AriaRole.Option).Nth(0).ClickAsync();
        await itemCategoryMenu.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        var transientEvidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(transientEvidenceDirectory))
        {
            Directory.CreateDirectory(transientEvidenceDirectory);
            await Page.SetViewportSizeAsync(1920, 1080);
            await itemCategoryTrigger.ClickAsync();
            await itemCategoryMenu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(transientEvidenceDirectory, "f4-filter-motion-start.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Allow,
                Caret = ScreenshotCaret.Hide
            });
            await Task.Delay(70, TestContext.Current.CancellationToken);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(transientEvidenceDirectory, "f4-filter-motion-mid.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Allow,
                Caret = ScreenshotCaret.Hide
            });
            await Task.Delay(160, TestContext.Current.CancellationToken);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(transientEvidenceDirectory, "f4-filter-motion-settled.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Allow,
                Caret = ScreenshotCaret.Hide
            });
            await itemCategoryMenu.GetByRole(AriaRole.Option).Nth(0).ClickAsync();
            await itemCategoryMenu.WaitForAsync(new() { State = WaitForSelectorState.Detached });
            await Page.SetViewportSizeAsync(1366, 900);
        }

        var itemCodeTrigger = currentOrderPanel.Locator(".vpp-history-detail-code").First;
        await itemCodeTrigger.ClickAsync();
        await currentOrderPanel.Locator(".vpp-history-detail-code-popover").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await currentOrderPanel.Locator(".vpp-order-items-transient-backdrop").ClickAsync(new() { Force = true });
        await currentOrderPanel.Locator(".vpp-history-detail-code-popover").WaitForAsync(new() { State = WaitForSelectorState.Detached });

        var scrollContract = await Page.EvaluateAsync<string[]>("""
            () => {
                const grid = document.querySelector("[data-testid='current-order-panel-items'] .vpp-order-items-grid");
                const scrollHost = grid?.querySelector('.rz-data-grid-data, .rz-datatable-scrollable-body, .rz-datatable-tablewrapper');
                const visibleHeader = grid?.closest('.vpp-order-items-grid-region')?.querySelector('.vpp-history-detail-grid-header');
                return [
                    scrollHost ? getComputedStyle(scrollHost).overflowY : '',
                    visibleHeader && !scrollHost?.contains(visibleHeader) ? 'outside-scroll-host' : '',
                    grid?.getAttribute('style') ?? ''
                ];
            }
            """);
        new[] { "auto", "scroll" }.Should().Contain(scrollContract[0], "the grid body should own vertical scrolling");
        scrollContract[1].Should().Be("outside-scroll-host", "the shared visible header should remain outside the virtualized scroll body");
        scrollContract[2].Should().Contain("width: 100%", "the virtualized shared grid should fill its bounded region");

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
                    const grid = document.querySelector('.vpp-order-items-grid');
                    return grid && [grid, ...grid.querySelectorAll('*')]
                        .some(element => ['auto', 'scroll'].includes(getComputedStyle(element).overflowY)
                            && element.scrollHeight > element.clientHeight * 4);
                }
                """,
                null,
                new PageWaitForFunctionOptions { Timeout = 60_000 });
            var longListContract = await Page.EvaluateAsync<double[]>("""
                () => {
                    const grid = document.querySelector('.vpp-order-items-grid');
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
                    const grid = document.querySelector('.vpp-order-items-grid');
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
                        document.querySelectorAll('.vpp-order-items-grid tbody tr').length,
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
        (await Page.Locator(".vpp-orders-summary-grid article.is-selected button[aria-pressed='true']").InnerTextAsync())
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
