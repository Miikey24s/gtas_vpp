using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ProductCatalogTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Employee_SearchesCatalogByAllItemNameTerms_WithOrWithoutVietnameseSpacing()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var grid = Page.Locator(".vpp-catalog-grid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

        var searchInput = Page.Locator(".vpp-catalog-filter-group .vpp-filter-search input");
        foreach (var keyword in new[] { "but long", "butlong", "bút-lông" })
        {
            await searchInput.FillAsync(keyword);
            await Assertions.Expect(grid.Locator("tbody"))
                .ToContainTextAsync("Bút Lông", new() { Timeout = 15_000 });
        }

        await searchInput.FillAsync("but");
        await Assertions.Expect(grid.Locator("tbody"))
            .ToContainTextAsync("Bút", new() { Timeout = 15_000 });
        await Assertions.Expect(grid.Locator("tbody"))
            .Not.ToContainTextAsync("Bảng 0,8m x 1,2m", new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task Employee_CanBrowseRequestProductCatalog()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await Page.Locator(".vpp-catalog-workspace").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

        var grid = Page.Locator(".vpp-catalog-grid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await grid.Locator("tbody tr").CountAsync()).Should().BeGreaterThan(0);
        (await Page.Locator(".vpp-catalog-card-header").CountAsync()).Should().Be(0);
        (await Page.Locator("[data-testid='catalog-data-surface']").GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
        (await grid.Locator("thead th").First.InnerTextAsync()).Trim().Should().Be("#");
        (await grid.Locator("tbody tr").First.Locator("td").First.InnerTextAsync()).Trim().Should().Be("1");

        var searchInput = Page.Locator(".vpp-catalog-filter-group .vpp-filter-search input");
        await searchInput.FillAsync("giay");
        await Assertions.Expect(grid.Locator("tbody"))
            .ToContainTextAsync("Giấy", new() { Timeout = 15_000 });
        await searchInput.FillAsync(string.Empty);
        await Assertions.Expect(grid.Locator("tbody tr").First).ToBeVisibleAsync();

        var itemHeader = grid.Locator("thead th").Nth(1);
        var idleSortIcon = itemHeader
            .Locator(".rz-sortable-column-icon.rzi-sort:not(.rzi-sort-asc):not(.rzi-sort-desc)");
        var idleSortChromeIsStable = await idleSortIcon.EvaluateAsync<bool>("""
            element => {
                const style = getComputedStyle(element);
                return style.display !== 'none'
                    && Number.parseFloat(style.width) > 0
                    && Number.parseFloat(style.opacity) === 0;
            }
        """);
        idleSortChromeIsStable.Should().BeTrue(
            "inactive sort keeps a stable track but stays visually quiet");

        var itemTitleBeforeSort = await itemHeader.Locator(".rz-column-title").EvaluateAsync<string>("""
            element => {
                const rect = element.getBoundingClientRect();
                return `${rect.left}|${rect.width}`;
            }
        """);
        await itemHeader.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        var hoverSortOpacity = await idleSortIcon.EvaluateAsync<double>(
            "element => Number.parseFloat(getComputedStyle(element).opacity)");
        hoverSortOpacity.Should().BeGreaterThan(0,
            "hover reveals the sort affordance without moving the label");

        await itemHeader.ClickAsync();
        var ascendingSortIcon = itemHeader.Locator(".rz-sortable-column-icon.rzi-sort-asc");
        await ascendingSortIcon.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await ascendingSortIcon.EvaluateAsync<string>("element => getComputedStyle(element).opacity"))
            .Should().Be("1");
        var itemTitleAfterSort = await itemHeader.Locator(".rz-column-title").EvaluateAsync<string>("""
            element => {
                const rect = element.getBoundingClientRect();
                return `${rect.left}|${rect.width}`;
            }
        """);
        itemTitleAfterSort.Should().Be(itemTitleBeforeSort,
            "active sort uses the reserved icon track and must not shift the column title");

        await itemHeader.ClickAsync();
        await itemHeader.Locator(".rz-sortable-column-icon.rzi-sort-desc")
            .WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await itemHeader.ClickAsync();
        await idleSortIcon.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        await grid.Locator("tbody tr").First.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        (await idleSortIcon.EvaluateAsync<string>("element => getComputedStyle(element).opacity"))
            .Should().Be("0", "the third click restores the default ordering and quiet header state");
        var gridCornerRadius = await grid.EvaluateAsync<string>("""
            element => {
                const style = getComputedStyle(element);
                return `${style.borderTopLeftRadius}|${style.borderTopRightRadius}`;
            }
        """);
        gridCornerRadius.Should().Be("0px|0px", "the grid header joins the toolbar inside one shared data surface");
        var surfaceSeam = await Page.GetByTestId("catalog-data-surface").EvaluateAsync<string>("""
            surface => {
                const toolbar = surface.querySelector('.vpp-data-surface-toolbar-slot');
                const header = surface.querySelector('.vpp-data-grid thead');
                const grid = surface.querySelector('.vpp-data-grid');
                if (!toolbar || !header || !grid) return 'missing';
                const surfaceStyle = getComputedStyle(surface);
                const gridStyle = getComputedStyle(grid);
                const toolbarRect = toolbar.getBoundingClientRect();
                const headerRect = header.getBoundingClientRect();
                const oneOwner = parseFloat(surfaceStyle.borderTopWidth) > 0
                    && parseFloat(surfaceStyle.borderTopLeftRadius) > 0
                    && parseFloat(gridStyle.borderTopLeftRadius) === 0;
                const aligned = Math.abs(toolbarRect.bottom - headerRect.top) <= 1;
                const notOverlapping = headerRect.top >= toolbarRect.bottom - .5;
                return `${oneOwner && aligned && notOverlapping}`
                    + `|toolbar=${toolbarRect.bottom}|header=${headerRect.top}`
                    + `|surfaceRadius=${surfaceStyle.borderTopLeftRadius}|gridRadius=${gridStyle.borderTopLeftRadius}`;
            }
        """);
        surfaceSeam.Should().StartWith("true", "toolbar and header must form one surface without nested-card overlap");
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "catalog-surface-seam-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

        await grid.Locator(".rz-paginator, .rz-pager").WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await grid.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
        var filterTrigger = Page.Locator(".vpp-filter-select-trigger").First;
        var pageSizeDropdown = grid.Locator(".rz-paginator .rz-dropdown, .rz-pager .rz-dropdown").Last;
        var filterTriggerChrome = await ReadControlChromeAsync(filterTrigger);
        var pageSizeChrome = await ReadControlChromeAsync(pageSizeDropdown);
        pageSizeChrome.Should().Be(filterTriggerChrome, "default page-size select must reuse neutral canonical filter chrome");

        await filterTrigger.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        var filterHoverChrome = await ReadControlChromeAsync(filterTrigger);
        await pageSizeDropdown.HoverAsync();
        await Page.WaitForTimeoutAsync(200);
        var pageSizeHoverChrome = await ReadControlChromeAsync(pageSizeDropdown);
        pageSizeHoverChrome.Should().Be(filterHoverChrome, "page-size hover must reuse canonical filter hover chrome");

        await filterTrigger.ClickAsync();
        var filterPanel = Page.Locator(".vpp-filter-select-popover:popover-open").First;
        await filterPanel.WaitForAsync();
        var filterPopupGeometry = await ReadPopupGeometryAsync(
            filterPanel,
            filterPanel.Locator("[role='option']").First);
        var filterSelectedChrome = await ReadOptionChromeAsync(filterPanel.Locator("[role='option']").First);
        var filterHoverOption = filterPanel.Locator("[role='option']").Nth(1);
        await filterHoverOption.HoverAsync();
        var filterHoverOptionChrome = await ReadOptionChromeAsync(filterHoverOption);
        await Page.Keyboard.PressAsync("Escape");

        await pageSizeDropdown.ClickAsync();
        var pageSizePanel = Page.Locator(".rz-dropdown-panel:visible").Last;
        await pageSizePanel.WaitForAsync();
        await Page.WaitForTimeoutAsync(100);
        var pageSizeFocusEnvelope = await pageSizeDropdown.EvaluateAsync<string>("""
            element => {
                const style = getComputedStyle(element);
                return `${style.outlineStyle}|${style.outlineWidth}|${style.outlineOffset}|${style.borderRadius}`;
            }
        """);
        pageSizeFocusEnvelope.Should().StartWith("none|",
            "opening the page-size popup must not draw the global external oval focus ring");
        var pageSizePopupGeometry = await ReadPopupGeometryAsync(
            pageSizePanel,
            pageSizePanel.Locator(".rz-state-highlight").First);
        pageSizePopupGeometry.Should().Be(filterPopupGeometry, "Radzen select popup must reuse canonical filter popup geometry");
        var pageSizePopupContainment = await pageSizePanel.EvaluateAsync<string>("""
            panel => {
                const panelRect = panel.getBoundingClientRect();
                const options = [...panel.querySelectorAll('.rz-dropdown-item, .rz-dropdown-items > li')];
                const list = panel.querySelector('.rz-dropdown-items');
                const optionsContained = options.every(option => {
                    const rect = option.getBoundingClientRect();
                    return rect.left >= panelRect.left - 1 && rect.right <= panelRect.right + 1;
                });
                const firstRect = options[0]?.getBoundingClientRect();
                const listRect = list?.getBoundingClientRect();
                const firstStyle = options[0] ? getComputedStyle(options[0]) : null;
                return `${optionsContained}|${panel.scrollWidth <= panel.clientWidth + 1}`
                    + `|panel=${panelRect.left},${panelRect.right},${panel.clientWidth},${panel.scrollWidth}`
                    + `|list=${listRect?.left},${listRect?.right},${list?.clientWidth},${list?.scrollWidth}`
                    + `|first=${firstRect?.left},${firstRect?.right}`
                    + `|style=${firstStyle?.marginLeft},${firstStyle?.left},${firstStyle?.translate},${firstStyle?.transform},${firstStyle?.paddingLeft}`;
            }
        """);
        pageSizePopupContainment.Should().StartWith("true|true|",
            "page-size option hover/selected surfaces must stay inside the popup instead of bleeding to the right");
        var pageSizeSelectedChrome = await ReadOptionChromeAsync(pageSizePanel.Locator(".rz-state-highlight").First);
        var pageSizeHoverOption = pageSizePanel.Locator(".rz-dropdown-item:not(.rz-state-highlight)").First;
        await pageSizeHoverOption.HoverAsync();
        var pageSizeHoverOptionChrome = await ReadOptionChromeAsync(pageSizeHoverOption);
        OptionBackground(pageSizeSelectedChrome).Should().Be(OptionBackground(filterSelectedChrome),
            "selected options use the canonical neutral background");
        pageSizeHoverOptionChrome.Should().Be(filterHoverOptionChrome, "hovered options use the canonical filter hover state");
        var popupDirection = await pageSizePanel.EvaluateAsync<string>("""
            (panel) => {
                const trigger = document.getElementById(panel.id.replace(/^popup-/, ''));
                if (!trigger) return 'missing';
                const panelRect = panel.getBoundingClientRect();
                const triggerRect = trigger.getBoundingClientRect();
                return `${panelRect.top < triggerRect.top}|${getComputedStyle(panel).animationName}`;
            }
        """);
        popupDirection.Should().Be("true|vpp-transient-enter-up",
            "a pager popup flipped above its trigger must animate upward and remain independent of sidebar motion");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "catalog-page-size-popup-open-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

        await grid.Locator("thead th").First.ClickAsync();
        await pageSizePanel.WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden
        });

        var catalogGeometry = await Page.EvaluateAsync<string>("""
            () => {
                const workspace = document.querySelector('.vpp-catalog-workspace');
                const grid = document.querySelector('.vpp-catalog-grid');
                const pager = grid?.querySelector('.rz-paginator, .rz-pager');
                if (!workspace || !grid || !pager) return 'missing';
                const workspaceRect = workspace.getBoundingClientRect();
                const pagerRect = pager.getBoundingClientRect();
                const ok = Math.abs(workspaceRect.bottom - pagerRect.bottom) <= 2
                    && document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1;
                return `${ok}|bottom=${Math.round(workspaceRect.bottom - pagerRect.bottom)}|document=${document.documentElement.scrollHeight}/${document.documentElement.clientHeight}`;
            }
        """);
        catalogGeometry.Should().StartWith("true", "Catalog pager must stay at the workspace bottom without document scrolling");

        await Page.SetViewportSizeAsync(960, 768);
        await Page.WaitForTimeoutAsync(250);
        var narrowGridGeometry = await grid.EvaluateAsync<string>("""
            element => {
                const dataViewport = element.querySelector('.rz-data-grid-data');
                const headers = [...element.querySelectorAll('thead th')];
                const itemHeader = headers[1];
                const categoryHeader = headers[2];
                if (!dataViewport || !itemHeader || !categoryHeader) return 'missing';
                const itemRect = itemHeader.getBoundingClientRect();
                const categoryRect = categoryHeader.getBoundingClientRect();
                const stableColumns = itemRect.width >= 279 && categoryRect.width >= 199;
                const separated = itemRect.right <= categoryRect.left + .5;
                const nativeOverflow = dataViewport.scrollWidth > dataViewport.clientWidth;
                return `${stableColumns && separated && nativeOverflow}`
                    + `|item=${itemRect.width}|category=${categoryRect.width}`
                    + `|scroll=${dataViewport.scrollWidth}/${dataViewport.clientWidth}`;
            }
        """);
        narrowGridGeometry.Should().StartWith("true",
            "the shared data-surface must preserve column geometry and use native horizontal scrolling when space is constrained");

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "catalog-shared-frame-narrow-960x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

        await Page.Keyboard.PressAsync("Escape");
    }

    private static Task<string> ReadControlChromeAsync(ILocator control) => control.EvaluateAsync<string>("""
        element => {
            const style = getComputedStyle(element);
            return `${style.height}|${style.borderRadius}|${style.backgroundColor}|${style.color}|${style.boxShadow}`;
        }
    """);

    private static Task<string> ReadOptionChromeAsync(ILocator option) => option.EvaluateAsync<string>("""
        element => {
            const style = getComputedStyle(element);
            return `${style.minHeight}|${style.borderRadius}|${style.backgroundColor}|${style.color}|${style.fontWeight}`;
        }
    """);

    private static string OptionBackground(string chrome) => chrome.Split('|')[2];

    private static async Task<string> ReadPopupGeometryAsync(ILocator panel, ILocator selectedOption)
    {
        await selectedOption.WaitForAsync();
        var panelRadius = await panel.EvaluateAsync<string>("element => getComputedStyle(element).borderRadius");
        var optionGeometry = await selectedOption.EvaluateAsync<string>("""
            element => {
                const style = getComputedStyle(element);
                return `${style.minHeight}|${style.borderRadius}`;
            }
        """);
        return $"{panelRadius}|{optionGeometry}";
    }
}
