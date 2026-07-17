using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_test_support;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class OrderManagementTests : TestBase, IMutatingUiTest
{
    private const string ApprovalReason = "LEAN-05 bổ sung cần duyệt";
    private const string RejectionReason = "LEAN-05 bổ sung cần từ chối";
    private const string ManagerRejectionReason = "Thiếu căn cứ số lượng LEAN-05";

    [Fact]
    public async Task Employees_CreateSupplements_ManagerApprovesAndRejects_WithAuditableEndStates()
    {
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
        await CreateSupplementAsync(ApprovalReason, verifyRequiredReason: true);

        await SwitchUserAsync(TestAccounts.DepartmentPeer);
        await CreateSupplementAsync(RejectionReason, verifyRequiredReason: false);

        await SwitchUserAsync(TestAccounts.Manager);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=pending");
        var approvalGrid = Page.Locator(".vpp-data-card.vpp-datagrid:visible");
        await approvalGrid.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        var approveRow = FindQueueRow(approvalGrid, ApprovalReason);
        await approveRow.WaitForAsync();
        await (await GetInteractiveButtonAsync(approveRow, new Regex("^Duyệt đơn "))).ClickAsync();
        var approveDialog = Page.Locator(".rz-dialog:visible").Last;
        await approveDialog.GetByText("Duyệt đơn", new() { Exact = true }).WaitForAsync();
        await approveDialog.GetByRole(AriaRole.Button, new() { Name = "Có", Exact = true }).ClickAsync();
        await Page.GetByText("Đã duyệt đơn thành công.", new() { Exact = false }).WaitForAsync();

        var rejectRow = FindQueueRow(approvalGrid, RejectionReason);
        await rejectRow.WaitForAsync();
        await (await GetInteractiveButtonAsync(rejectRow, new Regex("^Từ chối đơn "))).ClickAsync();
        var rejectDialog = Page.Locator(".rz-dialog:visible").Last;
        await rejectDialog.GetByText("Từ chối đơn", new() { Exact = true }).WaitForAsync();
        await rejectDialog.GetByRole(AriaRole.Button, new() { Name = "Từ chối", Exact = true }).ClickAsync();
        await rejectDialog.GetByText("Vui lòng nhập lý do từ chối.", new() { Exact = false }).WaitForAsync();
        await rejectDialog.Locator("textarea[name='RejectReason']").FillAsync(ManagerRejectionReason);
        await rejectDialog.GetByRole(AriaRole.Button, new() { Name = "Từ chối", Exact = true }).ClickAsync();
        await Page.GetByText("Đã từ chối đơn thành công.", new() { Exact = false }).WaitForAsync();

        await Page.GetByText("Không có mục chờ", new() { Exact = false }).WaitForAsync();
        await Page.GetByText("Không có đơn bổ sung nào cần duyệt.", new() { Exact = false }).WaitForAsync();

        await AssertSupplementEndStateAsync(TestAccounts.Employee, "Đã duyệt", ApprovalReason, "Duyệt đơn");
        await AssertSupplementEndStateAsync(TestAccounts.DepartmentPeer, "Đã từ chối", RejectionReason, "Từ chối đơn");

        failedRequests.Should().BeEmpty();
        consoleErrors.Should().BeEmpty();
    }

    private async Task CreateSupplementAsync(string reason, bool verifyRequiredReason)
    {
        Exception? navigationFailure = null;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
            var orderPage = Page.Locator(".order-page:visible");
            await orderPage.WaitForAsync();
            try
            {
                var createSupplementButton = await GetInteractiveButtonAsync(
                    orderPage,
                    "Tạo đơn bổ sung");
                await createSupplementButton.ClickAsync();
                navigationFailure = null;
                break;
            }
            catch (Exception exception) when (exception is TimeoutException or InvalidOperationException)
            {
                navigationFailure = exception;
            }
        }

        if (navigationFailure is not null)
        {
            var notifications = await Page.Locator(".rz-notification, .rz-growl")
                .AllInnerTextsAsync();
            throw new InvalidOperationException(
                $"The interactive supplement action did not navigate. URL: {Page.Url}. " +
                $"Notifications: {string.Join(" | ", notifications)}. " +
                $"Page: {await Page.Locator("body").InnerTextAsync()}",
                navigationFailure);
        }
        await WaitForUrlMatchAsync(
            new Regex(".*/dashboard/order-create\\?isAdditional=True.*", RegexOptions.IgnoreCase),
            TimeSpan.FromSeconds(120));
        var wizard = Page.Locator(".vpp-wizard:visible");
        await wizard.WaitForAsync();

        await (await GetInteractiveButtonAsync(wizard, "Thêm sản phẩm vào đơn")).ClickAsync();
        await (await GetInteractiveButtonAsync(wizard, "Tiếp theo")).ClickAsync();

        if (verifyRequiredReason)
        {
            await (await GetInteractiveButtonAsync(wizard, "Gửi duyệt")).ClickAsync();
            await Page.GetByText(
                    "Vui lòng nhập lý do cho đơn bổ sung trước khi tiếp tục.",
                    new() { Exact = false })
                .WaitForAsync();
        }

        await (await GetInteractiveButtonAsync(wizard, "Quay lại")).ClickAsync();
        await wizard.GetByLabel("Trường này bắt buộc với đơn bổ sung.", new() { Exact = true })
            .Last.FillAsync(reason);
        await (await GetInteractiveButtonAsync(wizard, "Tiếp theo")).ClickAsync();
        await (await GetInteractiveButtonAsync(wizard, "Gửi duyệt")).ClickAsync();

        await WaitForUrlMatchAsync(
            new Regex(".*/dashboard\\?tab=0.*", RegexOptions.IgnoreCase),
            TimeSpan.FromSeconds(30));
        var supplementCard = Page.Locator(".vpp-data-card-grid-shell:visible").Last;
        await supplementCard.GetByText("Bổ sung", new() { Exact = true }).WaitForAsync();
        await supplementCard.GetByText("Chờ duyệt", new() { Exact = true }).WaitForAsync();
    }

    private ILocator FindQueueRow(ILocator grid, string reason)
        => grid.Locator("tbody tr", new LocatorLocatorOptions { HasText = reason });

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

    private async Task AssertSupplementEndStateAsync(
        QaTestAccount account,
        string expectedStatus,
        string creationReason,
        string expectedDecisionAction)
    {
        await SwitchUserAsync(account);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
        await Page.Locator(".order-page").WaitForAsync();
        var supplementCard = Page.Locator(".vpp-data-card-grid-shell:visible").Last;
        await supplementCard.GetByText("Bổ sung", new() { Exact = true }).WaitForAsync();
        await supplementCard.GetByText(expectedStatus, new() { Exact = true }).WaitForAsync();

        await supplementCard.Locator("button[title='Xem lịch sử phiên bản']").ClickAsync();
        var historyDialog = Page.Locator(".rz-dialog:visible").Last;
        await historyDialog.GetByText("Vòng đời đơn yêu cầu", new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByText(creationReason, new() { Exact = false }).First.WaitForAsync();
        await historyDialog.GetByText(expectedDecisionAction, new() { Exact = false }).WaitForAsync();
        await historyDialog.GetByRole(AriaRole.Button, new() { Name = "Đóng", Exact = true }).ClickAsync();
    }
}
