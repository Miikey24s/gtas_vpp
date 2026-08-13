using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Settlement;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class PeriodSettlementWorkspaceTests : TestBase, IAuthenticatedUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_SETTLEMENT_SCREENSHOT_DIR";
    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public async Task SettlementWorkspace_ShowsFinancialColumns_GroupAllOption_AndStablePageSizePopup(
        int width,
        int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsAsync(TestAccounts.Procurement);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=settle");

        var surface = Page.GetByTestId("period-settlement-data-surface");
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });

        if (width == 1366)
        {
            await SetDarkModeAsync(true);
            (await Page.EvaluateAsync<bool>(
                    "() => document.documentElement.classList.contains('rz-theme-dark')"))
                .Should().BeTrue();
        }

        await Assertions.Expect(Page.GetByText("Độ phủ bảng giá", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator(".vpp-settlement-kpi-card"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.Locator(".vpp-settlement-decision-cards:visible"))
            .ToBeVisibleAsync();
        await Assertions.Expect(Page.Locator(".vpp-settlement-export-card:visible")).ToHaveCountAsync(2);
        var decisionCardWidths = await Page.Locator(
                ".vpp-settlement-decision-cards:visible > .vpp-decision-select, "
                + ".vpp-settlement-decision-cards:visible > .vpp-settlement-export-card")
            .EvaluateAllAsync<double[]>("cards => cards.map(card => card.getBoundingClientRect().width)");
        (decisionCardWidths.Max() - decisionCardWidths.Min()).Should().BeLessThanOrEqualTo(1,
            "the four settlement cards must share one equal-width grid");
        await Assertions.Expect(Page.GetByText("Phương án chốt", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Tạo phiên bản hiệu chỉnh", Exact = true }))
            .ToHaveCountAsync(0);

        var settlementAction = Page.GetByRole(
            AriaRole.Button,
            new() { NameRegex = new Regex("^Chốt( lại)? kỳ$") }).First;
        await Assertions.Expect(settlementAction).ToBeEnabledAsync();
        await settlementAction.ClickAsync();
        var settlementPreviewDialog = Page.GetByTestId("settlement-preview-dialog");
        await settlementPreviewDialog.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Assertions.Expect(settlementPreviewDialog.Locator(".vpp-settlement-preview-summary article"))
            .ToHaveCountAsync(5);
        await Assertions.Expect(settlementPreviewDialog.Locator(".vpp-settlement-preview-select"))
            .ToHaveCountAsync(2);
        await Assertions.Expect(settlementPreviewDialog.Locator(".vpp-settlement-preview-orders"))
            .ToBeVisibleAsync();
        await AssertElementInsideViewportAsync(settlementPreviewDialog);
        await CaptureAsync($"settlement-preview-dialog-{width}x{height}.png");
        await settlementPreviewDialog.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).ClickAsync();
        await settlementPreviewDialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });

        var supplierPicker = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Chọn nhà cung cấp", Exact = true });
        await supplierPicker.ClickAsync();
        var selectedSupplier = Page.Locator(
            ".vpp-decision-select-popover:popover-open [role='option'][aria-selected='true']");
        await Assertions.Expect(selectedSupplier).ToBeVisibleAsync();
        await Assertions.Expect(selectedSupplier.Locator(".vpp-icon")).ToBeVisibleAsync();
        var selectedSupplierBackground = await selectedSupplier.EvaluateAsync<string>(
            "option => getComputedStyle(option).backgroundColor");
        selectedSupplierBackground.Should().Be("rgba(0, 0, 0, 0)",
            "selected vertical options stay neutral until hover or focus");
        await selectedSupplier.ClickAsync();

        await AssertColumnsAsync(surface, ["Trạng thái", "Đặt nhiều nhất", "Thành tiền", "VAT", "Tổng cộng"]);
        await AssertCombinedOrderCountColumnAsync(surface);
        await Assertions.Expect(surface.Locator("thead").GetByText("Dòng mặt hàng", new() { Exact = true }))
            .ToBeVisibleAsync();
        await AssertCompactRowRhythmAsync(surface);
        await AssertSettlementSummaryFooterAsync(surface);
        await Assertions.Expect(surface.Locator("thead").GetByText("Trạng thái", new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(surface.Locator("tbody .vpp-settlement-status-count").First.Locator(".vpp-status-badge"))
            .ToHaveCountAsync(2);
        await Assertions.Expect(surface.Locator("tbody .vpp-settlement-status-count").GetByText("Đã duyệt: 0", new() { Exact = true }).First)
            .ToBeVisibleAsync();
        if (width >= 1100)
        {
            await AssertFilterColumnOrderAsync(surface);
        }
        await CaptureAsync($"settlement-departments-{width}x{height}.png");
        await AssertNumericColumnsAlignedAsync(surface);

        if (width == 1920)
        {
            var orderTypeFilter = surface.GetByRole(AriaRole.Button, new() { Name = "Loại đơn", Exact = true });
            await orderTypeFilter.ClickAsync();
            await Page.GetByRole(AriaRole.Option, new() { Name = "Đơn thường", Exact = true }).ClickAsync();
            await Assertions.Expect(surface.Locator("thead").GetByText("Loại đơn", new() { Exact = true }))
                .ToBeVisibleAsync();
            var firstOrderTypeChips = surface.Locator("tbody tr").First.Locator(".vpp-settlement-order-count .vpp-category-chip");
            await Assertions.Expect(firstOrderTypeChips.First).ToContainTextAsync("Đơn thường");
            await Assertions.Expect(firstOrderTypeChips)
                .ToHaveCountAsync(1);
            await orderTypeFilter.ClickAsync();
            await Page.GetByRole(AriaRole.Option, new() { Name = "Tất cả loại đơn", Exact = true }).ClickAsync();

            var statusFilter = surface.GetByRole(AriaRole.Button, new() { Name = "Trạng thái", Exact = true });
            await statusFilter.ClickAsync();
            await Page.GetByRole(AriaRole.Option, new() { Name = "Đã gửi", Exact = true }).ClickAsync();
            await Assertions.Expect(surface.Locator("tbody tr").First.Locator(".vpp-settlement-status-count .vpp-status-badge"))
                .ToHaveCountAsync(1);
            await Assertions.Expect(surface.Locator("tfoot .vpp-settlement-status-count .vpp-status-badge"))
                .ToHaveCountAsync(1);
            await statusFilter.ClickAsync();
            await Page.GetByRole(AriaRole.Option, new() { Name = "Tất cả trạng thái", Exact = true }).ClickAsync();

            var search = surface.GetByPlaceholder("Tìm phòng ban hoặc mã đơn", new() { Exact = true });
            await search.FillAsync("không-có-dữ-liệu-tổng-hợp");
            await Assertions.Expect(surface.GetByTestId("settlement-summary-label"))
                .ToHaveTextAsync("Tổng sau lọc");
            await AssertSettlementSummaryFooterAsync(surface);
            await Assertions.Expect(surface.Locator("tfoot .vpp-settlement-summary-value"))
                .ToHaveTextAsync(["0", "0", "0", "0", "0"]);
            await surface.GetByRole(AriaRole.Button, new() { Name = "Xóa bộ lọc", Exact = true }).ClickAsync();
            await Assertions.Expect(surface.GetByTestId("settlement-summary-label")).ToHaveTextAsync("Tổng cộng");
        }

        await Page.GetByRole(AriaRole.Button, new() { Name = "Theo người đặt", Exact = true }).ClickAsync();
        await Assertions.Expect(surface.GetByPlaceholder("Tìm người dùng hoặc mã đơn", new() { Exact = true }))
            .ToBeVisibleAsync();
        await AssertColumnsAsync(surface, ["Người dùng", "Phòng ban", "Trạng thái", "Thành tiền", "VAT", "Tổng cộng"]);
        await AssertCombinedOrderCountColumnAsync(surface);
        await AssertNumericColumnsAlignedAsync(surface);
        await AssertSettlementSummaryFooterAsync(surface);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Theo mặt hàng", Exact = true }).ClickAsync();
        await Assertions.Expect(surface.GetByPlaceholder("Tìm mã hoặc tên mặt hàng", new() { Exact = true }))
            .ToBeVisibleAsync();
        await AssertColumnsAsync(surface, ["Đơn giá", "Thuế VAT", "Thành tiền", "Tổng cộng"]);
        await Assertions.Expect(surface.Locator("thead").GetByText("Độ phủ", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await AssertNumericColumnsAlignedAsync(surface);
        await AssertSettlementSummaryFooterAsync(surface);
        await CaptureAsync($"settlement-items-{width}x{height}.png");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Theo phòng ban", Exact = true }).ClickAsync();
        var viewOrders = surface.GetByRole(AriaRole.Button, new() { Name = "Xem đơn", Exact = true }).First;
        await viewOrders.ClickAsync();
        var drawer = Page.Locator(".vpp-settlement-detail-drawer.is-open");
        await drawer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var groupPicker = drawer.GetByRole(AriaRole.Button, new() { Name = "Đơn trong nhóm", Exact = true });
        if (await groupPicker.CountAsync() > 0)
        {
            await Assertions.Expect(drawer.GetByText("Tất cả đơn trong nhóm", new() { Exact = true }).First)
                .ToBeVisibleAsync();
            await groupPicker.ClickAsync();
            var selectedGroup = Page.GetByRole(
                AriaRole.Option,
                new() { Name = "Tất cả đơn trong nhóm", Exact = true });
            await Assertions.Expect(selectedGroup).ToHaveAttributeAsync("aria-selected", "true");
            var selectedGroupBackground = await selectedGroup.EvaluateAsync<string>(
                "element => getComputedStyle(element).backgroundColor");
            selectedGroupBackground.Should().Be("rgba(0, 0, 0, 0)");
            await selectedGroup.ClickAsync();
            await CaptureAsync($"settlement-group-all-{width}x{height}.png");
        }

        var correctionAction = drawer.GetByRole(
            AriaRole.Button,
            new() { NameRegex = new Regex("Điều chỉnh sau chốt|Sửa đơn", RegexOptions.IgnoreCase) });
        await Assertions.Expect(correctionAction).ToBeVisibleAsync();
        await correctionAction.ClickAsync();
        var dialog = Page.GetByTestId("post-settlement-order-correction-dialog");
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var actionPicker = dialog.Locator(".vpp-decision-select-trigger");
        await Assertions.Expect(actionPicker).ToBeVisibleAsync();
        await actionPicker.ClickAsync();
        var actionPopoverId = await actionPicker.GetAttributeAsync("aria-controls");
        actionPopoverId.Should().NotBeNullOrWhiteSpace();
        var actionPopover = Page.Locator($"#{actionPopoverId}");
        await actionPopover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var selectedAction = actionPopover.Locator("[role='option'][aria-selected='true']");
        await Assertions.Expect(selectedAction).ToBeVisibleAsync();
        await selectedAction.ClickAsync();
        await actionPopover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Assertions.Expect(dialog.Locator(".vpp-order-correction-items .rz-numeric").First)
            .ToBeVisibleAsync();
        await AssertElementInsideViewportAsync(dialog);
        await CaptureAsync($"settlement-order-correction-dialog-{width}x{height}.png");
        await dialog.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).ClickAsync();
        await dialog.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await drawer.GetByRole(AriaRole.Button, new() { Name = "Đóng", Exact = true }).ClickAsync();

        var pageSize = surface.Locator(".rz-paginator .rz-dropdown, .rz-pager .rz-dropdown").Last;
        await Assertions.Expect(pageSize).ToBeVisibleAsync();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            await pageSize.ClickAsync();
            var panel = Page.Locator(".rz-dropdown-panel.vpp-page-size-panel:visible").Last;
            await Assertions.Expect(panel).ToBeVisibleAsync();
            var triggerBox = await pageSize.BoundingBoxAsync();
            var panelBox = await panel.BoundingBoxAsync();
            triggerBox.Should().NotBeNull();
            panelBox.Should().NotBeNull();
            RectanglesOverlap(triggerBox!, panelBox!).Should().BeFalse();
            if (attempt == 0)
            {
                await CaptureAsync($"settlement-page-size-{width}x{height}.png");
            }

            await pageSize.ClickAsync();
            await Assertions.Expect(panel).ToBeHiddenAsync();
        }

        if (width == 1366)
        {
            await SetDarkModeAsync(false);
        }
    }

    private async Task SetDarkModeAsync(bool darkMode)
    {
        var currentMode = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('rz-theme-dark')");
        if (currentMode == darkMode)
        {
            return;
        }

        await Page.Locator(".user-menu-trigger").First.ClickAsync();
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            darkMode
                ? "() => document.documentElement.classList.contains('rz-theme-dark')"
                : "() => !document.documentElement.classList.contains('rz-theme-dark')");
        var backdrop = Page.Locator(".user-dropdown-backdrop:visible");
        if (await backdrop.CountAsync() > 0)
        {
            await backdrop.ClickAsync();
            await backdrop.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await WaitForRenderSettleAsync();
    }

    private static async Task AssertColumnsAsync(ILocator surface, IReadOnlyList<string> expected)
    {
        foreach (var column in expected)
        {
            await Assertions.Expect(surface.Locator("thead").GetByText(column, new() { Exact = true }))
                .ToBeVisibleAsync();
        }
    }

    private static async Task AssertCombinedOrderCountColumnAsync(ILocator surface)
    {
        await Assertions.Expect(surface.Locator("thead").GetByText("Đơn thường", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(surface.Locator("thead").GetByText("Đơn bổ sung", new() { Exact = true }))
            .ToHaveCountAsync(0);

        var firstOrderCountCell = surface.Locator("tbody .vpp-settlement-order-count").First;
        await Assertions.Expect(firstOrderCountCell).ToBeVisibleAsync();
        await Assertions.Expect(firstOrderCountCell.Locator(".vpp-category-chip")).ToHaveCountAsync(2);
        await Assertions.Expect(firstOrderCountCell.GetByText(new Regex("^Đơn thường: \\d+$"))).ToBeVisibleAsync();
        await Assertions.Expect(firstOrderCountCell.GetByText(new Regex("^Đơn bổ sung: \\d+$"))).ToBeVisibleAsync();

        var geometryErrors = await surface.EvaluateAsync<string[]>("""
            element => {
                const orderHeader = [...element.querySelectorAll('thead th')]
                    .find(cell => cell.textContent?.trim() === 'Tổng đơn');
                const statusHeader = [...element.querySelectorAll('thead th')]
                    .find(cell => cell.textContent?.trim() === 'Trạng thái');
                const bodyCounts = element.querySelector('tbody .vpp-settlement-order-count');
                const footerCounts = element.querySelector('tfoot .vpp-settlement-order-count');
                if (!orderHeader || !statusHeader || !bodyCounts || !footerCounts) {
                    return ['Thiếu cột tổng đơn, trạng thái hoặc dòng tổng hợp để kiểm tra hình học.'];
                }

                const orderRect = orderHeader.getBoundingClientRect();
                const statusRect = statusHeader.getBoundingClientRect();
                const bodyRect = bodyCounts.getBoundingClientRect();
                const footerRect = footerCounts.getBoundingClientRect();
                const messages = [];

                if (orderRect.width < 196 || orderRect.width > 204) {
                    messages.push(`Cột tổng đơn chưa compact (${orderRect.width}px).`);
                }
                if (Math.abs(statusRect.left - orderRect.right) > 1) {
                    messages.push('Cột tổng đơn và trạng thái còn khoảng trống bất thường.');
                }
                if (Math.abs(bodyRect.left - footerRect.left) > 1) {
                    messages.push('Badge dòng dữ liệu và dòng tổng hợp chưa thẳng cột.');
                }
                const bodyCenter = bodyRect.left + bodyRect.width / 2;
                const footerCenter = footerRect.left + footerRect.width / 2;
                const headerCenter = orderRect.left + orderRect.width / 2;
                if (Math.abs(bodyCenter - headerCenter) > 1 || Math.abs(footerCenter - headerCenter) > 1) {
                    messages.push('Badge tổng đơn chưa cùng tâm với tiêu đề cột.');
                }
                return messages;
            }
            """);

        geometryErrors.Should().BeEmpty(string.Join(Environment.NewLine, geometryErrors));
    }

    private static async Task AssertFilterColumnOrderAsync(ILocator surface)
    {
        var errors = await surface.EvaluateAsync<string[]>(
            """
            element => {
                const checks = [
                    ['.vpp-settlement-filter-department', 'Phòng ban'],
                    ['.vpp-settlement-filter-order-type', 'Tổng đơn'],
                    ['.vpp-settlement-filter-status', 'Trạng thái']
                ];
                const headers = [...element.querySelectorAll('thead th')];
                const messages = [];
                let lastFilterLeft = -Infinity;
                let lastHeaderLeft = -Infinity;
                for (const [selector, title] of checks) {
                    const filter = element.querySelector(selector);
                    const header = headers.find(cell => cell.textContent?.trim() === title);
                    if (!filter || !header) {
                        messages.push(`Thiếu filter hoặc cột ${title}.`);
                        continue;
                    }
                    const filterLeft = filter.getBoundingClientRect().left;
                    const headerLeft = header.getBoundingClientRect().left;
                    if (filterLeft <= lastFilterLeft || headerLeft <= lastHeaderLeft) {
                        messages.push(`Thứ tự filter/cột ${title} chưa đồng nhất.`);
                    }
                    lastFilterLeft = filterLeft;
                    lastHeaderLeft = headerLeft;
                }
                return messages;
            }
            """);

        errors.Should().BeEmpty(string.Join(Environment.NewLine, errors));
    }

    private static async Task AssertNumericColumnsAlignedAsync(ILocator surface)
    {
        var errors = await surface.EvaluateAsync<string[]>("""
            element => {
                const headers = [...element.querySelectorAll('thead th.vpp-settlement-number')];
                const firstRow = element.querySelector('tbody tr');
                const cells = firstRow
                    ? [...firstRow.querySelectorAll('td.vpp-settlement-number')]
                    : [];

                return headers.flatMap((header, index) => {
                    const cell = cells[index];
                    const wrapper = header.querySelector(':scope > div:not(.rz-cell-filter)');
                    if (!cell || !wrapper) return [`Cột số ${index + 1} thiếu header hoặc ô dữ liệu.`];

                    const wrapperStyle = getComputedStyle(wrapper);
                    const cellStyle = getComputedStyle(cell);
                    const messages = [];

                    if (wrapperStyle.justifyContent !== 'flex-end') {
                        messages.push(`Cột ${index + 1}: tiêu đề chưa canh phải (${wrapperStyle.justifyContent}).`);
                    }
                    if (cellStyle.textAlign !== 'right') {
                        messages.push(`Cột ${index + 1}: dữ liệu chưa canh phải (${cellStyle.textAlign}).`);
                    }
                    return messages;
                });
            }
            """);

        errors.Should().BeEmpty(string.Join(Environment.NewLine, errors));
    }

    private static async Task AssertCompactRowRhythmAsync(ILocator surface)
    {
        var rowHeights = await surface.Locator("tbody tr").EvaluateAllAsync<double[]>(
            "rows => rows.slice(0, 8).map(row => row.getBoundingClientRect().height)");

        if (rowHeights.Length == 0)
        {
            return;
        }

        rowHeights.Should().OnlyContain(
            height => height >= 38 && height <= 52,
            "dòng có tên và mã phụ vẫn phải nằm trong nhịp two-line chuẩn, không bị kéo giãn theo chiều cao grid");
        (rowHeights.Max() - rowHeights.Min()).Should().BeLessThanOrEqualTo(
            2,
            "các dòng cùng loại dữ liệu phải có chiều cao thị giác đồng đều");
    }

    private static async Task AssertSettlementSummaryFooterAsync(ILocator surface)
    {
        var errors = await surface.Locator(".vpp-settlement-order-grid:visible").First.EvaluateAsync<string[]>(
            """
            grid => {
                const footer = grid.querySelector('tfoot tr');
                const pager = grid.querySelector('.rz-paginator, .rz-pager');
                const label = footer?.querySelector('[data-testid="settlement-summary-label"]');
                const headers = [...grid.querySelectorAll('thead th')];
                const footerCells = footer ? [...footer.querySelectorAll(':scope > td')] : [];
                const styles = getComputedStyle(grid);
                const expectedRowHeight = Number.parseFloat(styles.getPropertyValue('--vpp-data-row-compact-height'));
                const expectedPagerHeight = Number.parseFloat(styles.getPropertyValue('--vpp-data-footer-height'));
                const messages = [];

                if (!footer || !pager || !label) {
                    return ['Thiếu dòng tổng hợp, nhãn tổng hoặc pager của grid Chốt kỳ.'];
                }

                const footerBox = footer.getBoundingClientRect();
                const pagerBox = pager.getBoundingClientRect();
                const footerCell = label.closest('td') ?? footer.querySelector('td');
                const footerCellIndex = footerCell ? footerCells.indexOf(footerCell) : -1;
                const lastBodyRow = grid.querySelector('tbody tr:last-child');
                const lastBodyCell = footerCellIndex >= 0
                    ? lastBodyRow?.children[footerCellIndex]
                    : lastBodyRow?.querySelector('td');
                if (Math.abs(footerBox.height - expectedRowHeight) > 1) {
                    messages.push(`Dòng tổng hợp cao ${footerBox.height}px, chuẩn ${expectedRowHeight}px.`);
                }
                if (Math.abs(pagerBox.height - expectedPagerHeight) > 1) {
                    messages.push(`Pager cao ${pagerBox.height}px, chuẩn ${expectedPagerHeight}px.`);
                }
                if (footerCells.length !== headers.length) {
                    messages.push(`Dòng tổng hợp có ${footerCells.length} ô nhưng header có ${headers.length} cột.`);
                }
                if (pagerBox.top + 1 < footerBox.bottom || pagerBox.top - footerBox.bottom > 20) {
                    messages.push(`Dòng tổng hợp chưa nằm sát trên pager (gap ${pagerBox.top - footerBox.bottom}px).`);
                }
                if (!label.textContent?.trim()) {
                    messages.push('Nhãn dòng tổng hợp đang trống.');
                }
                if (footerCell && Number.parseFloat(getComputedStyle(footerCell).borderTopWidth) > 0) {
                    messages.push('Dòng tổng hợp vẫn tự vẽ border trên, gây chồng separator với dòng dữ liệu cuối.');
                }
                if (lastBodyCell && footerCell
                    && getComputedStyle(lastBodyCell).backgroundColor === getComputedStyle(footerCell).backgroundColor) {
                    messages.push(`Nền dòng tổng hợp chưa phân biệt với dòng dữ liệu (footer=${getComputedStyle(footerCell).backgroundColor}, body=${getComputedStyle(lastBodyCell).backgroundColor}, footerClass=${footerCell.className}, bodyClass=${lastBodyCell.className}, column=${footerCellIndex}).`);
                }

                const numericCells = footerCells.filter(cell => cell.classList.contains('vpp-settlement-number'));
                if (numericCells.some(cell => getComputedStyle(cell).textAlign !== 'right')) {
                    messages.push('Có ô số trong dòng tổng hợp chưa canh phải.');
                }
                const actionCell = footer.querySelector('td.rz-col-actions');
                if (actionCell) {
                    const actionStyle = getComputedStyle(actionCell);
                    const separatorWidth = Number.parseFloat(actionStyle.borderInlineStartWidth || actionStyle.borderLeftWidth);
                    const previousCell = actionCell.previousElementSibling;
                    const previousStyle = previousCell ? getComputedStyle(previousCell) : null;
                    const previousSeparatorWidth = previousStyle
                        ? Number.parseFloat(previousStyle.borderInlineEndWidth || previousStyle.borderRightWidth)
                        : 0;
                    const beforeStyle = getComputedStyle(actionCell, '::before');
                    const afterStyle = getComputedStyle(actionCell, '::after');
                    const hasPseudoSeparator = [beforeStyle, afterStyle].some(style =>
                        style.display !== 'none'
                        && (Number.parseFloat(style.width) > 0 || Number.parseFloat(style.borderLeftWidth) > 0));
                    if (separatorWidth > 0
                        || previousSeparatorWidth > 0
                        || actionStyle.boxShadow !== 'none'
                        || hasPseudoSeparator) {
                        messages.push('Separator cột thao tác vẫn đè lên dòng tổng hợp.');
                    }
                    if (actionStyle.position === 'sticky') {
                        messages.push('Ô thao tác của dòng tổng hợp vẫn bị frozen/sticky và có thể đè lên dải tổng.');
                    }
                }
                return messages;
            }
            """);

        errors.Should().BeEmpty(string.Join(Environment.NewLine, errors));
    }

    private static async Task AssertElementInsideViewportAsync(ILocator element)
    {
        var geometry = await element.EvaluateAsync<double[]>(
            """
            node => {
                const rect = node.getBoundingClientRect();
                return [rect.left, rect.right, rect.top, rect.bottom, innerWidth, innerHeight];
            }
            """);
        geometry[0].Should().BeGreaterThanOrEqualTo(-1);
        geometry[1].Should().BeLessThanOrEqualTo(geometry[4] + 1);
        geometry[2].Should().BeGreaterThanOrEqualTo(-1);
        geometry[3].Should().BeLessThanOrEqualTo(geometry[5] + 1);
    }

    private async Task CaptureAsync(string fileName)
    {
        var configured = Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return;
        }

        var directory = Path.GetFullPath(configured);
        Directory.CreateDirectory(directory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }

    private static bool RectanglesOverlap(LocatorBoundingBoxResult first, LocatorBoundingBoxResult second) =>
        first.X < second.X + second.Width
        && first.X + first.Width > second.X
        && first.Y < second.Y + second.Height
        && first.Y + first.Height > second.Y;
}
