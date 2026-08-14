using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Library;

[Collection(ReadOnlyE2ECollection.Name)]
public class LibraryGridScrollTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task LookupColumnPicker_TogglesAndResetsWithVisibleTotalCount()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=0", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.GetByTestId("lookup-categories-data-surface");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var trigger = surface.Locator(".vpp-column-picker-trigger");
        await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var initialCount = await trigger.Locator(".vpp-column-picker-count").InnerTextAsync();
        var countParts = initialCount.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        countParts.Should().HaveCount(2);
        var initialVisible = int.Parse(countParts[0]);
        var total = int.Parse(countParts[1]);
        total.Should().BeGreaterThan(initialVisible);

        await trigger.ClickAsync();
        var popover = Page.Locator(".vpp-column-picker-popover:popover-open");
        await popover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var labels = await popover.Locator(".vpp-column-picker-label").AllInnerTextsAsync();
        labels.Should().NotContain("#");
        labels.Should().NotContain("Thao tác");
        labels.Should().ContainSingle(label => label == "ID hệ thống");
        labels.Should().Contain("Mã loại");
        labels.Should().Contain("Tên loại");

        var selectedBackground = await popover.Locator(".vpp-column-picker-option.is-selected").First
            .EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");
        selectedBackground.Should().Be("rgba(0, 0, 0, 0)");

        var targetLabel = await popover.EvaluateAsync<string>("""
            element => {
                const option = [...element.querySelectorAll('.vpp-column-picker-option')]
                    .find(candidate => {
                        const checkbox = candidate.querySelector('input[type="checkbox"]');
                        return checkbox && !checkbox.checked && !checkbox.disabled;
                    });
                const label = option?.querySelector('.vpp-column-picker-label')?.textContent?.trim();
                if (!label) {
                    throw new Error('No hidden pickable lookup column was rendered.');
                }

                return label;
            }
            """);
        var hiddenOption = popover.Locator(".vpp-column-picker-option")
            .Filter(new() { HasText = targetLabel });
        await hiddenOption.ClickAsync();
        await Assertions.Expect(trigger.Locator(".vpp-column-picker-count"))
            .ToHaveTextAsync($"{initialVisible + 1}/{total}");
        await surface.Locator("thead th").Filter(new() { HasText = targetLabel }).WaitForAsync();

        if (!await popover.IsVisibleAsync())
        {
            await trigger.ClickAsync();
            await popover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        }

        await popover.Locator(".vpp-column-picker-reset").ClickAsync();
        await Assertions.Expect(trigger.Locator(".vpp-column-picker-count")).ToHaveTextAsync(initialCount);
    }

    [Fact]
    public async Task LookupRowActions_KeepEditDirectAndMoveLifecycleIntoOverflow()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=0", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.GetByTestId("lookup-categories-data-surface");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var directEdit = surface.GetByRole(AriaRole.Button, new() { Name = "Sửa", Exact = true }).First;
        var moreActions = surface.GetByRole(AriaRole.Button, new() { Name = "Thao tác khác", Exact = true }).First;
        await directEdit.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await moreActions.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
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

        (await surface.Locator(".vpp-admin-active-switch").CountAsync()).Should().Be(0,
            "lifecycle không còn cạnh tranh trực tiếp với action Sửa");
        await moreActions.ClickAsync();
        var lifecycleMenu = Page.Locator(".rz-context-menu:visible");
        await lifecycleMenu.WaitForAsync();
        await lifecycleMenu.GetByText("Vô hiệu hóa", new() { Exact = true }).WaitForAsync();
        await lifecycleMenu.GetByText("Xóa vĩnh viễn", new() { Exact = true }).WaitForAsync();
        var menuEvidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(menuEvidenceDirectory))
        {
            Directory.CreateDirectory(menuEvidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(menuEvidenceDirectory, "admin-row-action-menu-open-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await Page.Keyboard.PressAsync("Escape");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "admin-row-action-hierarchy-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    [Fact]
    public async Task Category_Grid_Uses_Bounded_Data_Surface_Without_Header_Overlap()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 420);
        await Page.GotoAsync($"{BaseUrl}library?tab=1", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.Locator("[data-testid='category-admin-data-surface']");
        var grid = Page.Locator("[data-testid='category-admin-data-surface'] .vpp-data-grid:visible");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await grid.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.WaitForFunctionAsync("""
            () => {
                const grid = [...document.querySelectorAll('[data-testid="category-admin-data-surface"] .vpp-data-grid')]
                    .find(candidate => candidate.getClientRects().length > 0);
                const body = grid?.querySelector('.rz-data-grid-data');
                return !!body && (body.querySelectorAll('tbody > tr').length > 0
                    || !!grid.querySelector('.rz-datatable-emptymessage'));
            }
            """);

        (await surface.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(1);
        (await surface.Locator(".vpp-column-picker-trigger").CountAsync()).Should().Be(1);
        var createButton = surface.Locator(".vpp-collection-header-add");
        await createButton.WaitForAsync();

        var geometry = await surface.EvaluateAsync<double[]>("""
            element => {
                const body = document.querySelector('.vpp-layout-body');
                const workspace = element.closest('[data-vpp-workspace-pattern="collection"]');
                const panel = element.closest('.rz-tabview-panel');
                const toolbar = element.querySelector('.vpp-data-toolbar');
                const create = element.querySelector('.vpp-collection-header-add');
                const grid = element.querySelector('.vpp-data-grid');
                const header = grid?.querySelector('thead');
                const firstRow = grid?.querySelector('tbody > tr');
                const pager = grid?.querySelector('.rz-paginator, .rz-pager');
                if (!body || !workspace || !panel || !toolbar || !create || !grid || !header || !firstRow || !pager) {
                    throw new Error('Canonical category data surface was not rendered.');
                }
                return [
                    workspace.getBoundingClientRect().left - panel.getBoundingClientRect().left,
                    panel.getBoundingClientRect().right - workspace.getBoundingClientRect().right,
                    toolbar.getBoundingClientRect().height,
                    create.closest('[data-vpp-collection-header="true"]') ? 1 : 0,
                    body.scrollHeight - body.clientHeight,
                    element.getBoundingClientRect().bottom,
                    pager.getBoundingClientRect().bottom,
                    window.innerHeight,
                    firstRow.getBoundingClientRect().top - header.getBoundingClientRect().bottom
                ];
            }
            """);
        geometry[0].Should().BeApproximately(0, 0.5);
        geometry[1].Should().BeApproximately(0, 0.5);
        geometry[2].Should().BeApproximately(42, 2);
        geometry[3].Should().Be(1, "the create action belongs to the collection header");
        geometry[4].Should().BeLessThanOrEqualTo(1, "the route uses a bounded grid viewport instead of document scrolling");
        geometry[5].Should().BeLessThanOrEqualTo(geometry[7] + 1);
        geometry[6].Should().BeLessThanOrEqualTo(geometry[5] + 1);
        geometry[8].Should().BeApproximately(0, 0.5);

        var statusBadges = grid.Locator("tbody .vpp-status-badge");
        var emptyState = grid.Locator(".rz-datatable-emptymessage");
        ((await statusBadges.CountAsync()) > 0 || (await emptyState.CountAsync()) > 0).Should().BeTrue(
            "the isolated fixture may be empty, but the typed grid must settle to rows or its empty state");
        if (await statusBadges.CountAsync() > 0)
        {
            var firstBadge = statusBadges.First;
            var badgeText = (await firstBadge.InnerTextAsync()).Trim();
            var badgeClass = await firstBadge.GetAttributeAsync("class");
            if (badgeText is "Hoạt động" or "Active")
            {
                badgeClass.Should().Contain("vpp-status-badge-info",
                    "Active resource phải dùng semantic info thay vì success");
            }
            else
            {
                badgeClass.Should().Contain("vpp-status-badge-neutral",
                    "Inactive resource phải dùng semantic neutral");
            }
        }
        (await surface.Locator(".vpp-data-toolbar .vpp-collection-header-add").CountAsync()).Should().Be(0);
        (await surface.Locator("th.rz-col-actions .vpp-collection-header-add").CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Category_Editor_OpensAsTypedDialogInsteadOfInlineRowEdit()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=1", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.Locator("[data-testid='category-admin-data-surface']");
        var createButton = surface.Locator(".vpp-collection-header-add");
        await createButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await createButton.ClickAsync();

        var dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--compact:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await dialog.Locator("[data-testid='category-editor']").CountAsync()).Should().Be(1);
        (await surface.Locator(".rz-cell-editing").CountAsync()).Should().Be(0);

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa2-category-collection-editor.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    [Fact]
    public async Task Supplier_Editor_UsesStandardAdaptiveDialog()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=3", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.Locator("[data-testid='supplier-admin-data-surface']");
        var createButton = surface.Locator(".vpp-collection-header-add");
        await createButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await createButton.ClickAsync();

        var dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--standard:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await dialog.Locator("[data-testid='supplier-editor']").CountAsync()).Should().Be(1);
        (await surface.Locator(".rz-cell-editing").CountAsync()).Should().Be(0);

        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    [Fact]
    public async Task Item_Editor_UsesStandardAdaptiveDialog()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=2", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.Locator("[data-testid='item-admin-data-surface']");
        var createButton = surface.Locator(".vpp-collection-header-add");
        await createButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await createButton.ClickAsync();

        var dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--standard:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await dialog.Locator("[data-testid='item-editor']").CountAsync()).Should().Be(1);
        (await surface.Locator(".rz-cell-editing").CountAsync()).Should().Be(0);

        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    [Fact]
    public async Task Department_Editor_UsesStandardAdaptiveDialog()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=5", new() { WaitUntil = WaitUntilState.Load });
        var surface = Page.Locator("[data-testid='department-admin-data-surface']");
        var createButton = surface.Locator(".vpp-collection-header-add");
        await createButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await createButton.ClickAsync();
        var dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--standard:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await dialog.Locator("[data-testid='department-editor']").CountAsync()).Should().Be(1);
        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    [Fact]
    public async Task Pricing_Grids_Use_Bounded_Data_Surfaces_And_NormalFlow_Tabs()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 420);
        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists", new() { WaitUntil = WaitUntilState.Load });

        var priceListSurface = Page.Locator("[data-testid='price-lists-data-surface']");
        var priceListGrid = priceListSurface.Locator(".vpp-admin-page-grid");
        var secondaryTabs = Page.Locator(".vpp-layout-header .vpp-header-tab-group");
        await priceListSurface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await priceListGrid.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await secondaryTabs.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Page.WaitForFunctionAsync("""
            () => {
                const grid = document.querySelector('[data-testid="price-lists-data-surface"] .vpp-admin-page-grid');
                const body = grid?.querySelector('.rz-data-grid-data');
                return !!body && (body.querySelectorAll('tbody > tr').length > 0
                    || !!grid.querySelector('.rz-datatable-emptymessage'));
            }
            """);

        await priceListSurface.Locator(".vpp-admin-action-label").First.WaitForAsync();
        var priceListMoreActions = priceListSurface.GetByRole(AriaRole.Button, new() { Name = "Thao tác khác", Exact = true }).First;
        await priceListMoreActions.ClickAsync();
        await Page.Locator(".rz-context-menu:visible").WaitForAsync();
        await Page.Keyboard.PressAsync("Escape");

        var priceListMetrics = await priceListSurface.EvaluateAsync<double[]>("""
            element => {
                const body = document.querySelector('.vpp-layout-body');
                const tabs = document.querySelector('.vpp-layout-header .vpp-header-tab-group');
                const grid = element.querySelector('.vpp-admin-page-grid');
                const pager = element.querySelector('.rz-paginator, .rz-pager');
                if (!body || !tabs || !grid || !pager) {
                    throw new Error('Canonical price-list surface was not rendered.');
                }
                return [
                    getComputedStyle(tabs).display === 'flex' ? 1 : 0,
                    tabs.getBoundingClientRect().bottom,
                    element.getBoundingClientRect().top,
                    parseFloat(getComputedStyle(grid).borderTopWidth),
                    body.scrollHeight - body.clientHeight,
                    pager.getBoundingClientRect().bottom,
                    window.innerHeight
                ];
            }
            """);
        priceListMetrics[0].Should().Be(1);
        priceListMetrics[1].Should().BeLessThanOrEqualTo(priceListMetrics[2] + 1,
            "nested navigation stays in normal flow and cannot cover the data toolbar");
        priceListMetrics[3].Should().Be(0);
        priceListMetrics[4].Should().BeLessThanOrEqualTo(1);
        priceListMetrics[5].Should().BeLessThanOrEqualTo(priceListMetrics[6] + 1);

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa4-price-list-collection.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=prices", new() { WaitUntil = WaitUntilState.Load });
        var priceSurface = Page.Locator("[data-testid='prices-data-surface']");
        var priceGrid = priceSurface.Locator(".vpp-price-grid");
        await priceSurface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await priceGrid.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await priceSurface.Locator(".vpp-admin-action-label").First.WaitForAsync();
        await priceSurface.GetByRole(AriaRole.Button, new() { Name = "Thao tác khác", Exact = true }).First.WaitForAsync();

        var priceMetrics = await priceSurface.EvaluateAsync<double[]>("""
            element => {
                const body = document.querySelector('.vpp-layout-body');
                const grid = element.querySelector('.vpp-price-grid');
                if (!body || !grid) {
                    throw new Error('Canonical price surface was not rendered.');
                }
                return [
                    body.scrollHeight - body.clientHeight,
                    grid.getBoundingClientRect().bottom,
                    window.innerHeight,
                    parseFloat(getComputedStyle(grid).borderTopWidth)
                ];
            }
            """);
        priceMetrics[0].Should().BeGreaterThan(1,
            "short-height adaptive workspaces must let the outer page scroll instead of clipping the price grid");
        priceMetrics[3].Should().Be(0);

        var priceLayoutBody = Page.Locator(".vpp-layout-body");
        await priceLayoutBody.EvaluateAsync("element => element.scrollTop = element.scrollHeight");
        await WaitForRenderSettleAsync();
        var priceGridBottom = await priceGrid.EvaluateAsync<double>("element => element.getBoundingClientRect().bottom");
        priceGridBottom.Should().BeLessThanOrEqualTo(priceMetrics[2] + 1,
            "the complete price data surface must remain reachable by page scrolling");

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa4-price-collection.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    [Fact]
    public async Task PriceList_Editor_UsesCanonicalSupplierSelect_AndHidesDeferredFields()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=price-lists", new() { WaitUntil = WaitUntilState.Load });

        var surface = Page.Locator("[data-testid='price-lists-data-surface']");
        var createButton = surface.Locator(".vpp-collection-header-add");
        await createButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await createButton.ClickAsync();

        var dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--standard:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await dialog.Locator("[data-testid='price-list-editor']").CountAsync()).Should().Be(1);
        await Assertions.Expect(dialog.GetByText("Mã hợp đồng tham chiếu", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(dialog.GetByText("Chiết khấu (%)", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(dialog.GetByText("Khoản giảm thêm", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(dialog.GetByText("Phụ phí", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(dialog.GetByText("Phí vận chuyển", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(dialog.GetByText("Đơn vị tiền tệ", new() { Exact = true })).ToHaveCountAsync(0);

        var supplierSelect = dialog.Locator(".vpp-decision-select");
        await Assertions.Expect(supplierSelect).ToHaveCountAsync(1);
        await Assertions.Expect(dialog.Locator(".rz-dropdown")).ToHaveCountAsync(0);
        var supplierTrigger = supplierSelect.Locator(".vpp-decision-select-trigger");
        var supplierPopover = dialog.Locator(".vpp-decision-select-popover");
        var supplierOptionCount = await supplierPopover.Locator("button[role='option']").CountAsync();
        if (supplierOptionCount > 0)
        {
            await supplierTrigger.ClickAsync();
            await supplierPopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await Assertions.Expect(supplierPopover.Locator("input")).ToHaveCountAsync(0);
        }
        else
        {
            await Assertions.Expect(supplierTrigger).ToBeDisabledAsync();
        }

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "price-list-editor-design-system-select.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        if (supplierOptionCount > 0)
        {
            await Page.Keyboard.PressAsync("Escape");
            await supplierPopover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    [Fact]
    public async Task Class_Definitions_Use_Compact_Master_Detail_Layout()
    {
        var browserErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            if (message.Type == "error")
            {
                browserErrors.Add(message.Text);
            }
        };

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
        await master.Locator("tbody > tr").First.WaitForAsync();
        await detail.Locator("tbody > tr").First.WaitForAsync();
        await master.Locator(".rz-paginator, .rz-pager").WaitForAsync();
        await detail.Locator(".rz-paginator, .rz-pager").WaitForAsync();

        var masterPageSize = master.Locator(".rz-paginator .rz-dropdown, .rz-pager .rz-dropdown").Last;
        await masterPageSize.ClickAsync();
        var masterPageSizePanel = Page.Locator(".rz-dropdown-panel:visible").Last;
        await masterPageSizePanel.WaitForAsync();
        await detail.Locator("thead th").First.ClickAsync();
        await masterPageSizePanel.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.vpp-admin-class-split .rz-datatable-loading')].every(element => {
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
        await master.Locator(".vpp-class-code-value").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible
        });
        await detail.Locator("tbody > tr").First.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible
        });

        var quickEvidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(quickEvidenceDirectory))
        {
            Directory.CreateDirectory(quickEvidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(quickEvidenceDirectory, "class-fixed-master-detail.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

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
                    getComputedStyle(split).display === 'grid' ? 1 : 0,
                    master.getBoundingClientRect().right <= detail.getBoundingClientRect().left ? 1 : 0,
                    masterTable.scrollWidth - masterData.clientWidth
                ];
            }
            """);
        layout[0].Should().Be(1);
        layout[1].Should().Be(1);
        layout[2].Should().BeLessThanOrEqualTo(1);

        var pickerTrigger = Page.Locator("[data-testid='lookup-categories-data-surface'] .vpp-column-picker-trigger");
        (await pickerTrigger.CountAsync()).Should().Be(1);
        (await split.Locator(".vpp-filter-search").CountAsync()).Should().Be(2);
        (await split.Locator(".rz-grid-filter-icon").CountAsync()).Should().Be(0);
        var masterChrome = await master.EvaluateAsync<string[]>("""
            element => {
                const header = element.querySelector('thead');
                const codeCell = element.querySelector('.vpp-class-code-value');
                const nameCell = element.querySelector('.vpp-class-name-value');
                if (!header || !codeCell || !nameCell) {
                    throw new Error('Class master list chrome was not rendered.');
                }

                return [
                    getComputedStyle(header).display,
                    codeCell.textContent?.trim() ?? '',
                    nameCell.textContent?.trim() ?? ''
                ];
            }
            """);
        masterChrome[0].Should().NotBe("none");
        masterChrome[1].Should().NotBeNullOrWhiteSpace("the class code must own a dedicated column");
        masterChrome[2].Should().NotBeNullOrWhiteSpace("the class name must own a dedicated column");
        masterChrome[1].Should().NotBe(masterChrome[2], "code and name must not be merged into one visual cell");

        var masterSemantics = await master.EvaluateAsync<double[]>("""
            element => {
                const codeCell = element.querySelector('.vpp-class-code-value');
                const nameCell = element.querySelector('.vpp-class-name-value');
                const statusBadge = element.querySelector('tbody .vpp-status-badge');
                const statusCell = statusBadge?.closest('td');
                const statusHeader = element.querySelector('thead th.vpp-class-status-column');
                if (!codeCell || !nameCell || !statusHeader || !statusCell || !statusBadge) {
                    throw new Error('Lookup category columns do not match their semantic cells.');
                }

                const headerBox = statusHeader.getBoundingClientRect();
                const cellBox = statusCell.getBoundingClientRect();
                return [
                    codeCell.querySelectorAll('.vpp-status-badge').length + nameCell.querySelectorAll('.vpp-status-badge').length,
                    statusCell.querySelectorAll('.vpp-status-badge').length,
                    statusCell.textContent?.trim().length ?? -1,
                    Math.abs(headerBox.left - cellBox.left),
                    Math.abs(headerBox.width - cellBox.width)
                ];
            }
            """);
        masterSemantics[0].Should().Be(0);
        masterSemantics[1].Should().Be(1);
        masterSemantics[2].Should().BeGreaterThan(0);
        masterSemantics[3].Should().BeLessThanOrEqualTo(1);
        masterSemantics[4].Should().BeLessThanOrEqualTo(1);

        var surfaceGeometry = await Page.EvaluateAsync<double[]>("""
            () => {
                const body = document.querySelector('.vpp-layout-body');
                const masterSurface = document.querySelector('[data-testid="lookup-categories-data-surface"]');
                const detailSurface = document.querySelector('[data-testid="lookup-values-data-surface"]');
                const detailData = detailSurface?.querySelector('.rz-data-grid-data');
                const masterPager = masterSurface?.querySelector('.rz-paginator, .rz-pager');
                const detailPager = detailSurface?.querySelector('.rz-paginator, .rz-pager');
                const detailRows = detailSurface?.querySelectorAll('tbody > tr');
                const firstCell = detailRows?.[0]?.querySelector('td');
                const secondCell = detailRows?.[1]?.querySelector('td');
                if (!body || !masterSurface || !detailSurface || !detailData || !masterPager || !detailPager || !firstCell || !secondCell) {
                    throw new Error('Lookup surfaces did not render their bounded body and footer.');
                }

                return [
                    body.scrollHeight - body.clientHeight,
                    Math.abs(masterSurface.getBoundingClientRect().top - detailSurface.getBoundingClientRect().top),
                    Math.abs(masterSurface.getBoundingClientRect().bottom - detailSurface.getBoundingClientRect().bottom),
                    masterSurface.getBoundingClientRect().bottom - masterPager.getBoundingClientRect().bottom,
                    detailSurface.getBoundingClientRect().bottom - detailPager.getBoundingClientRect().bottom,
                    detailData.scrollHeight - detailData.clientHeight,
                    getComputedStyle(detailData).overflowY === 'auto' || getComputedStyle(detailData).overflowY === 'scroll' ? 1 : 0,
                    getComputedStyle(firstCell).backgroundColor === getComputedStyle(secondCell).backgroundColor ? 1 : 0
                ];
            }
            """);
        surfaceGeometry[0].Should().BeLessThanOrEqualTo(1, "Lookup keeps scrolling inside each data surface");
        surfaceGeometry[1].Should().BeLessThanOrEqualTo(1);
        surfaceGeometry[2].Should().BeLessThanOrEqualTo(1);
        surfaceGeometry[3].Should().BeApproximately(0, 1);
        surfaceGeometry[4].Should().BeApproximately(0, 1);
        surfaceGeometry[5].Should().BeGreaterThan(1, "the value grid owns a real vertical scroll region");
        surfaceGeometry[6].Should().Be(1);
        surfaceGeometry[7].Should().Be(1, "admin rows use one neutral background instead of zebra striping");

        var layoutEvidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(layoutEvidenceDirectory))
        {
            Directory.CreateDirectory(layoutEvidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(layoutEvidenceDirectory, "aa1-lookup-master-desktop.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        var initialCountParts = (await pickerTrigger.Locator(".vpp-column-picker-count").InnerTextAsync())
            .Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        initialCountParts.Should().HaveCount(2);
        var initialVisibleCount = int.Parse(initialCountParts[0]);
        var totalPickableCount = int.Parse(initialCountParts[1]);
        initialVisibleCount.Should().BeGreaterThan(0);
        totalPickableCount.Should().BeGreaterThanOrEqualTo(initialVisibleCount);
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

                const rootFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);

                return [
                    element.getBoundingClientRect().width,
                    Math.min(rootFontSize * 20, window.innerWidth - rootFontSize),
                    header.getBoundingClientRect().height,
                    item.getBoundingClientRect().height,
                    parseFloat(getComputedStyle(element).borderRadius),
                    getComputedStyle(element).position === 'fixed' ? 1 : 0,
                    element.getBoundingClientRect().right <= window.innerWidth ? 1 : 0,
                    element.getBoundingClientRect().bottom <= window.innerHeight ? 1 : 0
                ];
            }
            """);
        pickerChrome[0].Should().BeApproximately(pickerChrome[1], 2);
        pickerChrome[2].Should().BeGreaterThanOrEqualTo(50);
        pickerChrome[3].Should().BeGreaterThanOrEqualTo(38);
        pickerChrome[4].Should().BeGreaterThanOrEqualTo(8);
        pickerChrome[5].Should().Be(1);
        pickerChrome[6].Should().Be(1);
        pickerChrome[7].Should().Be(1);

        var selectedOptionChrome = await pickerPanel.Locator(".vpp-column-picker-option.is-selected").First
            .EvaluateAsync<string[]>("""
                element => {
                    const checkbox = element.querySelector('.vpp-column-picker-checkbox');
                    return [
                        getComputedStyle(element).backgroundColor,
                        checkbox ? getComputedStyle(checkbox).backgroundColor : ''
                    ];
                }
                """);
        selectedOptionChrome[0].Should().Be("rgba(0, 0, 0, 0)",
            "dòng đã chọn chỉ dùng checkbox, không phủ nền xanh lên option");
        selectedOptionChrome[1].Should().NotBe("rgba(0, 0, 0, 0)");

        var search = pickerPanel.Locator(".vpp-column-picker-search input");
        var initialOptionCount = await pickerPanel.Locator(".vpp-column-picker-option").CountAsync();
        var targetColumnLabel = await pickerPanel.EvaluateAsync<string>("""
            element => {
                const option = [...element.querySelectorAll('.vpp-column-picker-option')]
                    .find(candidate => {
                        const checkbox = candidate.querySelector('input[type="checkbox"]');
                        return checkbox && !checkbox.checked && !checkbox.disabled;
                    });
                const label = option?.querySelector('.vpp-column-picker-label')?.textContent?.trim();
                if (!label) {
                    throw new Error('No hidden pickable class column was rendered.');
                }

                return label;
            }
            """);
        await search.PressSequentiallyAsync(
            targetColumnLabel,
            new LocatorPressSequentiallyOptions { Delay = 40 });
        await Page.WaitForFunctionAsync(
            """
            () => {
                const popover = document.querySelector('.vpp-column-picker-popover:popover-open');
                return !popover || popover.querySelectorAll('.vpp-column-picker-option').length === 1;
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 15_000 });
        if (!await pickerPanel.IsVisibleAsync())
        {
            await pickerTrigger.ClickAsync();
            await pickerPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }
        var filteredOptions = pickerPanel.Locator(".vpp-column-picker-option:visible");
        (await filteredOptions.CountAsync()).Should().Be(1);
        (await filteredOptions.InnerTextAsync()).Should().Contain(targetColumnLabel);
        await search.FillAsync(string.Empty);
        await Page.WaitForFunctionAsync(
            """
            expected => {
                const popover = document.querySelector('.vpp-column-picker-popover:popover-open');
                return !popover || popover.querySelectorAll('.vpp-column-picker-option').length === expected;
            }
            """,
            initialOptionCount);
        if (!await pickerPanel.IsVisibleAsync())
        {
            await pickerTrigger.ClickAsync();
            await pickerPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }

        var targetOption = pickerPanel.Locator(".vpp-column-picker-option")
            .Filter(new LocatorFilterOptions { HasText = targetColumnLabel });
        (await targetOption.CountAsync()).Should().Be(1);
        await targetOption.ClickAsync();
        var pickerCount = pickerTrigger.Locator(".vpp-column-picker-count");
        var expectedVisibleCount = initialVisibleCount + 1;
        var actualVisibleCount = initialVisibleCount;
        var toggleDeadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < toggleDeadline)
        {
            actualVisibleCount = int.Parse((await pickerCount.InnerTextAsync()).Split('/')[0]);
            if (actualVisibleCount == expectedVisibleCount)
            {
                break;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        if (actualVisibleCount != expectedVisibleCount)
        {
            var checkbox = targetOption.Locator("input[type='checkbox']");
            bool? checkboxState = await checkbox.CountAsync() == 1
                ? await checkbox.IsCheckedAsync()
                : null;
            throw new InvalidOperationException(
                $"Column picker toggle did not update. Expected count {expectedVisibleCount}, " +
                $"actual {actualVisibleCount}, checkbox {checkboxState}, " +
                $"popover visible {await pickerPanel.IsVisibleAsync()}, " +
                $"browser errors: {string.Join(" | ", browserErrors)}.");
        }
        var targetHeader = master.Locator("thead th")
            .Filter(new LocatorFilterOptions { HasText = targetColumnLabel });
        await targetHeader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        if (!await pickerPanel.IsVisibleAsync())
        {
            await pickerTrigger.ClickAsync();
            await pickerPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        }

        await pickerPanel.Locator(".vpp-column-picker-reset").ClickAsync();
        await Page.WaitForFunctionAsync(
            """
            expected => document.querySelector(
                '[data-testid="lookup-categories-data-surface"] .vpp-column-picker-count')?.textContent?.trim() === expected
            """,
            $"{initialVisibleCount}/{totalPickableCount}");
        (await pickerTrigger.Locator(".vpp-column-picker-count").InnerTextAsync())
            .Should().Be($"{initialVisibleCount}/{totalPickableCount}");
        await targetHeader.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });

        await Page.SetViewportSizeAsync(1024, 768);
        // Dưới breakpoint compact, fixed split chuyển thành một cột; không còn splitter kéo.
        await Page.WaitForFunctionAsync("""
            () => {
                const split = document.querySelector('.vpp-admin-class-split');
                return !!split && getComputedStyle(split).gridTemplateColumns.split(' ').length === 1;
            }
            """);
        var compactColumnCount = await split.EvaluateAsync<int>("element => getComputedStyle(element).gridTemplateColumns.split(' ').length");
        compactColumnCount.Should().Be(1);
    }

    [Fact]
    public async Task LookupEditor_Uses_AdaptiveDialogShell_OnDesktopAndMobile()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}library?tab=0", new() { WaitUntil = WaitUntilState.Load });
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");

        var surface = Page.Locator("[data-testid='lookup-categories-data-surface']");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var categoryActions = surface.Locator("tbody .vpp-admin-actions").First;
        await categoryActions.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await categoryActions.Locator(".rz-switch").CountAsync()).Should().Be(1,
            "soft deactivation is a status switch rather than a delete-looking button");
        (await categoryActions.Locator("button:has(.rzi:text-is('delete_forever'))").CountAsync()).Should().Be(1,
            "hard delete is a separate explicit action");
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.vpp-admin-class-split .rz-datatable-loading')].every(element => {
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
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa1-lookup-actions-desktop.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await surface.Locator(".vpp-collection-header-add").ClickAsync();

        var dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--compact:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await dialog.Locator(".vpp-adaptive-dialog-shell[data-vpp-admin-dialog-size='compact']").CountAsync()).Should().Be(1);
        (await dialog.Locator(".vpp-adaptive-dialog-body").CountAsync()).Should().Be(1);
        (await dialog.Locator(".vpp-adaptive-dialog-footer").CountAsync()).Should().Be(1);
        var categoryPlaceholders = await dialog.Locator("input[placeholder], textarea[placeholder]").CountAsync();
        categoryPlaceholders.Should().BeGreaterThanOrEqualTo(2,
            "create fields use examples derived from the loaded database rows");

        var desktopBox = await dialog.BoundingBoxAsync();
        desktopBox.Should().NotBeNull();
        desktopBox!.Width.Should().BeLessThanOrEqualTo(640);
        desktopBox.X.Should().BeGreaterThan(0);
        desktopBox.X.Should().BeLessThan(1366 - desktopBox.Width);

        var description = dialog.Locator("textarea.rz-textarea");
        await description.FocusAsync();
        var focusChrome = await description.EvaluateAsync<string[]>("""
            element => {
                const field = element.closest('.rz-form-field');
                if (!field) {
                    throw new Error('Lookup description is not owned by a RadzenFormField.');
                }

                const style = getComputedStyle(element);
                return [
                    style.outlineStyle,
                    style.boxShadow,
                    style.borderTopColor,
                    document.activeElement === element ? 'active' : 'inactive'
                ];
            }
            """);
        focusChrome[0].Should().Be("none");
        focusChrome[1].Should().Be("none");
        focusChrome[2].Should().Be("rgba(0, 0, 0, 0)");
        focusChrome[3].Should().Be("active");

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa1-lookup-editor-desktop.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var valueSurface = Page.Locator("[data-testid='lookup-values-data-surface']");
        await valueSurface.Locator(".vpp-collection-header-add").ClickAsync();
        var valueDialog = Page.Locator(".rz-dialog.vpp-admin-dialog--compact:visible");
        await valueDialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await valueDialog.Locator("[data-testid='lookup-value-editor']").CountAsync()).Should().Be(1);
        (await valueDialog.GetByText("Trường bổ sung", new() { Exact = false }).CountAsync()).Should().Be(0);
        (await valueDialog.Locator("input[placeholder], textarea[placeholder]").CountAsync())
            .Should().BeGreaterThanOrEqualTo(2);
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa1-lookup-value-editor-desktop.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await Page.Keyboard.PressAsync("Escape");
        await valueDialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        await Page.SetViewportSizeAsync(390, 844);
        var sidebarBackdrop = Page.Locator(".vpp-sidebar-backdrop:visible");
        if (await sidebarBackdrop.CountAsync() > 0)
        {
            await sidebarBackdrop.ClickAsync();
            await sidebarBackdrop.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await surface.Locator(".vpp-collection-header-add").ClickAsync();
        dialog = Page.Locator(".rz-dialog.vpp-admin-dialog--compact:visible");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var mobileBox = await dialog.BoundingBoxAsync();
        mobileBox.Should().NotBeNull();
        mobileBox!.Width.Should().BeLessThan(390);
        mobileBox.Height.Should().BeLessThan(844);
        mobileBox.X.Should().BeGreaterThanOrEqualTo(0);
        mobileBox.Y.Should().BeGreaterThan(0);

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "aa1-lookup-editor-mobile.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await Page.Keyboard.PressAsync("Escape");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }
}
