using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Library;

public class LibraryGridScrollTests : TestBase
{
    [Fact]
    public async Task Category_Grid_Uses_Page_Scroll_Without_Header_Overlap()
    {
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}library?tab=1");

        var grid = Page.Locator(".library-share-grid:visible");
        var gridData = grid.Locator(".rz-data-grid-data");
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.WaitForTimeoutAsync(500);

        var gridChrome = await grid.EvaluateAsync<string[]>("""
            element => {
                const style = getComputedStyle(element);
                return [style.borderTopWidth, style.borderRadius, style.boxShadow, style.backgroundColor];
            }
            """);
        gridChrome.Should().Equal("0px", "0px", "none", "rgba(0, 0, 0, 0)");

        var edgeOffsets = await grid.EvaluateAsync<double[]>("""
            element => {
                const panel = element.closest('.rz-tabview-panel');
                if (!panel) {
                    throw new Error('Category grid tab panel was not rendered.');
                }

                const gridRect = element.getBoundingClientRect();
                const panelRect = panel.getBoundingClientRect();
                return [gridRect.left - panelRect.left, panelRect.right - gridRect.right];
            }
            """);
        edgeOffsets[0].Should().BeApproximately(0, 0.5);
        edgeOffsets[1].Should().BeApproximately(0, 0.5);

        var toolbarAlignment = await grid.EvaluateAsync<double[]>("""
            element => {
                const header = element.querySelector('.rz-group-header');
                const customHeader = element.querySelector('.rz-custom-header');
                const picker = element.querySelector('.vpp-column-picker-trigger');
                if (!header || !customHeader || !picker) {
                    throw new Error('Category toolbar or column picker was not rendered.');
                }

                const customRect = customHeader.getBoundingClientRect();
                const pickerRect = picker.getBoundingClientRect();
                return [
                    header.getBoundingClientRect().height,
                    Math.abs((customRect.top + customRect.height / 2) - (pickerRect.top + pickerRect.height / 2))
                ];
            }
            """);
        toolbarAlignment[0].Should().BeLessThan(60);
        toolbarAlignment[1].Should().BeLessThan(5);

