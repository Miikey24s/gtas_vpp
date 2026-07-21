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
        var orderCard = Page.Locator(".vpp-data-card-grid-shell:visible").Last;
        await orderCard.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.GetByText("Đơn đã gửi", new() { Exact = true }).WaitForAsync();
        await orderCard.Locator(".vpp-order-grid tbody tr").First.WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await Page.Locator(".vpp-orders-story-heading p").CountAsync()).Should().Be(0,
            "a submitted order should not repeat navigation or processing guidance below the takeaway");
        (await Page.Locator(".vpp-orders-evidence article").CountAsync()).Should().Be(4,
            "the submitted story should keep one balanced row of non-duplicated evidence");
        await Page.GetByText("Chi tiết đơn", new() { Exact = true }).WaitForAsync();

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

        var quantity = Page.Locator(".vpp-wizard-split-right input[role='spinbutton']").First;
        await quantity.FillAsync("4");
        await quantity.PressAsync("Tab");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Tiếp theo" }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Cập nhật đơn" }).ClickAsync();
        await WaitForUrlMatchAsync(
            new System.Text.RegularExpressions.Regex(".*/dashboard\\?tab=0.*"),
            TimeSpan.FromSeconds(30));
        orderCard = Page.Locator(".vpp-data-card-grid-shell:visible").Last;
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Xem lịch sử phiên bản của đơn ")
        }).Last.ClickAsync();
        var historyDialog = Page.Locator(".rz-dialog:visible").Last;
        await historyDialog.GetByText("Vòng đời đơn yêu cầu", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByText("Phiên bản 2", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByText("Cập nhật đơn", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByRole(AriaRole.Button, new() { Name = "Đóng" }).ClickAsync();

        orderCard = Page.Locator(".vpp-data-card-grid-shell:visible").Last;
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Hủy đơn ")
        }).Last.ClickAsync();
        var cancelDialog = Page.Locator(".rz-dialog:visible").Last;
        await cancelDialog.GetByText("Xác nhận hủy đơn", new() { Exact = false }).WaitForAsync();
        await cancelDialog.GetByRole(AriaRole.Button, new() { Name = "Hủy đơn" }).ClickAsync();
        await Page.GetByText("Đã hủy đơn.", new() { Exact = false })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        orderCard = Page.Locator(".vpp-data-card-grid-shell:visible").Last;
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
