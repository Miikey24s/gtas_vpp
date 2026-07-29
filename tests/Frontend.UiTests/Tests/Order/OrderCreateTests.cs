using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class OrderCreateTests : TestBase, IMutatingUiTest
{
    [Fact]
    public async Task Employee_CanEditCancelAndInspectRegularLifecycle()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        var consoleErrors = new List<string>();
        var failedRequests = new List<string>();
        Page.Console += (_, message) =>
        {
            var isExpectedCircuitTransitionError = message.Text.Contains(
                    "Failed to complete negotiation with the server",
                    StringComparison.OrdinalIgnoreCase)
                && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
            if (message.Type == "error" && !isExpectedCircuitTransitionError)
            {
                consoleErrors.Add(message.Text);
            }
        };
        Page.RequestFailed += (_, request) =>
        {
            var isExpectedCircuitTransition = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && (uri.AbsolutePath.Equals("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/negotiate", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/initializers", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            var isExpectedNavigationAssetAbort = uri is not null
                && (uri.AbsolutePath.Contains("/favicon.", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Contains("/images/vpp-app-icon.", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Contains("/Components/Layout/ReconnectModal.", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith("/images/login-bg-optimized.jpeg", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitTransition && !isExpectedNavigationAssetAbort)
            {
                failedRequests.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
        var orderCard = Page.Locator("[data-testid='current-order-panel']:visible").Last;
        await orderCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.GetByText("Đơn đã gửi", new() { Exact = true }).WaitForAsync();
        await orderCard.Locator(".vpp-order-items-grid tbody tr").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        // Canonical order-items column order: # · Mặt hàng · Danh mục · Đơn vị · Số lượng · Ghi chú.
        var columnGeometry = await orderCard.Locator(".vpp-order-items-grid").EvaluateAsync<double[]>("""
            grid => {
                const headers = [...grid.querySelectorAll('thead th')];
                const dataRow = [...grid.querySelectorAll('tbody tr')]
                    .find(row => row.querySelectorAll('td').length >= 5);
                const cells = [...dataRow.querySelectorAll('td')];
                const textRect = element => {
                    const range = document.createRange();
                    range.selectNodeContents(element);
                    return range.getBoundingClientRect();
                };
                const quantityHeader = textRect(headers[4].querySelector('.rz-column-title'));
                const quantityCell = textRect(cells[4].querySelector('.rz-cell-data'));
                const uomHeader = textRect(headers[3].querySelector('.rz-column-title'));
                const uomCell = textRect(cells[3].querySelector('.rz-cell-data'));
                const center = rect => rect.left + rect.width / 2;
                return [
                    Math.abs(quantityHeader.right - quantityCell.right),
                    Math.abs(center(uomHeader) - center(uomCell)),
                    headers[0].getBoundingClientRect().width,
                    headers[1].getBoundingClientRect().width,
                    headers[2].getBoundingClientRect().width,
                    headers[3].getBoundingClientRect().width,
                    headers[4].getBoundingClientRect().width,
                    headers[5].getBoundingClientRect().width,
                    grid.getBoundingClientRect().width
                ];
            }
            """);
        columnGeometry[0].Should().BeLessThanOrEqualTo(1.5, "quantity header and values should share the same right axis");
        columnGeometry[1].Should().BeLessThanOrEqualTo(1.5, "UOM header and values should share the same center axis");
        var gridWidth = columnGeometry[8];
        (columnGeometry[2] / gridWidth).Should().BeApproximately(0.05, 0.01, "the row-number track should use the canonical 5% ratio");
        (columnGeometry[3] / gridWidth).Should().BeApproximately(0.31, 0.015, "the item track should remain the largest flexible data track");
        (columnGeometry[4] / gridWidth).Should().BeApproximately(0.18, 0.01, "the category track should use the canonical 18% ratio");
        (columnGeometry[5] / gridWidth).Should().BeApproximately(0.12, 0.01, "the UOM track should use the canonical 12% ratio");
        (columnGeometry[6] / gridWidth).Should().BeApproximately(0.14, 0.01, "the quantity track should use the canonical 14% ratio");
        (columnGeometry[7] / gridWidth).Should().BeApproximately(0.20, 0.01, "the note track should use the canonical 20% ratio");
        (await Page.Locator(".vpp-orders-story-heading p").CountAsync()).Should().Be(0,
            "a submitted order should not repeat navigation or processing guidance below the takeaway");
        (await Page.Locator(".vpp-orders-summary-grid article").CountAsync()).Should().Be(3,
            "the submitted story should keep regular, supplement and previous-cycle context visible");
        await Page.GetByText("Đơn kỳ hiện tại", new() { Exact = true }).Last.WaitForAsync();

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "w1-dashboard-my-orders-submitted-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        var editButton = Page.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Chỉnh sửa đơn ")
        }).Last;
        await editButton.ClickAsync();
        await WaitForUrlMatchAsync(
            new System.Text.RegularExpressions.Regex(".*/dashboard/order-create.*orderId=.*"),
            TimeSpan.FromSeconds(30));
        await Page.Locator(".vpp-wizard").WaitForAsync();

        var quantityStepper = Page.Locator(".vpp-order-draft-item .vpp-order-quantity-stepper").First;
        await quantityStepper.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        var quantityInput = quantityStepper.Locator("input[type=number]");
        await quantityInput.FillAsync("4");
        await quantityInput.PressAsync("Tab");
        (await quantityInput.InputValueAsync()).Should().Be("4");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tiếp tục" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cập nhật đơn" }).ClickAsync();
        await WaitForUrlMatchAsync(
            new System.Text.RegularExpressions.Regex(".*/dashboard\\?tab=0.*"),
            TimeSpan.FromSeconds(30));
        orderCard = Page.Locator("[data-testid='current-order-panel']:visible").Last;
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Xem lịch sử phiên bản của đơn ")
        }).Last.ClickAsync();
        var historyDialog = Page.Locator(".rz-dialog:visible").Last;
        await historyDialog.GetByText("Vòng đời đơn yêu cầu", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByText("Phiên bản 2", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByText("Cập nhật đơn", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByRole(AriaRole.Button, new() { Name = "Đóng" }).ClickAsync();

        orderCard = Page.Locator("[data-testid='current-order-panel']:visible").Last;
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Hủy đơn ")
        }).Last.ClickAsync();
        var cancelDialog = Page.Locator(".rz-dialog:visible").Last;
        await cancelDialog.GetByText("Xác nhận hủy đơn", new() { Exact = false }).WaitForAsync();
        await cancelDialog.GetByRole(AriaRole.Button, new() { Name = "Hủy đơn" }).ClickAsync();
        await Page.GetByText("Đã hủy đơn.", new() { Exact = false })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        orderCard = Page.Locator("[data-testid='current-order-panel']:visible").Last;
        await orderCard.GetByText("Đã hủy", new() { Exact = false }).WaitForAsync();
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Tạo phiên bản thay thế cho đơn ")
        }).WaitForAsync();
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Xem lịch sử phiên bản của đơn ")
        }).Last.ClickAsync();
        historyDialog = Page.Locator(".rz-dialog:visible").Last;
        await historyDialog.GetByText("Phiên bản 3", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByText("Hủy đơn", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByRole(AriaRole.Button, new() { Name = "Đóng" }).ClickAsync();

        failedRequests.Should().BeEmpty();
        consoleErrors.Should().BeEmpty();
    }

    private async Task WaitForUrlMatchAsync(Regex expectedUrl, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (expectedUrl.IsMatch(Page.Url))
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException(
            $"Timed out waiting for URL '{expectedUrl}'. Last URL: {Page.Url}");
    }
}
