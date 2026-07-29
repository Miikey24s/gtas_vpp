using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class F4OwnerReviewTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task AdditionalOrder_DirectQuantityAndReviewRemainUsable()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard/order-create?isAdditional=True");
        await WaitForRowsAsync(".vpp-order-builder-grid", ".vpp-order-builder-virtual-row");

        var availableAction = Page.Locator(".vpp-order-builder-row-action:not(.is-remove)").First;
        await availableAction.ClickAsync();
        await Page.Locator(".vpp-order-builder-row-action:not(.is-remove)").First.ClickAsync();
        await Page.Locator(".vpp-order-draft-item").Nth(1).WaitForAsync();

        var quantityInput = Page.Locator(".vpp-order-draft-item .vpp-order-quantity-input").First;
        await quantityInput.FillAsync("4");
        await quantityInput.PressAsync("Tab");
        (await quantityInput.InputValueAsync()).Should().Be("4");

        var draftColumns = await Page.Locator(".vpp-order-draft").EvaluateAsync<string>("""
            draft => {
                const header = draft.querySelector('.vpp-order-draft-columns');
                const row = draft.querySelector('.vpp-order-draft-item-main');
                const input = draft.querySelector('.vpp-order-quantity-input');
                if (!header || !row || !input) return 'missing';
                const headerColumns = getComputedStyle(header).gridTemplateColumns;
                const rowColumns = getComputedStyle(row).gridTemplateColumns;
                return `${headerColumns === rowColumns && input.getBoundingClientRect().width >= 40}`
                    + `|header=${headerColumns}|row=${rowColumns}|input=${input.getBoundingClientRect().width}`;
            }
            """);
        draftColumns.Should().StartWith("true", "draft headers, rows and direct quantity input must share the same column tracks");
        await CaptureAsync("t001-additional-order-draft-1920x1080.png");

        var continueButton = Page.GetByRole(AriaRole.Button, new() { Name = "Tiếp tục" });
        (await continueButton.IsEnabledAsync()).Should().BeTrue("a missing supplement reason is validated visibly before submit instead of silently disabling navigation");
        await continueButton.ClickAsync();

        var reviewPanel = Page.Locator(".vpp-order-review-panel");
        await reviewPanel.WaitForAsync();
        await Page.Locator(".vpp-order-review-warning").WaitForAsync();
        await Page.Locator(".vpp-order-review-grid tbody tr").First.WaitForAsync();
        var reviewGeometry = await reviewPanel.EvaluateAsync<string>("""
            panel => {
                const frame = panel.querySelector('.vpp-order-review-grid-frame');
                const header = panel.querySelector('.vpp-order-review-header');
                const footer = panel.querySelector('.vpp-order-builder-data-footer');
                if (!frame || !header || !footer) return 'missing';
                const bounded = document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1;
                return `${bounded && panel.getBoundingClientRect().height > 500 && frame.getBoundingClientRect().height > 250}`
                    + `|panel=${panel.getBoundingClientRect().height}|grid=${frame.getBoundingClientRect().height}`;
            }
            """);
        reviewGeometry.Should().StartWith("true", "the review step must be a bounded full-height data surface instead of unstyled document content");
        await CaptureAsync("t001-additional-order-review-1920x1080.png");
    }

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
        await WaitForRowsAsync(".vpp-order-builder-grid", ".vpp-order-builder-virtual-row");
        var activeHeaderTab = Page.Locator(".vpp-header-tab.is-active");
        await activeHeaderTab.WaitForAsync();
        (await activeHeaderTab.InnerTextAsync()).Trim().Should().Be("Đơn hàng của tôi");
        (await Page.Locator(".vpp-header-breadcrumb, .vpp-header-breadcrumb-ancestor, .vpp-header-breadcrumb-leaf").CountAsync()).Should().Be(0);
        (await Page.Locator(".vpp-order-create-page .vpp-workflow-step").CountAsync()).Should().Be(2);
        var workflowStepperGeometry = await Page.Locator(".vpp-order-create-page .vpp-workflow-stepper").EvaluateAsync<string>("""
            stepper => {
                const rect = stepper.getBoundingClientRect();
                const button = stepper.querySelector('button');
                const buttonRect = button?.getBoundingClientRect();
                const style = getComputedStyle(stepper);
                const fullyContainsButton = !!buttonRect
                    && buttonRect.top >= rect.top - .5
                    && buttonRect.bottom <= rect.bottom + .5;
                return `${rect.height >= 34 && fullyContainsButton}`
                    + `|stepper=${rect.height}|button=${buttonRect?.height ?? 0}`
                    + `|flex=${style.flexGrow}/${style.flexShrink}/${style.flexBasis}`
                    + `|overflow=${style.overflowX}/${style.overflowY}`;
            }
            """);
        workflowStepperGeometry.Should().StartWith("true", "the compact workflow stepper must keep its full visual height inside the full-screen order wizard");
        await AssertBoundedPageAndSharedFilterAsync("order-create", ".vpp-order-create-page");
        var visibleRowNumbers = await Page.Locator(".vpp-order-builder-virtual-row .vpp-order-builder-virtual-cell:first-child")
            .AllTextContentsAsync();
        visibleRowNumbers.Take(3).Should().Equal("1", "2", "3");
        var firstCodeTrigger = Page.Locator(".vpp-order-builder-grid .vpp-cell-value-popover-trigger").First;
        await firstCodeTrigger.ClickAsync();
        await Page.Locator(".vpp-order-builder-grid .vpp-cell-value-popover-panel:visible").First.WaitForAsync();
        await CaptureAsync("ds3-order-create-code-popover-1920x1080.png");
        await firstCodeTrigger.ClickAsync();

        var productRequestsDuringScroll = new List<string>();
        Page.Request += (_, request) =>
        {
            if (request.Url.Contains("/api/VPPRequest/products", StringComparison.OrdinalIgnoreCase))
            {
                productRequestsDuringScroll.Add(request.Url);
            }
        };
        var mountedRowsBeforeScroll = await Page.Locator(".vpp-order-builder-virtual-row").CountAsync();
        var scrollPaintContract = await Page.Locator(".vpp-order-builder-grid").EvaluateAsync<string>("""
            async grid => {
                const scroller = grid;
                if (!scroller) throw new Error('Order catalog virtual scroller was not found.');
                const header = grid.querySelector('.vpp-order-builder-virtual-header');
                const firstRow = grid.querySelector('.vpp-order-builder-virtual-row');
                if (!header || !firstRow) throw new Error('Order catalog header or row was not found.');
                const rowHeight = Math.max(1, firstRow.getBoundingClientRect().height);
                const maxScroll = Math.max(0, scroller.scrollHeight - scroller.clientHeight);
                const positions = [0, .35, .85, 1.35, 3.15, 7.4, 12.65, 18.2]
                    .map(multiplier => Math.min(maxScroll, rowHeight * multiplier));
                const failures = [];
                for (const position of positions) {
                    scroller.scrollTop = position;
                    scroller.dispatchEvent(new Event('scroll', { bubbles: true }));
                    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                    const headerRect = header.getBoundingClientRect();
                    const sampleY = headerRect.top + Math.max(1, headerRect.height / 2);
                    const sampleXs = [.05, .25, .55, .82, .96]
                        .map(ratio => headerRect.left + headerRect.width * ratio);
                    const hits = sampleXs.map(x => document.elementFromPoint(x, sampleY));
                    const headerOwnsPaint = hits.every(hit => hit?.closest('.vpp-order-builder-virtual-header') === header);
                    if (!headerOwnsPaint) {
                        failures.push(`${Math.round(position)}:${hits.map(hit => `${hit?.tagName ?? 'none'}.${hit?.className ?? ''}`).join('>')}`);
                    }
                }
                const headerStyle = getComputedStyle(header);
                const body = grid.querySelector('.vpp-order-builder-virtual-body');
                const bodyStyle = body ? getComputedStyle(body) : null;
                return `${failures.length === 0}|failures=${failures.join(',')}|positions=${positions.map(Math.round).join(',')}`
                    + `|header=${headerStyle.position}/${headerStyle.zIndex}/${headerStyle.transform}`
                    + `|body=${bodyStyle?.position}/${bodyStyle?.zIndex}/${bodyStyle?.transform}`;
            }
            """);
        scrollPaintContract.Should().StartWith("true", "the sticky header must own the paint layer throughout virtual scrolling without per-row JavaScript guards");
        var mountedRowsAfterScroll = await Page.Locator(".vpp-order-builder-virtual-row").CountAsync();
        var virtualizationGeometry = await Page.Locator(".vpp-order-create-page").EvaluateAsync<string>("""
            root => {
                const selectors = [
                    '.vpp-wizard-stage', '.wizard-step-products', '.vpp-order-builder',
                    '.vpp-split-editor-workspace', '.vpp-order-builder-catalog',
                    '.vpp-order-builder-data-surface', '.vpp-data-surface-body',
                    '.vpp-order-builder-grid-frame', '.vpp-order-builder-grid'
                ];
                return selectors.map(selector => {
                    const element = root.querySelector(selector);
                    if (!element) return `${selector}=missing`;
                    const rect = element.getBoundingClientRect();
                    const style = getComputedStyle(element);
                    return `${selector}=${rect.height}/${element.clientHeight}/${element.scrollHeight}/${style.overflowY}`;
                }).join('|');
            }
            """);
        var headerAndViewportChrome = await Page.Locator(".vpp-order-builder-grid").EvaluateAsync<string>("""
            grid => {
                const scroller = grid;
                const header = grid.querySelector('.vpp-order-builder-virtual-header');
                if (!scroller || !header) return 'missing';
                const scrollerStyle = getComputedStyle(scroller);
                const headerStyle = getComputedStyle(header);
                const headerRect = header.getBoundingClientRect();
                const topElement = document.elementFromPoint(
                    headerRect.left + Math.min(12, headerRect.width / 2),
                    headerRect.top + headerRect.height / 2);
                const radii = [
                    scrollerStyle.borderTopLeftRadius,
                    scrollerStyle.borderTopRightRadius,
                    scrollerStyle.borderBottomRightRadius,
                    scrollerStyle.borderBottomLeftRadius
                ];
                const squareViewport = radii.every(radius => Number.parseFloat(radius) === 0);
                const opaqueHeader = headerStyle.backgroundColor !== 'rgba(0, 0, 0, 0)'
                    && headerStyle.backgroundColor !== 'transparent';
                const headerOwnsStack = headerStyle.position === 'sticky'
                    && Number.parseInt(headerStyle.zIndex || '0', 10) > 0;
                const headerOwnsHitArea = topElement?.closest('.vpp-order-builder-virtual-header') === header;
                const headerBottom = headerRect.bottom;
                const overlappingCodes = [...grid.querySelectorAll('.vpp-cell-value-popover-trigger')]
                    .filter(trigger => {
                        const rect = trigger.getBoundingClientRect();
                        return rect.top < headerBottom && rect.bottom > headerRect.top;
                    });
                const overlapProtected = overlappingCodes.every(trigger => {
                    const rect = trigger.getBoundingClientRect();
                    const hit = document.elementFromPoint(
                        rect.left + Math.min(8, rect.width / 2),
                        Math.min(headerBottom - 1, rect.top + rect.height / 2));
                    return hit?.closest('.vpp-order-builder-virtual-header') === header;
                });
                const firstOverlap = overlappingCodes[0];
                const firstOverlapRect = firstOverlap?.getBoundingClientRect();
                const firstOverlapHit = firstOverlapRect
                    ? document.elementFromPoint(firstOverlapRect.left + 8, Math.min(headerBottom - 1, firstOverlapRect.top + firstOverlapRect.height / 2))
                    : null;
                const hasLegacyRowGuard = !!grid.querySelector('.vpp-virtual-row-under-header');
                return `${squareViewport && opaqueHeader && headerOwnsStack && headerOwnsHitArea && overlapProtected && !hasLegacyRowGuard}`
                    + `|radii=${radii.join(',')}|background=${headerStyle.backgroundColor}`
                    + `|header=${headerStyle.position}/${headerStyle.zIndex}`
                    + `|hit=${topElement?.tagName ?? 'none'}|overlap=${overlappingCodes.length}|overlapHit=${firstOverlapHit?.tagName ?? 'none'}`;
            }
            """);
        productRequestsDuringScroll.Should().BeEmpty("client-snapshot virtualization must not call the product API while scrolling");
        mountedRowsBeforeScroll.Should().BeLessThanOrEqualTo(40, virtualizationGeometry);
        mountedRowsAfterScroll.Should().BeLessThanOrEqualTo(40, virtualizationGeometry);
        headerAndViewportChrome.Should().StartWith("true", "virtual rows must stay visually below an opaque header and the inner scroll viewport must join the footer without rounded corners");
        await CaptureAsync("f4-review-order-create-1920x1080.png");
    }

    [Fact]
    public async Task HistoryDetailSearch_MatchesTheOrderListSearchMotif()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");

        var listSearch = Page.Locator(".vpp-history-orders-card .vpp-filter-search");
        var detailSearch = Page.Locator(".vpp-history-detail-toolbar .vpp-filter-search");
        await listSearch.WaitForAsync(new() { Timeout = 60_000 });
        await detailSearch.WaitForAsync(new() { Timeout = 60_000 });

        var parity = await Page.EvaluateAsync<double[][]>("""
            () => {
                const list = document.querySelector('.vpp-history-orders-card .vpp-filter-search');
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
            () => ['.vpp-history-orders-card .vpp-filter-search', '.vpp-history-detail-toolbar .vpp-filter-search']
                .map(selector => {
                    const style = getComputedStyle(document.querySelector(selector));
                    return `${style.backgroundColor}|${style.boxShadow}`;
                })
            """);
        visualParity[1].Should().Be(visualParity[0]);

        await listSearch.Locator("input").FillAsync("udad");
        var activeSearchChrome = await listSearch.EvaluateAsync<string>("""
            search => {
                const input = search.querySelector('input');
                const searchStyle = getComputedStyle(search);
                const inputStyle = getComputedStyle(input);
                const borderIsContinuous = ['borderTopColor', 'borderRightColor', 'borderBottomColor', 'borderLeftColor']
                    .map(property => searchStyle[property])
                    .every(color => color === searchStyle.borderTopColor);
                const inputHasNoBorder = ['borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth']
                    .map(property => inputStyle[property])
                    .every(width => width === '0px');
                return `${borderIsContinuous && inputHasNoBorder && inputStyle.backgroundColor === 'rgba(0, 0, 0, 0)' && inputStyle.outlineStyle === 'none'}|border=${searchStyle.borderTopColor}|inputBorder=${inputStyle.borderTopWidth}|inputBg=${inputStyle.backgroundColor}|inputOutline=${inputStyle.outlineStyle}`;
            }
        """);
        activeSearchChrome.Should().StartWith("true", "the search wrapper must own one continuous border without an inner input ring or opaque fill");
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
                        const webkitScrollbar = getComputedStyle(element, '::-webkit-scrollbar');
                        return style.scrollbarGutter === 'auto'
                            && (!style.scrollbarWidth || style.scrollbarWidth === 'auto')
                            && webkitScrollbar.width === 'auto';
                    });
                const radzenScrollbarOptOut = document.body.classList.contains('rz-default-scrollbars');
                return `${documentDoesNotScroll && sameHeight && nativeScrollbar && radzenScrollbarOptOut}|document=${documentDoesNotScroll}|height=${sameHeight}|native=${nativeScrollbar}|optout=${radzenScrollbarOptOut}`;
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

    private async Task WaitForRowsAsync(string gridSelector, string rowSelector = "tbody tr")
    {
        await Page.WaitForFunctionAsync(
            $"() => document.querySelector('{gridSelector}')?.querySelectorAll('{rowSelector}').length > 1",
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