        var createButton = grid.Locator(".vpp-library-primary-action");
        var reloadButton = grid.Locator(".vpp-library-refresh-action");
        await createButton.WaitForAsync();
        await reloadButton.WaitForAsync();
        (await createButton.InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        (await reloadButton.InnerTextAsync()).Should().NotBeNullOrWhiteSpace();
        (await reloadButton.GetAttributeAsync("aria-label")).Should().NotBeNullOrWhiteSpace();

        var actionSizes = await grid.EvaluateAsync<double[]>("""
            element => {
                const create = element.querySelector('.vpp-library-primary-action');
                const reload = element.querySelector('.vpp-library-refresh-action');
                if (!create || !reload) {
                    throw new Error('Library actions were not rendered.');
                }

                return [
                    create.getBoundingClientRect().height,
                    reload.getBoundingClientRect().height,
                    reload.getBoundingClientRect().width
                ];
            }
            """);
        actionSizes[0].Should().BeApproximately(34, 1);
        actionSizes[1].Should().BeApproximately(34, 1);
        actionSizes[2].Should().BeGreaterThan(70);

        var actionAppearance = await reloadButton.EvaluateAsync<string[]>("""
            element => {
                const icon = element.querySelector('.rzi');
                const style = getComputedStyle(element);
                const iconStyle = icon ? getComputedStyle(icon) : null;
                return [style.opacity, style.color, iconStyle?.opacity ?? '0'];
            }
            """);
        actionAppearance[0].Should().Be("1");
        actionAppearance[2].Should().Be("1");

        var primaryTabs = Page.Locator(".vpp-admin-tabs > .rz-tabview-nav");
        var primaryTabPosition = await primaryTabs.EvaluateAsync<string[]>("""
            element => [getComputedStyle(element).position, getComputedStyle(element).top]
            """);
        primaryTabPosition.Should().Equal("sticky", "-7px");

        var deletedCells = grid.Locator(".vpp-admin-is-deleted-cell");
        await deletedCells.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 15000
        });
        (await deletedCells.CountAsync()).Should().BeGreaterThan(0);
        (await deletedCells.Locator(".vpp-admin-status-badge").CountAsync()).Should().Be(0);

        var hasInternalVerticalScroll = await gridData.EvaluateAsync<bool>(
            "element => element.scrollHeight > element.clientHeight + 1");
        hasInternalVerticalScroll.Should().BeFalse();

        var contentScroller = Page.Locator(".vpp-layout-body");
        await contentScroller.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await Page.WaitForTimeoutAsync(100);

        var stickyPrimaryGap = await Page.EvaluateAsync<double>("""
            () => {
                const scroller = document.querySelector('.vpp-layout-body');
                const tabs = document.querySelector('.vpp-admin-tabs > .rz-tabview-nav');
                if (!scroller || !tabs) {
                    throw new Error('Library scroll container or primary tabs were not rendered.');
                }

                return tabs.getBoundingClientRect().top - scroller.getBoundingClientRect().top;
            }
            """);
        stickyPrimaryGap.Should().BeInRange(-1, 8);

        var headerRowGap = await Page.EvaluateAsync<double>("""
            () => {
                const grid = document.querySelector('.library-share-grid');
                const header = grid?.querySelector('thead');
                const firstRow = grid?.querySelector('tbody > tr');
                if (!header || !firstRow) {
                    throw new Error('Category grid header or first row was not rendered.');
                }

                return firstRow.getBoundingClientRect().top - header.getBoundingClientRect().bottom;
            }
            """);

        headerRowGap.Should().BeApproximately(0, 0.5);
    }

    [Fact]
    public async Task Pricing_Grids_Use_Content_Height_And_Page_Scroll()
    {
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists");

        var priceListGrid = Page.Locator(".vpp-price-list-workspace .vpp-admin-page-grid");
        await priceListGrid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.WaitForTimeoutAsync(500);

        (await Page.Locator(".vpp-price-list-workspace .vpp-admin-section-title").CountAsync()).Should().Be(0);

        var secondaryTabHostPadding = await Page.EvaluateAsync<double?>("""
            () => {
                const secondaryTabs = document.querySelector('.vpp-secondary-tabs');
                if (!secondaryTabs) {
                    return null;
                }

                const hostPanel = secondaryTabs.parentElement?.closest('.rz-tabview-panel');
                if (!hostPanel) {
                    throw new Error('Pricing tab host panel was not rendered.');
                }

                return parseFloat(getComputedStyle(hostPanel).paddingTop);
            }
            """);
        if (secondaryTabHostPadding.HasValue)
        {
            secondaryTabHostPadding.Value.Should().BeLessThanOrEqualTo(0.5);
        }

        var secondaryTabs = Page.Locator(".vpp-secondary-tabs > .rz-tabview-nav");
        await secondaryTabs.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var secondaryTabPosition = await secondaryTabs.EvaluateAsync<string[]>("""
            element => [getComputedStyle(element).position, getComputedStyle(element).top]
            """);
        secondaryTabPosition.Should().Equal("sticky", "41px");

        var priceListLayout = await priceListGrid.EvaluateAsync<double[]>("""
            element => {
                const pager = element.querySelector('.rz-paginator, .rz-pager');
                return [
                    element.getBoundingClientRect().height,
                    parseFloat(getComputedStyle(element).borderTopWidth),
                    pager && getComputedStyle(pager).display !== 'none' ? 1 : 0
                ];
            }
            """);
        priceListLayout[0].Should().BeLessThan(300);
        priceListLayout[1].Should().Be(0);
        priceListLayout[2].Should().Be(0);

        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=prices");
        var priceGrid = Page.Locator(".vpp-price-grid");
        await priceGrid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.WaitForTimeoutAsync(500);

        var nestedVerticalScrollers = await priceGrid.EvaluateAsync<int>("""
            element => [...element.querySelectorAll('*')].filter(child => {
                const overflowY = getComputedStyle(child).overflowY;
                return (overflowY === 'auto' || overflowY === 'scroll')
                    && child.scrollHeight > child.clientHeight + 1;
            }).length
            """);
        nestedVerticalScrollers.Should().Be(0);

        await Page.Locator(".vpp-layout-body").EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await Page.WaitForTimeoutAsync(100);
        var stickyTabGaps = await Page.EvaluateAsync<double[]>("""
            () => {
                const scroller = document.querySelector('.vpp-layout-body');
                const primary = document.querySelector('.vpp-admin-tabs > .rz-tabview-nav');
                const secondary = document.querySelector('.vpp-secondary-tabs > .rz-tabview-nav');
                if (!scroller || !primary || !secondary) {
                    throw new Error('Pricing sticky tab stack was not rendered.');
                }

                const scrollerTop = scroller.getBoundingClientRect().top;
                return [
                    primary.getBoundingClientRect().top - scrollerTop,
                    secondary.getBoundingClientRect().top - scrollerTop
                ];
            }
            """);
        stickyTabGaps[0].Should().BeApproximately(0, 1);
        stickyTabGaps[1].Should().BeApproximately(48, 1);
    }

    [Fact]
    public async Task Class_Definitions_Use_Compact_Master_Detail_Layout()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=0");

        var split = Page.Locator(".vpp-admin-class-split");
        var master = Page.Locator(".vpp-class-master-grid");
        var detail = Page.Locator(".vpp-class-detail-grid");
        await split.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await master.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await detail.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await master.Locator(".rz-data-grid-data").WaitForAsync();
        await master.Locator(".vpp-class-master-cell").First.WaitForAsync();

        var layout = await Page.EvaluateAsync<double[]>("""
            () => {
                const split = document.querySelector('.vpp-admin-class-split');
                const master = document.querySelector('.vpp-class-master-grid');
                const detail = document.querySelector('.vpp-class-detail-grid');
                const masterData = master?.querySelector('.rz-data-grid-data');
                const masterTable = master?.querySelector('table');
                if (!split || !master || !detail || !masterData || !masterTable) {
                    throw new Error('Class master-detail layout was not rendered.');
                }

                return [
                    getComputedStyle(split).flexDirection === 'row' ? 1 : 0,
                    master.getBoundingClientRect().right <= detail.getBoundingClientRect().left ? 1 : 0,
                    masterTable.scrollWidth - masterData.clientWidth
                ];
            }
            """);
        layout[0].Should().Be(1);
        layout[1].Should().Be(1);
        layout[2].Should().BeLessThanOrEqualTo(1);

        var pickerTrigger = master.Locator(".vpp-column-picker-trigger");
        (await pickerTrigger.CountAsync()).Should().Be(1);
        var masterChrome = await master.EvaluateAsync<string[]>("""
            element => {
                const header = element.querySelector('thead');
                const row = element.querySelector('tbody > tr');
                const cell = element.querySelector('.vpp-class-master-cell');
                if (!header || !row || !cell) {
                    throw new Error('Class master list chrome was not rendered.');
                }

                return [
                    getComputedStyle(header).display,
                    getComputedStyle(cell).display,
                    getComputedStyle(cell).gap
                ];
            }
            """);
        masterChrome[0].Should().NotBe("none");
        masterChrome[1].Should().Be("grid");

        var initialVisibleCount = int.Parse(await pickerTrigger.Locator(".vpp-column-picker-count").InnerTextAsync());
        initialVisibleCount.Should().BeGreaterThan(0);
        await pickerTrigger.ClickAsync();
        var pickerPanel = Page.Locator(".vpp-column-picker-popover:popover-open");
        await pickerPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var pickerChrome = await pickerPanel.EvaluateAsync<double[]>("""
            element => {
                const header = element.querySelector('.vpp-column-picker-heading');
                const item = element.querySelector('.vpp-column-picker-option');
                if (!header || !item) {
                    throw new Error('Column picker panel chrome was not rendered.');
                }

                return [
                    element.getBoundingClientRect().width,
                    header.getBoundingClientRect().height,
                    item.getBoundingClientRect().height,
                    parseFloat(getComputedStyle(element).borderRadius),
                    getComputedStyle(element).position === 'fixed' ? 1 : 0,
                    element.getBoundingClientRect().right <= window.innerWidth ? 1 : 0,
                    element.getBoundingClientRect().bottom <= window.innerHeight ? 1 : 0
                ];
            }
            """);
        pickerChrome[0].Should().BeApproximately(320, 2);
        pickerChrome[1].Should().BeGreaterThanOrEqualTo(50);
        pickerChrome[2].Should().BeGreaterThanOrEqualTo(38);
        pickerChrome[3].Should().BeGreaterThanOrEqualTo(8);
        pickerChrome[4].Should().Be(1);
        pickerChrome[5].Should().Be(1);
        pickerChrome[6].Should().Be(1);

        var search = pickerPanel.Locator(".vpp-column-picker-search input");
        await search.FillAsync("Class Code");
        var filteredOptions = pickerPanel.Locator(".vpp-column-picker-option:visible");
        (await filteredOptions.CountAsync()).Should().Be(1);
        (await filteredOptions.InnerTextAsync()).Should().Contain("Class Code");
        await search.FillAsync(string.Empty);

        var classCodeOption = pickerPanel.Locator(".vpp-column-picker-option").Filter(new LocatorFilterOptions { HasText = "Class Code" });
        (await classCodeOption.CountAsync()).Should().Be(1);
        await classCodeOption.Locator("input[type='checkbox']").SetCheckedAsync(true, new LocatorSetCheckedOptions { Force = true });
        await Page.WaitForTimeoutAsync(300);
        int.Parse(await pickerTrigger.Locator(".vpp-column-picker-count").InnerTextAsync()).Should().Be(initialVisibleCount + 1);
        (await master.Locator("thead th").Filter(new LocatorFilterOptions { HasText = "Class Code" }).CountAsync()).Should().Be(1);

        if (!await pickerPanel.IsVisibleAsync())
        {
            await pickerTrigger.ClickAsync();
            await pickerPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }

        await pickerPanel.Locator(".vpp-column-picker-reset").ClickAsync();
        await Page.WaitForTimeoutAsync(300);
        int.Parse(await pickerTrigger.Locator(".vpp-column-picker-count").InnerTextAsync()).Should().Be(initialVisibleCount);
        (await master.Locator("thead th").Filter(new LocatorFilterOptions { HasText = "Class Code" }).CountAsync()).Should().Be(0);

        await Page.SetViewportSizeAsync(1200, 768);
        await Page.WaitForTimeoutAsync(200);
        var compactDirection = await split.EvaluateAsync<string>("element => getComputedStyle(element).flexDirection");
        compactDirection.Should().Be("column");
    }
}
