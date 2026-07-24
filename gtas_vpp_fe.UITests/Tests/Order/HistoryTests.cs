using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class HistoryTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task LongDetailGrid_KeepsVirtualRowsBelowFixedHeader()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Commit
        });

        var detailRegion = Page.Locator(".vpp-history-detail-grid-region");
        var detailHeader = detailRegion.Locator(".vpp-history-detail-grid-header");
        await detailHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var detailGrid = detailRegion.Locator(".vpp-history-detail-grid");
        var detailScroll = detailGrid.Locator(".rz-data-grid-data");
        await Page.WaitForFunctionAsync("""
            () => {
                const surface = document.querySelector('.vpp-history-detail-grid .rz-data-grid-data');
                return surface && surface.scrollHeight > surface.clientHeight + 1;
            }
        """);
        await detailScroll.EvaluateAsync("surface => { surface.scrollTop = Math.min(180, surface.scrollHeight - surface.clientHeight); }");
        await Page.WaitForFunctionAsync("""
            () => document.querySelector('.vpp-history-detail-grid .rz-data-grid-data')?.scrollTop > 0
        """);
        await Page.WaitForTimeoutAsync(80);

        var fixedHeaderLayer = await detailRegion.EvaluateAsync<string>("""
            region => {
                const surface = region.querySelector('.rz-data-grid-data');
                const visibleHead = region.querySelector('.vpp-history-detail-grid-header');
                const nativeHead = region.querySelector('.vpp-history-detail-grid thead');
                const headers = [...region.querySelectorAll('.vpp-history-detail-grid-header-cell')];
                if (!surface || !visibleHead || !nativeHead || !headers.length) return 'missing';

                const parseAlpha = color => {
                    const match = color.match(/rgba?\([^,]+,[^,]+,[^,]+(?:,\s*([\d.]+))?\)/);
                    return match ? Number(match[1] ?? 1) : 0;
                };
                const hits = headers.map(header => {
                    const rect = header.getBoundingClientRect();
                    const hit = document.elementFromPoint(rect.left + Math.min(6, rect.width / 2), rect.top + rect.height / 2);
                    return {
                        matches: hit?.closest('.vpp-history-detail-grid-header-cell') === header,
                        tag: hit?.tagName ?? 'none',
                        className: hit?.className?.toString() ?? '',
                        text: hit?.textContent?.trim().slice(0, 32) ?? ''
                    };
                });
                const headRect = visibleHead.getBoundingClientRect();
                const surfaceRect = surface.getBoundingClientRect();
                const headStyle = getComputedStyle(visibleHead);
                const nativeHeadStyle = getComputedStyle(nativeHead);
                const opaque = parseAlpha(headStyle.backgroundColor) >= .99;
                const separatedFromViewport = Math.abs(headRect.bottom - surfaceRect.top) <= 1;
                const isolated = getComputedStyle(surface).isolation === 'isolate';
                const nativeHeaderPreserved = nativeHeadStyle.position === 'absolute'
                    && nativeHead.getBoundingClientRect().width <= 1;
                const hitHeader = hits.every(hit => hit.matches);
                return `${hitHeader && opaque && separatedFromViewport && isolated && nativeHeaderPreserved}|hit=${hitHeader}|hits=${JSON.stringify(hits)}|opaque=${opaque}|separated=${separatedFromViewport}|isolated=${isolated}|native=${nativeHeaderPreserved}|scroll=${surface.scrollTop}|head=${headRect.top}/${headRect.bottom}|surface=${surfaceRect.top}/${surfaceRect.bottom}`;
            }
        """);

        var screenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
        Directory.CreateDirectory(screenshotDirectory);
        await detailRegion.ScreenshotAsync(new()
        {
            Path = Path.Combine(screenshotDirectory, "history-detail-fixed-header-scrolled.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        fixedHeaderLayer.Should().StartWith("true", "virtualized detail rows must stay inside a separate scroll viewport below the fixed visible header");
    }

    [Fact]
    public async Task Employee_CanOpenSeededRequestHistoryFromHistoryTab()
    {
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.Commit
        });

        var loadingState = Page.Locator(".vpp-history-loading-state");
        await loadingState.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await loadingState.Locator(".vpp-history-loading-kpi").CountAsync()).Should().Be(4);
        (await Page.Locator(".vpp-history-loading-line").CountAsync()).Should().Be(0, "initial loading must use geometry-matched skeletons instead of a global progress line");
        var loadingScreenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
        Directory.CreateDirectory(loadingScreenshotDirectory);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(loadingScreenshotDirectory, "history-loading-1280x720.png"),
            Animations = ScreenshotAnimations.Disabled
        });

        var historyGrid = Page.Locator(".vpp-history-grid");
        await historyGrid.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        var rows = historyGrid.Locator("tbody tr");
        (await rows.CountAsync()).Should().BeGreaterThan(0);
        var chartScreenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
        Directory.CreateDirectory(chartScreenshotDirectory);
        var chartValueLabels = Page.Locator(".vpp-history-chart-value-label");
        await chartValueLabels.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var chartLabelGeometry = await Page.Locator(".vpp-history-chart svg").EvaluateAsync<string>("""
            svg => {
                const labels = [...svg.querySelectorAll('.vpp-history-chart-value-label')];
                const paths = [...svg.querySelectorAll('.rz-column-series path')]
                    .filter(path => path.getBoundingClientRect().width > 0 && path.getBoundingClientRect().height > 0);
                const inside = labels.length > 0 && labels.every(label => {
                    const labelRect = label.getBoundingClientRect();
                    const centerX = labelRect.left + (labelRect.width / 2);
                    const centerY = labelRect.top + (labelRect.height / 2);
                    return paths.some(path => {
                        const rect = path.getBoundingClientRect();
                        return centerX >= rect.left - 1 && centerX <= rect.right + 1
                            && centerY >= rect.top - 1 && centerY <= rect.bottom + 1;
                    });
                });
                return `${inside}|labels=${labels.map(label => label.textContent).join(',')}`;
            }
        """);
        chartLabelGeometry.Should().StartWith("true", "chart values must remain centered inside their visible columns");
        await Page.Locator(".vpp-history-chart-card").ScreenshotAsync(new()
        {
            Path = Path.Combine(chartScreenshotDirectory, "history-chart-values.png"),
            Animations = ScreenshotAnimations.Disabled
        });

        var scopeLabels = await Page.Locator(".vpp-history-scope > button").AllTextContentsAsync();
        var normalizedScopeLabels = scopeLabels.Select(label => label.Trim()).ToArray();
        normalizedScopeLabels.Take(5).Should().Equal("Tất cả kỳ", "Kỳ này", "3 tháng", "6 tháng", "12 tháng");
        normalizedScopeLabels[5].Should().StartWith("Tùy chọn");
        (await Page.Locator(".vpp-history-scope > button").Nth(1).GetAttributeAsync("class")).Should().Contain("is-active");
        (await Page.Locator(".vpp-history-scope > button").First.GetAttributeAsync("class")).Should().NotContain("is-active");
        (await Page.Locator(".vpp-history-drawer-close").CountAsync()).Should().Be(0);
        await Page.Locator(".vpp-history-scope > button").Nth(1).ClickAsync();
        await Page.WaitForTimeoutAsync(60);
        (await Page.Locator(".vpp-history-loading-line").CountAsync()).Should().Be(0, "period changes must preserve content without rendering a global progress line");

        var chartLegend = Page.Locator(".vpp-history-chart-legend");
        var regularLegend = chartLegend.GetByRole(AriaRole.Button, new() { Name = "Đơn thường", Exact = true });
        var additionalLegend = chartLegend.GetByRole(AriaRole.Button, new() { Name = "Đơn bổ sung", Exact = true });
        (await regularLegend.GetAttributeAsync("aria-pressed")).Should().Be("true");
        (await additionalLegend.GetAttributeAsync("aria-pressed")).Should().Be("true");
        var legendGeometryBefore = await chartLegend.Locator("button").EvaluateAllAsync<string>("""
            buttons => JSON.stringify(buttons.map(button => {
                const rect = button.getBoundingClientRect();
                return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
            }))
        """);
        var legendDecoration = await chartLegend.Locator("button").EvaluateAllAsync<string>("""
            buttons => buttons.map(button => getComputedStyle(button).textDecorationLine).join('|')
        """);
        legendDecoration.Should().NotContain("line-through");
        var legendOpticalAlignment = await chartLegend.Locator("button").EvaluateAllAsync<string>("""
            buttons => {
                const deltas = buttons.map(button => {
                    const buttonRect = button.getBoundingClientRect();
                    const swatchRect = button.querySelector('.vpp-history-chart-swatch')?.getBoundingClientRect();
                    const labelRect = button.querySelector('.vpp-history-chart-legend-label')?.getBoundingClientRect();
                    if (!swatchRect || !labelRect) return 999;
                    const buttonCenter = buttonRect.top + buttonRect.height / 2;
                    const swatchCenter = swatchRect.top + swatchRect.height / 2;
                    const labelCenter = labelRect.top + labelRect.height / 2;
                    return Math.max(Math.abs(buttonCenter - swatchCenter), Math.abs(buttonCenter - labelCenter), Math.abs(swatchCenter - labelCenter));
                });
                return `${deltas.every(delta => delta <= 1)}|deltas=${deltas.map(delta => Math.round(delta * 10) / 10).join(',')}`;
            }
        """);
        legendOpticalAlignment.Should().StartWith("true", "legend swatches and labels must share the same visual centre inside a stable hitbox");
        await additionalLegend.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.vpp-history-chart-legend-item')[1]?.getAttribute('aria-pressed') === 'false'");
        (await additionalLegend.GetAttributeAsync("aria-pressed")).Should().Be("false");
        (await regularLegend.GetAttributeAsync("aria-pressed")).Should().Be("true");
        var legendGeometryAfter = await chartLegend.Locator("button").EvaluateAllAsync<string>("""
            buttons => JSON.stringify(buttons.map(button => {
                const rect = button.getBoundingClientRect();
                return { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
            }))
        """);
        legendGeometryAfter.Should().Be(legendGeometryBefore, "toggling a chart series must not move or resize the neighboring legend button");
        await regularLegend.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.vpp-history-chart-legend-item')[0]?.getAttribute('aria-pressed') === 'false'");
        (await regularLegend.GetAttributeAsync("aria-pressed")).Should().Be("false");
        await Page.Locator(".vpp-history-chart-empty").GetByText("Chưa chọn loại đơn để hiển thị", new() { Exact = true }).WaitForAsync();
        await additionalLegend.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.vpp-history-chart-legend-item')[1]?.getAttribute('aria-pressed') === 'true'");
        (await additionalLegend.GetAttributeAsync("aria-pressed")).Should().Be("true");
        await Page.Locator(".vpp-history-chart-empty").WaitForAsync(new() { State = WaitForSelectorState.Detached });
        await regularLegend.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.vpp-history-chart-legend-item')[0]?.getAttribute('aria-pressed') === 'true'");

        var codeTrigger = rows.First.Locator(".vpp-history-code-trigger");
        await codeTrigger.ClickAsync();
        var codePopover = rows.First.Locator(".vpp-history-code-popover");
        await codePopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await codePopover.InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        var codePopoverGeometry = await codePopover.EvaluateAsync<string>("""
            popover => {
                const anchor = popover.parentElement;
                if (!anchor) return 'missing';
                const popoverRect = popover.getBoundingClientRect();
                const anchorRect = anchor.getBoundingClientRect();
                const separated = popoverRect.top >= anchorRect.bottom - 1 || popoverRect.bottom <= anchorRect.top + 1;
                const contained = popoverRect.left >= 7 && popoverRect.right <= window.innerWidth - 7
                    && popoverRect.top >= 7 && popoverRect.bottom <= window.innerHeight - 7;
                return `${separated && contained}|position=${getComputedStyle(popover).position}|contained=${contained}`;
            }
        """);
        codePopoverGeometry.Should().StartWith("true", "the full order code must remain visible above the grid stacking and viewport boundaries");
        var copyActionGeometry = await codePopover.Locator(".vpp-history-popover-copy").EvaluateAsync<string>("""
            button => {
                const icon = button.querySelector('.vpp-icon');
                if (!icon) return 'missing';
                const buttonRect = button.getBoundingClientRect();
                const iconRect = icon.getBoundingClientRect();
                const buttonStyle = getComputedStyle(button);
                const iconStyle = getComputedStyle(icon);
                const borderless = [buttonStyle.borderTopWidth, buttonStyle.borderRightWidth, buttonStyle.borderBottomWidth, buttonStyle.borderLeftWidth].every(width => width === '0px');
                const compact = Math.abs(buttonRect.width - 20) <= 1 && Math.abs(buttonRect.height - 20) <= 1 && Math.abs(parseFloat(iconStyle.fontSize) - 14) <= 1;
                const centred = Math.abs((buttonRect.left + buttonRect.width / 2) - (iconRect.left + iconRect.width / 2)) <= 1
                    && Math.abs((buttonRect.top + buttonRect.height / 2) - (iconRect.top + iconRect.height / 2)) <= 1;
                return `${borderless && compact && centred}|borderless=${borderless}|button=${buttonRect.width}x${buttonRect.height}|icon=${iconStyle.fontSize}|centred=${centred}`;
            }
        """);
        copyActionGeometry.Should().StartWith("true", "the inline copy affordance must be a small borderless icon centred beside the value");
        var copyScreenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
        Directory.CreateDirectory(copyScreenshotDirectory);
        await codePopover.ScreenshotAsync(new()
        {
            Path = Path.Combine(copyScreenshotDirectory, "history-code-copy-action.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        await Page.Locator("#history-orders-title").ClickAsync();
        await codePopover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var noteTrigger = rows.First.Locator(".vpp-history-note-trigger");
        await noteTrigger.ClickAsync();
        var notePopover = rows.First.Locator(".vpp-history-note-popover");
        await notePopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await notePopover.Locator(".vpp-history-popover-value").InnerTextAsync()).Should().Be(await noteTrigger.GetAttributeAsync("title"));
        await notePopover.Locator(".vpp-history-popover-copy").WaitForAsync();
        await Page.WaitForTimeoutAsync(50);
        var notePopoverGeometry = await notePopover.EvaluateAsync<string>("""
            popover => {
                const anchor = popover.parentElement;
                if (!anchor) return 'missing';
                const popoverRect = popover.getBoundingClientRect();
                const anchorRect = anchor.getBoundingClientRect();
                const separated = popoverRect.top >= anchorRect.bottom - 1 || popoverRect.bottom <= anchorRect.top + 1;
                const contained = popoverRect.left >= 7 && popoverRect.right <= window.innerWidth - 7
                    && popoverRect.top >= 7 && popoverRect.bottom <= window.innerHeight - 7;
                return `${separated && contained}|separated=${separated}|contained=${contained}|above=${popover.classList.contains('is-above')}`;
            }
        """);
        notePopoverGeometry.Should().StartWith("true", "the note popover must choose a collision-free direction and remain inside the viewport");
        await Page.Locator("#history-orders-title").ClickAsync();
        await notePopover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var ordersKpi = Page.Locator(".vpp-history-kpi-card").Nth(1);
        var kpiDisclosureState = await Page.Locator(".vpp-history-kpi-card").EvaluateAllAsync<string>("""
            cards => cards.map(card => {
                const button = card.querySelector('.vpp-history-kpi-trigger');
                if (!button) return 'missing-button';

                const elements = [card, button, ...button.querySelectorAll('*')];
                const generated = elements.flatMap(element => ['::before', '::after']
                    .map(pseudo => getComputedStyle(element, pseudo))
                    .filter(style => style.display !== 'none'
                        && style.content !== 'none'
                        && style.content !== 'normal'
                        && style.content !== '""'));
                const unexpectedChildren = [...button.children]
                    .filter(child => child.tagName !== 'SPAN' && child.tagName !== 'STRONG');
                const backgrounds = elements
                    .map(element => getComputedStyle(element).backgroundImage)
                    .filter(value => value && value !== 'none');

                return `${generated.length === 0 && unexpectedChildren.length === 0 && backgrounds.length === 0}`;
            }).join('|')
        """);
        kpiDisclosureState.Should().Be("true|true|true|true", "KPI cards must contain only their label and value, without decorative chevrons from markup, generated content, or background images");
        var kpiScreenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
        Directory.CreateDirectory(kpiScreenshotDirectory);
        await Page.Locator(".vpp-history-kpis").ScreenshotAsync(new()
        {
            Path = Path.Combine(kpiScreenshotDirectory, "history-kpi-cards.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        await ordersKpi.HoverAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                const trigger = document.querySelectorAll('.vpp-history-kpi-trigger')[1];
                if (!trigger) return false;
                const background = getComputedStyle(trigger).backgroundColor;
                return background !== 'rgba(0, 0, 0, 0)' && background !== 'transparent';
            }
        """);
        var kpiHoverGeometry = await ordersKpi.EvaluateAsync<string>("""
            card => {
                const trigger = card.querySelector('.vpp-history-kpi-trigger');
                if (!trigger) return 'missing';
                const cardRect = card.getBoundingClientRect();
                const triggerRect = trigger.getBoundingClientRect();
                const triggerStyle = getComputedStyle(trigger);
                const fillsCard = Math.abs(cardRect.left - triggerRect.left) <= 1
                    && Math.abs(cardRect.right - triggerRect.right) <= 1
                    && Math.abs(cardRect.top - triggerRect.top) <= 1
                    && Math.abs(cardRect.bottom - triggerRect.bottom) <= 1;
                const hasHoverFill = triggerStyle.backgroundColor !== 'rgba(0, 0, 0, 0)'
                    && triggerStyle.backgroundColor !== 'transparent';
                return `${fillsCard && hasHoverFill}|fills=${fillsCard}|background=${triggerStyle.backgroundColor}`;
            }
        """);
        kpiHoverGeometry.Should().StartWith("true", "KPI hover must fill the full card without an inset white frame");
        await Page.Locator(".vpp-history-kpis").ScreenshotAsync(new()
        {
            Path = Path.Combine(kpiScreenshotDirectory, "history-kpi-hover.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        await ordersKpi.GetByRole(AriaRole.Button).ClickAsync();
        var kpiPopover = Page.Locator("#history-kpi-orders");
        await kpiPopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await kpiPopover.GetByText("Tổng số đơn đã gửi", new() { Exact = false }).WaitForAsync();
        await Page.Locator("#history-chart-title").ClickAsync();
        await kpiPopover.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        await rows.First.ClickAsync();
        var drawer = Page.Locator(".vpp-history-drawer");
        await drawer.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await drawer.GetByText("Chi tiết đơn hàng", new() { Exact = true }).WaitForAsync();
        await drawer.Locator(".vpp-history-detail-grid-header-cell.vpp-history-detail-item").WaitForAsync();
        var selectedOrderCode = (await drawer.Locator(".vpp-history-drawer-code strong").InnerTextAsync()).Trim();
        var drawerHeadingAlignment = await drawer.Locator(".vpp-history-drawer-code").EvaluateAsync<string>("""
            row => {
                const code = row.querySelector('strong');
                const badge = row.querySelector('.vpp-badge');
                if (!code || !badge) return 'missing';
                const rowRect = row.getBoundingClientRect();
                const codeRect = code.getBoundingClientRect();
                const badgeRect = badge.getBoundingClientRect();
                const center = rect => rect.top + rect.height / 2;
                const deltas = [Math.abs(center(rowRect) - center(codeRect)), Math.abs(center(rowRect) - center(badgeRect)), Math.abs(center(codeRect) - center(badgeRect))];
                const stablePill = Math.abs(badgeRect.height - 22) <= 1;
                return `${stablePill && deltas.every(delta => delta <= 1)}|height=${badgeRect.height}|deltas=${deltas.map(delta => Math.round(delta * 10) / 10).join(',')}`;
            }
        """);
        drawerHeadingAlignment.Should().StartWith("true", "the selected-order code and status badge must share one optical centre");
        await drawer.Locator(".vpp-history-drawer-summary").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await drawer.Locator(".vpp-history-detail-grid-region").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await drawer.Locator(".vpp-history-detail-footer").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var detailRegion = drawer.Locator(".vpp-history-detail-grid-region");
        var detailHeader = detailRegion.Locator(".vpp-history-detail-grid-header");
        await detailHeader.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var detailGrid = drawer.Locator(".vpp-history-detail-grid");
        var detailHeaderLabels = await detailHeader.Locator(".vpp-history-detail-grid-header-cell").AllInnerTextsAsync();
        detailHeaderLabels.Select(label => label.Trim()).Should().Equal("#", "Mặt hàng", "Danh mục", "Đơn vị", "Số lượng", "Ghi chú");
        var headerSurfaceParity = await Page.EvaluateAsync<string>("""
            () => {
                const master = document.querySelector('.vpp-history-grid thead th');
                const detailSurface = document.querySelector('.vpp-history-detail-grid-header');
                const detail = detailSurface?.querySelector('.vpp-history-detail-grid-header-cell');
                const body = document.querySelector('.vpp-history-grid tbody td');
                if (!master || !detailSurface || !detail || !body) return 'missing';
                const masterStyle = getComputedStyle(master);
                const detailSurfaceStyle = getComputedStyle(detailSurface);
                const detailStyle = getComputedStyle(detail);
                const bodyStyle = getComputedStyle(body);
                const sameBackground = masterStyle.backgroundColor === detailSurfaceStyle.backgroundColor;
                const tintedHeader = masterStyle.backgroundColor !== bodyStyle.backgroundColor;
                const sameColor = masterStyle.color === detailStyle.color;
                const sameWeight = masterStyle.fontWeight === detailStyle.fontWeight;
                return `${sameBackground && tintedHeader && sameColor && sameWeight}|bg=${masterStyle.backgroundColor}/${detailSurfaceStyle.backgroundColor}/body:${bodyStyle.backgroundColor}|color=${masterStyle.color}/${detailStyle.color}|weight=${masterStyle.fontWeight}/${detailStyle.fontWeight}`;
            }
        """);
        headerSurfaceParity.Should().StartWith("true", "master and detail tables must share the same header visual system");
        var detailScroll = detailGrid.Locator(".rz-data-grid-data");
        var detailCanScroll = await detailScroll.EvaluateAsync<bool>(
            "surface => surface.scrollHeight > surface.clientHeight + 1");
        if (detailCanScroll)
        {
            await detailScroll.EvaluateAsync("surface => { surface.scrollTop = Math.min(180, surface.scrollHeight - surface.clientHeight); }");
            await Page.WaitForFunctionAsync("""
                () => {
                    const surface = document.querySelector('.vpp-history-detail-grid .rz-data-grid-data');
                    return surface && surface.scrollTop > 0;
                }
            """);
            await Page.WaitForTimeoutAsync(50);

            var fixedHeaderLayer = await detailRegion.EvaluateAsync<string>("""
                region => {
                    const surface = region.querySelector('.rz-data-grid-data');
                    const head = region.querySelector('.vpp-history-detail-grid-header');
                    const headers = [...region.querySelectorAll('.vpp-history-detail-grid-header-cell')];
                    if (!surface || !head || !headers.length) return 'missing';

                    const parseAlpha = color => {
                        const match = color.match(/rgba?\([^,]+,[^,]+,[^,]+(?:,\s*([\d.]+))?\)/);
                        return match ? Number(match[1] ?? 1) : 0;
                    };
                    const headRect = head.getBoundingClientRect();
                    const surfaceRect = surface.getBoundingClientRect();
                    const hits = headers.map(header => {
                        const rect = header.getBoundingClientRect();
                        const hit = document.elementFromPoint(rect.left + Math.min(6, rect.width / 2), rect.top + rect.height / 2);
                        return {
                            matches: hit?.closest('.vpp-history-detail-grid-header-cell') === header,
                            tag: hit?.tagName ?? 'none',
                            className: hit?.className?.toString() ?? '',
                            text: hit?.textContent?.trim().slice(0, 32) ?? ''
                        };
                    });
                    const hitHeader = hits.every(hit => hit.matches);
                    const opaque = parseAlpha(getComputedStyle(head).backgroundColor) >= .99;
                    const separatedFromViewport = Math.abs(headRect.bottom - surfaceRect.top) <= 1;
                    const isolated = getComputedStyle(surface).isolation === 'isolate';
                    return `${hitHeader && opaque && separatedFromViewport && isolated}|hit=${hitHeader}|hits=${JSON.stringify(hits)}|opaque=${opaque}|separated=${separatedFromViewport}|isolated=${isolated}|scroll=${surface.scrollTop}|head=${headRect.top}/${headRect.bottom}|surface=${surfaceRect.top}/${surfaceRect.bottom}`;
                }
            """);

            var stickyScreenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
            Directory.CreateDirectory(stickyScreenshotDirectory);
            await detailRegion.ScreenshotAsync(new()
            {
                Path = Path.Combine(stickyScreenshotDirectory, "history-detail-fixed-header-scrolled.png"),
                Animations = ScreenshotAnimations.Disabled
            });
            fixedHeaderLayer.Should().StartWith("true", "virtualized detail rows must remain inside the body viewport below the fixed visible header");
        }
        var detailHierarchy = await drawer.EvaluateAsync<string>("""
            drawer => {
                const summary = drawer.querySelector('.vpp-history-drawer-summary');
                const grid = drawer.querySelector('.vpp-history-detail-grid-region');
                const footer = drawer.querySelector('.vpp-history-detail-footer');
                if (!summary || !grid || !footer) return 'missing';
                const summaryStyle = getComputedStyle(summary);
                const gridStyle = getComputedStyle(grid);
                const footerStyle = getComputedStyle(footer);
                const flat = summaryStyle.borderTopWidth === '0px'
                    && gridStyle.borderLeftWidth === '0px'
                    && gridStyle.borderRightWidth === '0px'
                    && footerStyle.borderLeftWidth === '0px'
                    && footerStyle.borderTopWidth === '1px';
                return `${flat}|summary=${summaryStyle.borderTopWidth}|grid=${gridStyle.borderLeftWidth}/${gridStyle.borderRightWidth}|footer=${footerStyle.borderLeftWidth}/${footerStyle.borderTopWidth}`;
            }
        """);
        detailHierarchy.Should().StartWith("true", "the detail panel should use one outer container with flat internal sections");
        var detailItem = drawer.Locator(".vpp-history-item-line").First;
        var detailItemName = detailItem.Locator(".vpp-history-item-name");
        var detailItemCode = detailItem.Locator(".vpp-history-detail-code");
        (await detailItemName.InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        var detailNoteGeometry = await detailGrid.Locator(".vpp-history-note-trigger").EvaluateAllAsync<string>("""
            triggers => triggers.map(trigger => {
                const rect = trigger.getBoundingClientRect();
                const cell = trigger.closest('td');
                const row = cell?.parentElement;
                const table = trigger.closest('table');
                const body = trigger.closest('tbody');
                const data = trigger.closest('.rz-data-grid-data');
                const grid = trigger.closest('.vpp-history-detail-grid');
                const drawer = trigger.closest('.vpp-history-drawer');
                const cellRect = cell?.getBoundingClientRect();
                const style = getComputedStyle(trigger);
                const cellStyle = cell ? getComputedStyle(cell) : null;
                const rowStyle = row ? getComputedStyle(row) : null;
                const cells = row ? [...row.children].map(item => `${item.className}:${Math.round(item.getBoundingClientRect().width)}`).join(',') : '';
                return `trigger=${rect.width}x${rect.height}@${rect.left},${rect.top};display=${style.display};visibility=${style.visibility};cell=${cellRect?.width}x${cellRect?.height};cellDisplay=${cellStyle?.display};cellWidth=${cellStyle?.width};cellClass=${cell?.className};row=${row?.getBoundingClientRect().width};rowDisplay=${rowStyle?.display};tracks=${rowStyle?.gridTemplateColumns};table=${table?.getBoundingClientRect().width};body=${body?.getBoundingClientRect().width};data=${data?.getBoundingClientRect().width};grid=${grid?.getBoundingClientRect().width};drawer=${drawer?.getBoundingClientRect().width};cells=${cells}`;
            }).join('|')
        """);
        detailNoteGeometry.Should().NotBeNullOrWhiteSpace();
        var detailNoteTrigger = detailGrid.Locator(".vpp-history-note-trigger").First;
        (await detailNoteTrigger.IsVisibleAsync()).Should().BeTrue($"the line-note column must remain visible in the responsive detail grid; {detailNoteGeometry}");
        (await detailNoteTrigger.InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        await detailNoteTrigger.ClickAsync();
        var detailNotePopover = detailGrid.Locator(".vpp-history-note-popover").First;
        await detailNotePopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await detailNotePopover.GetByRole(AriaRole.Button, new() { Name = "Sao chép", Exact = true }).WaitForAsync();
        await drawer.Locator(".vpp-history-order-meta").ClickAsync();
        await detailNotePopover.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        (await detailItemCode.InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        var identityOrder = await detailItem.EvaluateAsync<bool>("""
            item => {
                const name = item.querySelector('.vpp-history-item-name');
                const code = item.querySelector('.vpp-history-detail-code');
                if (!name || !code) return false;
                return name.getBoundingClientRect().top <= code.getBoundingClientRect().top;
            }
        """);
        identityOrder.Should().BeTrue("the human-readable item name must be visually primary, with VPP code below it");
        await detailItemCode.ClickAsync();
        var detailCodePopover = drawer.Locator(".vpp-history-detail-code-popover");
        await detailCodePopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.WaitForFunctionAsync("() => document.querySelector('.vpp-history-detail-code-popover')?.classList.contains('is-viewport-surface')");
        (await detailCodePopover.Locator(".vpp-history-popover-value").InnerTextAsync()).Should().Be(await detailItemCode.GetAttributeAsync("title"));
        await detailCodePopover.Locator(".vpp-history-popover-copy").WaitForAsync();
        var detailCodeGeometry = await detailCodePopover.EvaluateAsync<string>("""
            popover => {
                const anchor = popover.parentElement;
                if (!anchor) return 'missing';
                const popoverRect = popover.getBoundingClientRect();
                const anchorRect = anchor.getBoundingClientRect();
                const separated = popoverRect.top >= anchorRect.bottom - 1 || popoverRect.bottom <= anchorRect.top + 1;
                const contained = popoverRect.left >= 7 && popoverRect.right <= window.innerWidth - 7
                    && popoverRect.top >= 7 && popoverRect.bottom <= window.innerHeight - 7;
                return `${separated && contained}|position=${getComputedStyle(popover).position}|contained=${contained}`;
            }
        """);
        detailCodeGeometry.Should().StartWith("true", "the full item code must open as a viewport-safe disclosure surface");
        await Page.Locator("#history-orders-title").ClickAsync();
        await detailCodePopover.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        var detailScreenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
        Directory.CreateDirectory(detailScreenshotDirectory);
        await drawer.ScreenshotAsync(new()
        {
            Path = Path.Combine(detailScreenshotDirectory, "history-detail-item-identity.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        var detailSearchSurface = drawer.Locator(".vpp-history-detail-toolbar label");
        var detailSearchInput = detailSearchSurface.Locator("input");
        var detailClearFilters = drawer.Locator(".vpp-history-detail-clear");
        (await detailClearFilters.IsEnabledAsync()).Should().BeTrue();
        await detailSearchInput.FillAsync("Bìa");
        await Page.WaitForFunctionAsync("() => document.querySelector('.vpp-history-detail-toolbar label')?.classList.contains('is-active')");
        (await detailSearchSurface.GetAttributeAsync("class")).Should().Contain("is-active");
        (await detailClearFilters.GetAttributeAsync("class")).Should().Contain("is-active");
        await detailClearFilters.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelector('.vpp-history-detail-search input')?.value === ''");
        (await detailSearchInput.InputValueAsync()).Should().BeEmpty();
        await Page.WaitForFunctionAsync("() => !document.querySelector('.vpp-history-detail-toolbar label')?.classList.contains('is-active')");

        var detailCategoryFilter = drawer.Locator(".vpp-history-detail-select .vpp-history-select-trigger");
        await detailCategoryFilter.ClickAsync();
        var categoryOptions = Page.Locator(".vpp-history-detail-select .vpp-history-select-menu [role='option']");
        var categoryOptionCount = await categoryOptions.CountAsync();
        var longestCategoryIndex = await categoryOptions.EvaluateAllAsync<int>("""
            options => options.reduce((longest, option, index, all) =>
                index > 0 && option.textContent.trim().length > all[longest].textContent.trim().length ? index : longest,
                options.length > 1 ? 1 : 0)
        """);
        var longestOptionGeometry = await categoryOptions.Nth(longestCategoryIndex).EvaluateAsync<string>("""
            option => {
                const menu = option.closest('.vpp-history-select-menu');
                if (!menu) return 'missing';
                const full = option.scrollWidth <= option.clientWidth + 1;
                const rect = menu.getBoundingClientRect();
                const trigger = menu.parentElement?.querySelector('.vpp-history-select-trigger');
                const triggerRect = trigger?.getBoundingClientRect();
                const contained = rect.left >= 0 && rect.right <= window.innerWidth;
                const compact = rect.width <= 360 && (!triggerRect || rect.width >= triggerRect.width - 1);
                const anchored = !triggerRect || Math.min(Math.abs(rect.left - triggerRect.left), Math.abs(rect.right - triggerRect.right)) <= 2;
                return `${full && contained && compact && anchored}|full=${full}|contained=${contained}|compact=${compact}|anchored=${anchored}|width=${rect.width}|left=${rect.left}|right=${rect.right}|triggerLeft=${triggerRect?.left}|triggerRight=${triggerRect?.right}|viewport=${window.innerWidth}`;
            }
        """);
        longestOptionGeometry.Should().StartWith("true", "the category menu must stay content-sized, anchored to its trigger and viewport-safe");
        var selectedCategoryLabel = (await categoryOptions.Nth(longestCategoryIndex).InnerTextAsync()).Trim();
        await categoryOptions.Nth(longestCategoryIndex).ClickAsync();
        await Page.WaitForTimeoutAsync(200);
        if (categoryOptionCount > 1)
        {
            var categorySelectionState = await detailCategoryFilter.EvaluateAsync<string>("""
                trigger => `${trigger.classList.contains('is-active')}|title=${trigger.getAttribute('title')}|text=${trigger.textContent.trim()}`
            """);
            categorySelectionState.Should().StartWith("true", $"selecting detail category '{selectedCategoryLabel}' must activate the filter trigger");
        }
        var detailFilterGeometry = await detailCategoryFilter.EvaluateAsync<string>("""
            trigger => {
                const label = trigger.querySelector('.vpp-history-select-label');
                const icon = trigger.querySelector('.vpp-icon');
                const toolbar = trigger.closest('.vpp-history-detail-toolbar');
                if (!label || !icon || !toolbar) return 'missing';
                const labelRect = label.getBoundingClientRect();
                const iconRect = icon.getBoundingClientRect();
                const triggerRect = trigger.getBoundingClientRect();
                const noOverlap = labelRect.right <= iconRect.left - 2;
                const contained = toolbar.scrollWidth <= toolbar.clientWidth;
                const iconContained = iconRect.right <= triggerRect.right + 1;
                const completeValueRetained = trigger.getAttribute('title') === label.textContent.trim();
                return `${noOverlap && contained && iconContained && completeValueRetained}|noOverlap=${noOverlap}|contained=${contained}|icon=${iconContained}|title=${completeValueRetained}`;
            }
        """);
        detailFilterGeometry.Should().StartWith("true", "long detail filter labels must stay inside the stable toolbar and retain the complete value for disclosure");
        await detailSearchInput.FillAsync("Hộp");
        await Page.WaitForFunctionAsync("() => document.querySelector('.vpp-history-detail-clear')?.classList.contains('is-active')");
        (await detailClearFilters.GetAttributeAsync("class")).Should().Contain("is-active");
        var detailFilterRhythm = await drawer.Locator(".vpp-history-detail-toolbar").EvaluateAsync<string>("""
            toolbar => {
                const controls = [...toolbar.children];
                const rects = controls.map(control => control.getBoundingClientRect());
                const sameHeight = rects.every(rect => Math.abs(rect.height - rects[0].height) <= 1);
                const contained = toolbar.scrollWidth <= toolbar.clientWidth;
                return `${sameHeight && contained}|heights=${rects.map(rect => Math.round(rect.height)).join(',')}|contained=${contained}`;
            }
        """);
        detailFilterRhythm.Should().StartWith("true", "detail search, category and clear controls must share the order-list filter rhythm");
        await drawer.Locator(".vpp-history-detail-toolbar").ScreenshotAsync(new()
        {
            Path = Path.Combine(detailScreenshotDirectory, "history-detail-filters.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        await detailClearFilters.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                const clear = document.querySelector('.vpp-history-detail-clear');
                const category = document.querySelector('.vpp-history-detail-select .vpp-history-select-trigger');
                const search = document.querySelector('.vpp-history-detail-search input');
                return clear
                    && !clear.classList.contains('is-active')
                    && category
                    && !category.classList.contains('is-active')
                    && search?.value === '';
            }
        """);
        (await detailClearFilters.GetAttributeAsync("class")).Should().NotContain("is-active");
        var horizontalOverflow = await Page.EvaluateAsync<bool>("""
            () => document.documentElement.scrollWidth > document.documentElement.clientWidth
        """);
        horizontalOverflow.Should().BeFalse();

        await drawer.GetByRole(AriaRole.Button, new() { Name = "Xem lịch sử phiên bản", Exact = true }).ClickAsync();
        var dialog = Page.Locator(".rz-dialog:visible").Last;
        await dialog.GetByText("Vòng đời đơn yêu cầu", new() { Exact = false }).WaitForAsync();
        await dialog.GetByText("1 phiên bản", new() { Exact = false }).WaitForAsync();
        await dialog.GetByText("Dòng thời gian", new() { Exact = true }).WaitForAsync();
        await dialog.GetByText("Các phiên bản của đơn", new() { Exact = true }).WaitForAsync();
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Đóng", Exact = true }).ClickAsync();

        var mainFilterTriggers = Page.Locator(".vpp-history-filters .vpp-history-select-trigger");
        var orderTypeFilter = mainFilterTriggers.Nth(0);
        var statusFilter = mainFilterTriggers.Nth(1);
        var clearFilters = Page.Locator(".vpp-history-orders-card .vpp-history-clear");
        (await clearFilters.IsEnabledAsync()).Should().BeTrue();

        await ordersKpi.GetByRole(AriaRole.Button).ClickAsync();
        await kpiPopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await statusFilter.ClickAsync();
        await kpiPopover.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        var openFilterMenu = Page.Locator(".vpp-history-select-menu");
        await openFilterMenu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.Locator("#history-orders-title").ClickAsync();
        await openFilterMenu.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        await statusFilter.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Đã gửi", Exact = true }).ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.vpp-history-filters .vpp-history-select-trigger')[1]?.classList.contains('is-active')");
        (await statusFilter.GetAttributeAsync("class")).Should().Contain("is-active");
        (await clearFilters.GetAttributeAsync("class")).Should().Contain("is-active");

        await orderTypeFilter.ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Đơn thường", Exact = true }).ClickAsync();
        await Page.WaitForFunctionAsync("() => document.querySelectorAll('.vpp-history-filters .vpp-history-select-trigger')[0]?.classList.contains('is-active')");
        (await orderTypeFilter.GetAttributeAsync("class")).Should().Contain("is-active");

        await clearFilters.ClickAsync();
        await Page.WaitForFunctionAsync("""
            () => [...document.querySelectorAll('.vpp-history-filters .vpp-history-select-trigger')]
                .every(trigger => !trigger.classList.contains('is-active'))
                && !document.querySelector('.vpp-history-orders-card .vpp-history-clear')?.classList.contains('is-active')
        """);
        (await statusFilter.InnerTextAsync()).Should().Contain("Tất cả trạng thái");
        (await orderTypeFilter.InnerTextAsync()).Should().Contain("Tất cả loại đơn");
        await clearFilters.ClickAsync();

        var mainSearchSurface = Page.Locator(".vpp-history-orders-card .vpp-history-search");
        var mainSearchInput = mainSearchSurface.Locator("input");
        await mainSearchInput.FillAsync("QA");
        await Page.WaitForFunctionAsync("() => document.querySelector('.vpp-history-orders-card .vpp-history-search')?.classList.contains('is-active')");
        (await mainSearchSurface.GetAttributeAsync("class")).Should().Contain("is-active");
        var activeSearchVisual = await mainSearchSurface.EvaluateAsync<string>("""
            surface => {
                const input = surface.querySelector('input');
                const surfaceStyle = getComputedStyle(surface);
                const inputStyle = input ? getComputedStyle(input) : null;
                const isWhiteText = inputStyle?.color === 'rgb(255, 255, 255)';
                const hasOutline = surfaceStyle.boxShadow.includes('inset');
                return `${hasOutline && !isWhiteText}|outline=${hasOutline}|white=${isWhiteText}|bg=${surfaceStyle.backgroundColor}`;
            }
        """);
        activeSearchVisual.Should().StartWith("true", "a populated search must use a blue outline with readable dark text instead of a full blue fill");
        (await clearFilters.GetAttributeAsync("class")).Should().Contain("is-active");
        await clearFilters.ClickAsync();
        await Page.WaitForFunctionAsync("() => !document.querySelector('.vpp-history-orders-card .vpp-history-search')?.classList.contains('is-active')");
        (await mainSearchInput.InputValueAsync()).Should().BeEmpty();

        await statusFilter.ClickAsync();
        var statusMenu = Page.Locator(".vpp-history-orders-card .vpp-history-select-menu");
        await statusMenu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var statusMenuGeometry = await statusMenu.EvaluateAsync<string>("""
            menu => {
                const trigger = menu.parentElement?.querySelector('.vpp-history-select-trigger');
                if (!trigger) return 'missing';
                const rect = menu.getBoundingClientRect();
                const triggerRect = trigger.getBoundingClientRect();
                const contained = rect.left >= 7 && rect.right <= window.innerWidth - 7;
                const compact = rect.width <= 360 && rect.width >= triggerRect.width - 1;
                const anchored = Math.min(Math.abs(rect.left - triggerRect.left), Math.abs(rect.right - triggerRect.right)) <= 2;
                return `${contained && compact && anchored}|contained=${contained}|compact=${compact}|anchored=${anchored}|width=${rect.width}|trigger=${triggerRect.width}|left=${rect.left}|right=${rect.right}|triggerLeft=${triggerRect.left}|triggerRight=${triggerRect.right}`;
            }
        """);
        statusMenuGeometry.Should().StartWith("true", "a project select menu must remain content-sized instead of expanding to the viewport");

        await Page.EvaluateAsync("""
            () => {
                window.__historyGeometrySamples = [];
                window.__historyGeometrySampling = true;
                const selectors = {
                    chart: '.vpp-history-chart-card',
                    svg: '.vpp-history-chart svg',
                    orders: '.vpp-history-orders-card',
                    detail: '.vpp-history-drawer'
                };
                const sample = () => {
                    if (!window.__historyGeometrySampling) return;
                    const frame = {};
                    for (const [key, selector] of Object.entries(selectors)) {
                        const element = document.querySelector(selector);
                        if (!element) continue;
                        const rect = element.getBoundingClientRect();
                        frame[key] = { left: rect.left, top: rect.top, width: rect.width, height: rect.height };
                    }
                    window.__historyGeometrySamples.push(frame);
                    requestAnimationFrame(sample);
                };
                requestAnimationFrame(sample);
            }
        """);
        await Page.GetByRole(AriaRole.Option, new() { Name = "Đã từ chối", Exact = true }).ClickAsync();
        await Page.Locator(".vpp-history-orders-card").GetByText("Không có đơn nào khớp bộ lọc đã chọn.", new() { Exact = true }).WaitForAsync();
        await drawer.GetByText(selectedOrderCode, new() { Exact = true }).WaitForAsync();
        await Page.WaitForTimeoutAsync(240);
        var interactionGeometry = await Page.EvaluateAsync<string>("""
            () => {
                window.__historyGeometrySampling = false;
                const frames = window.__historyGeometrySamples ?? [];
                const keys = ['chart', 'svg', 'orders', 'detail'];
                const metrics = ['left', 'top', 'width', 'height'];
                const deltas = [];
                for (const key of keys) {
                    const samples = frames.map(frame => frame[key]).filter(Boolean);
                    if (!samples.length) return `missing=${key}`;
                    for (const metric of metrics) {
                        const values = samples.map(sample => sample[metric]);
                        deltas.push(`${key}.${metric}=${Math.max(...values) - Math.min(...values)}`);
                    }
                }
                const stable = deltas.every(entry => Number(entry.split('=')[1]) <= 1.5);
                return `${stable}|frames=${frames.length}|first=${JSON.stringify(frames[0])}|last=${JSON.stringify(frames.at(-1))}|${deltas.join(',')}`;
            }
        """);
        interactionGeometry.Should().StartWith("true", "filter interaction must keep chart/SVG, order list and detail panel geometry stable throughout loading and the zero-result transition");
        (await drawer.IsVisibleAsync()).Should().BeTrue("desktop detail must remain present when the master filter returns no rows");
        await Page.Locator(".vpp-history-page").ScreenshotAsync(new()
        {
            Path = Path.Combine(detailScreenshotDirectory, "history-zero-result-stable-layout.png"),
            Animations = ScreenshotAnimations.Disabled
        });
        await clearFilters.ClickAsync();
        await Page.Locator(".vpp-history-grid tbody tr").First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await drawer.Locator(".vpp-history-drawer-code").WaitForAsync(new() { State = WaitForSelectorState.Visible });

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");
            await Page.Locator(".vpp-history-page").WaitForAsync();

            var overflow = await Page.EvaluateAsync<bool>("""
                () => document.documentElement.scrollWidth > document.documentElement.clientWidth
            """);
            overflow.Should().BeFalse($"history must not horizontally scroll at {viewport.Width}x{viewport.Height}");

            if (viewport.Width >= 1280)
            {
                var verticalOverflow = await Page.EvaluateAsync<string>("""
                    () => {
                        const body = document.querySelector('.vpp-layout-body');
                        const page = document.querySelector('.vpp-history-page');
                        if (!body || !page) return 'missing';
                        const bodyFits = body.scrollHeight <= body.clientHeight + 1;
                        const pageFits = page.scrollHeight <= page.clientHeight + 1;
                        const documentFits = document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1;
                        return `${bodyFits && pageFits && documentFits}|body=${body.scrollHeight}/${body.clientHeight}|page=${page.scrollHeight}/${page.clientHeight}|document=${document.documentElement.scrollHeight}/${document.documentElement.clientHeight}`;
                    }
                """);
                verticalOverflow.Should().StartWith("true", $"desktop history must keep the outer page fixed and reserve scrolling for the detail grid at {viewport.Width}x{viewport.Height}");
            }

            if (viewport.Width < 900)
            {
                await Page.Locator(".vpp-history-mobile-list").WaitForAsync();
                (await Page.Locator(".vpp-history-drawer").IsVisibleAsync()).Should().BeFalse();
            }
            else
            {
                await Page.Locator(".vpp-history-grid").WaitForAsync();
                var rowHeights = await Page.Locator(".vpp-history-grid tbody tr").EvaluateAllAsync<double[]>(
                    "rows => rows.map(row => row.getBoundingClientRect().height)");
                rowHeights.Should().OnlyContain(height => height > 0);
                rowHeights.Distinct().Count().Should().BeLessThanOrEqualTo(1);

                var tableGeometry = await Page.EvaluateAsync<string>("""
                    () => {
                        const table = document.querySelector('.vpp-history-grid table');
                        const row = table?.querySelector('tbody tr');
                        if (!table || !row) return 'missing';
                        const visible = element => getComputedStyle(element).display !== 'none';
                        const headers = [...table.querySelectorAll('thead th')].filter(visible);
                        const cells = [...row.children].filter(visible);
                        if (headers.length !== cells.length || headers.length < 7) return `count=${headers.length}/${cells.length}`;
                        const headerRects = headers.map(element => element.getBoundingClientRect());
                        const cellRects = cells.map(element => element.getBoundingClientRect());
                        const aligned = headerRects.every((header, index) => {
                            const cell = cellRects[index];
                            return Math.abs(header.left - cell.left) <= 1 && Math.abs(header.width - cell.width) <= 1;
                        });
                        const tableRect = table.getBoundingClientRect();
                        const edgeAligned = Math.abs(headerRects[0].left - tableRect.left) <= 1
                            && Math.abs(headerRects.at(-1).right - tableRect.right) <= 1;
                        const identityGetsPriority = headerRects[2].width > headerRects[0].width
                            && headerRects[2].width > headerRects[1].width;
                        const wideDesktopUsesContentAwareTracks = window.innerWidth < 1600
                            || headerRects.at(-1).width >= headerRects[2].width * 1.25;
                        const textRect = element => {
                            const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);
                            const rects = [];
                            while (walker.nextNode()) {
                                if (!walker.currentNode.textContent.trim()) continue;
                                const range = document.createRange();
                                range.selectNodeContents(walker.currentNode);
                                const rect = range.getBoundingClientRect();
                                if (rect.width > 0) rects.push(rect);
                            }
                            if (!rects.length) return null;
                            const left = Math.min(...rects.map(rect => rect.left));
                            const right = Math.max(...rects.map(rect => rect.right));
                            return { left, right, center: (left + right) / 2 };
                        };
                        const visualRect = element => {
                            const surface = element.querySelector('.vpp-badge, .vpp-history-order-type');
                            if (surface) {
                                const rect = surface.getBoundingClientRect();
                                return { left: rect.left, right: rect.right, center: (rect.left + rect.right) / 2 };
                            }
                            return textRect(element);
                        };
                        const headerTextRects = headers.map(visualRect);
                        const cellTextRects = cells.map(visualRect);
                        const anchors = headers.map(header => header.classList.contains('vpp-history-col-center')
                            ? 'center'
                            : header.classList.contains('vpp-history-col-right') ? 'right' : 'left');
                        const visualDeltas = anchors.map((anchor, index) => {
                            const header = headerTextRects[index];
                            const cell = cellTextRects[index];
                            return header && cell ? Math.abs(header[anchor] - cell[anchor]) : 999;
                        });
                        const visualAligned = visualDeltas.every(delta => delta <= 2);
                        const categoricalVerticalDeltas = [3, 4].map(index => {
                            const surface = cells[index]?.querySelector('.vpp-history-order-type, .vpp-badge');
                            if (!surface) return 999;
                            const surfaceRect = surface.getBoundingClientRect();
                            const cellRect = cellRects[index];
                            return Math.abs((surfaceRect.top + surfaceRect.height / 2) - (cellRect.top + cellRect.height / 2));
                        });
                        const categoricalCentred = categoricalVerticalDeltas.every(delta => delta <= 1);
                        const headerLabels = headers.map(header => header.textContent.trim());
                        const fullDesktopColumns = window.innerWidth < 1100
                            || (headers.length === 9 && headerLabels.includes('Ngày gửi') && headerLabels.includes('Ghi chú'));
                        const headersFullyVisible = window.innerWidth < 1100 || headers.every(header => {
                            const title = header.querySelector('.rz-column-title-content, .rz-column-title') ?? header;
                            return title.scrollWidth <= title.clientWidth + 1;
                        });
                        const describe = elements => elements.map((element, index) => `${index}:${element.className}|${element.textContent.trim()}`).join(';');
                        return `${aligned && edgeAligned && identityGetsPriority && wideDesktopUsesContentAwareTracks && visualAligned && categoricalCentred && fullDesktopColumns && headersFullyVisible}|aligned=${aligned}|edges=${edgeAligned}|identity=${identityGetsPriority}|contentAware=${wideDesktopUsesContentAwareTracks}|visual=${visualAligned}|vertical=${categoricalCentred}|full=${fullDesktopColumns}|headers=${headersFullyVisible}|deltas=${visualDeltas.map(value => Math.round(value * 10) / 10).join(',')}|verticalDeltas=${categoricalVerticalDeltas.map(value => Math.round(value * 10) / 10).join(',')}|h=${describe(headers)}|c=${describe(cells)}`;
                    }
                """);

                var scrollbarGutterGeometry = await Page.EvaluateAsync<string>("""
                    () => {
                        const surface = document.querySelector('.vpp-history-grid .rz-data-grid-data');
                        const row = surface?.querySelector('tbody tr');
                        if (!surface || !row) return 'missing';
                        const hasOverflow = surface.scrollHeight > surface.clientHeight + 1;
                        const classMatches = surface.classList.contains('has-vertical-overflow') === hasOverflow;
                        const rightGap = Math.abs(surface.getBoundingClientRect().right - row.getBoundingClientRect().right);
                        const noUnusedStrip = hasOverflow || rightGap <= 2;
                        return `${classMatches && noUnusedStrip}|overflow=${hasOverflow}|class=${surface.classList.contains('has-vertical-overflow')}|gap=${Math.round(rightGap * 10) / 10}`;
                    }
                """);
                scrollbarGutterGeometry.Should().StartWith("true", "short lists must not reserve a false scrollbar strip at the right edge");

                if (viewport.Width >= 1366)
                {
                    var screenshotDirectory = Path.Combine(Path.GetTempPath(), "gtas-vpp-history-visual");
                    Directory.CreateDirectory(screenshotDirectory);
                    await Page.Locator(".vpp-history-orders-card").ScreenshotAsync(new()
                    {
                        Path = Path.Combine(screenshotDirectory, $"history-orders-{viewport.Width}x{viewport.Height}.png"),
                        Animations = ScreenshotAnimations.Disabled
                    });
                    if (await drawer.IsVisibleAsync())
                    {
                        await drawer.ScreenshotAsync(new()
                        {
                            Path = Path.Combine(screenshotDirectory, $"history-detail-{viewport.Width}x{viewport.Height}.png"),
                            Animations = ScreenshotAnimations.Disabled
                        });
                    }
                }

                if (viewport.Width == 1920 && await drawer.IsVisibleAsync())
                {
                    var detailDesktopHeaders = await drawer.Locator(".vpp-history-detail-grid-header-cell").EvaluateAllAsync<string>("""
                        headers => {
                            const labels = headers;
                            const complete = labels.every(label => label.scrollWidth <= label.clientWidth + 1);
                            return `${complete}|${labels.map(label => `${label.textContent.trim()}:${label.clientWidth}/${label.scrollWidth}`).join(',')}`;
                        }
                    """);
                    detailDesktopHeaders.Should().StartWith("true", "24-inch desktop must show every detail-table header in full without wrapping or horizontal scroll");
                }

                tableGeometry.Should().StartWith("true", $"history columns must share outer tracks and visual text axes at {viewport.Width}x{viewport.Height}");

                var clearFilterGeometry = await Page.EvaluateAsync<string>("""
                    () => {
                        const button = document.querySelector('.vpp-history-orders-card .vpp-history-clear');
                        if (!(button instanceof HTMLElement)) return 'missing';
                        const style = getComputedStyle(button);
                        const border = style.borderTopColor.match(/rgba?\([^)]*\)/)?.[0] ?? '';
                        const alpha = Number(border.match(/rgba?\([^,]+,[^,]+,[^,]+,\s*([\d.]+)\)/)?.[1] ?? '1');
                        const ok = !button.hasAttribute('disabled')
                            && !button.classList.contains('is-active')
                            && style.borderTopWidth === '1px'
                            && alpha >= .1;
                        return `${ok}|border=${style.borderTopWidth}|alpha=${alpha}`;
                    }
                """);
                clearFilterGeometry.Should().StartWith("true", "default clear-filter control must retain a visible neutral border");

                if (viewport.Width >= 1280)
                {
                    await Page.Locator(".vpp-history-drawer").WaitForAsync();
                    var layoutGeometry = await Page.EvaluateAsync<string>("""
                        () => {
                            const kpis = document.querySelector('.vpp-history-kpis');
                            const chart = document.querySelector('.vpp-history-chart-card');
                            const orders = document.querySelector('.vpp-history-orders-card');
                            const list = document.querySelector('.vpp-history-scope');
                            const detail = document.querySelector('.vpp-history-drawer');
                            const grid = document.querySelector('.vpp-history-grid table');
                            const gridSurface = document.querySelector('.vpp-history-grid-wrap');
                            const card = document.querySelector('.vpp-history-orders-card');
                            const page = document.querySelector('.vpp-history-page');
                            const orderScroll = document.querySelector('.vpp-history-grid .rz-data-grid-data');
                            const detailScroll = document.querySelector('.vpp-history-detail-grid .rz-data-grid-data');
                            if (!kpis || !chart || !orders || !list || !detail || !grid || !gridSurface || !card || !page || !orderScroll || !detailScroll) return 'missing';
                            const gapA = chart.getBoundingClientRect().top - kpis.getBoundingClientRect().bottom;
                            const gapB = orders.getBoundingClientRect().top - chart.getBoundingClientRect().bottom;
                            const tableWidth = gridSurface.getBoundingClientRect().width;
                            const cardWidth = card.getBoundingClientRect().width;
                            const pageBottom = page.getBoundingClientRect().bottom;
                            const ordersBottomDelta = Math.abs(pageBottom - orders.getBoundingClientRect().bottom);
                            const detailBottomDelta = Math.abs(pageBottom - detail.getBoundingClientRect().bottom);
                            const sharedBottomDelta = Math.abs(orders.getBoundingClientRect().bottom - detail.getBoundingClientRect().bottom);
                            const ok = Math.abs(list.getBoundingClientRect().top - detail.getBoundingClientRect().top) <= 1
                                && gapA >= 0 && gapA <= 20
                                && gapB >= 0 && gapB <= 20
                                && Math.abs(tableWidth - cardWidth) <= 2
                                && ordersBottomDelta <= 2
                                && detailBottomDelta <= 2
                                && sharedBottomDelta <= 2
                                && ['auto', 'scroll'].includes(getComputedStyle(orderScroll).overflowY)
                                && ['auto', 'scroll'].includes(getComputedStyle(detailScroll).overflowY);
                            return `${ok}|top=${Math.round(list.getBoundingClientRect().top - detail.getBoundingClientRect().top)}|gapA=${Math.round(gapA)}|gapB=${Math.round(gapB)}|table=${Math.round(tableWidth)}|card=${Math.round(cardWidth)}|bottom=${Math.round(ordersBottomDelta)}/${Math.round(detailBottomDelta)}/${Math.round(sharedBottomDelta)}`;
                        }
                    """);
                    layoutGeometry.Should().StartWith("true", $"desktop history must keep compact left-column rhythm while both internal-scroll data surfaces share the viewport bottom at {viewport.Width}x{viewport.Height}");
                }
            }
        }
    }
}
