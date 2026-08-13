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
        await Assertions.Expect(Page.Locator(".vpp-settlement-kpi-card.is-select > small"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByRole(
                AriaRole.Button,
                new() { Name = "Tạo phiên bản hiệu chỉnh", Exact = true }))
            .ToHaveCountAsync(0);

        var supplierPicker = Page.GetByRole(
            AriaRole.Button,
            new() { Name = "Chọn nhà cung cấp", Exact = true });
        await supplierPicker.ClickAsync();
        var selectedSupplier = Page.Locator(
            ".vpp-decision-select-popover:popover-open [role='option'][aria-selected='true']");
        await Assertions.Expect(selectedSupplier).ToBeVisibleAsync();
        await Assertions.Expect(selectedSupplier.Locator(".vpp-icon")).ToBeVisibleAsync();
        await selectedSupplier.ClickAsync();

        await AssertColumnsAsync(surface, ["Tạm tính", "Thuế GTGT (VAT)", "Thành tiền (gồm VAT)"]);
        await AssertCompactRowRhythmAsync(surface);
        await Assertions.Expect(surface.Locator("thead").GetByText("Trạng thái", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await CaptureAsync($"settlement-departments-{width}x{height}.png");
        await AssertNumericColumnsAlignedAsync(surface);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Theo người đặt", Exact = true }).ClickAsync();
        await AssertColumnsAsync(surface, ["Tạm tính", "Thuế GTGT (VAT)", "Thành tiền (gồm VAT)"]);
        await AssertNumericColumnsAlignedAsync(surface);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Theo mặt hàng", Exact = true }).ClickAsync();
        await AssertColumnsAsync(surface, ["Đơn giá", "Thuế VAT", "Tạm tính", "Thành tiền (gồm VAT)"]);
        await Assertions.Expect(surface.Locator("thead").GetByText("Độ phủ", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await AssertNumericColumnsAlignedAsync(surface);
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
