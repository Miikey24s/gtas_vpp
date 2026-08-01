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
                    || uri.AbsolutePath.StartsWith("/_framework/", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Contains("/images/vpp-app-icon.", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Contains("/Components/Layout/ReconnectModal.", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith("/images/login-bg-optimized.jpeg", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    // Blazor enhanced navigation legitimately cancels in-flight static
                    // asset requests (e.g. fingerprinted /js/vpp-interactions.<hash>.js)
                    // when a new navigation starts. Only ERR_ABORTED is expected here;
                    // any other failure kind for these assets still fails the test.
                    || uri.AbsolutePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
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
        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=pending");
        var approvalGrid = Page.GetByTestId("pending-approval-list");
        await approvalGrid.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });

        var approvalDetail = Page.Locator(".vpp-approval-detail:visible");

        await SelectQueueOrderByReasonAsync(approvalGrid, ApprovalReason);
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "pending-approval-split-populated-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled
            });
        }
        await approvalDetail.Locator("button:has-text('Duyệt')").ClickAsync();
        var approveDialog = Page.Locator(".rz-dialog:visible").Last;
        await approveDialog.GetByText("Duyệt đơn", new() { Exact = true }).WaitForAsync();
        await approveDialog.GetByRole(AriaRole.Button, new() { Name = "Có", Exact = true }).ClickAsync();
        await Page.GetByText("Đã duyệt đơn thành công.", new() { Exact = false }).WaitForAsync();

        await SelectQueueOrderByReasonAsync(approvalGrid, RejectionReason);
        await approvalDetail.Locator("button:has-text('Từ chối')").ClickAsync();
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

    [Fact]
    public async Task Manager_ReviewsPendingSupplementInCanonicalListDetailWorkspace()
    {
        const string visualReason = "UI motif pending approval review";

        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Employee);
        await CreateSupplementAsync(visualReason, verifyRequiredReason: false);
        await SwitchUserAsync(TestAccounts.Manager);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=pending");

        var approvalGrid = Page.GetByTestId("pending-approval-list");
        await approvalGrid.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await approvalGrid.Locator("tbody tr[aria-selected='true']").First.WaitForAsync();

        var approvalDetail = Page.Locator(".vpp-approval-detail:visible");
        await approvalDetail.Locator(".vpp-approval-detail-code-row h2").WaitForAsync();
        (await approvalGrid.Locator(".vpp-collection-header-copy").InnerTextAsync())
            .Should().Contain("Đơn bổ sung chờ duyệt");
        (await approvalDetail.Locator("button:has-text('Xuất PDF')").CountAsync()).Should().Be(1);
        (await approvalDetail.Locator("button:has-text('Xuất Excel')").CountAsync()).Should().Be(1);
        (await approvalDetail.Locator(".vpp-status-badge").CountAsync()).Should().BeGreaterThanOrEqualTo(2);

        var listFooterGap = await Page.EvaluateAsync<double>("""
            () => {
                const surface = document.querySelector('[data-testid="pending-approval-list"]')?.getBoundingClientRect();
                const pager = document.querySelector('[data-testid="pending-approval-list"] :is(.rz-paginator, .rz-pager)')?.getBoundingClientRect();
                return surface && pager ? surface.bottom - pager.bottom : Number.NaN;
            }
            """);
        listFooterGap.Should().BeApproximately(0, 1, "pager của master list phải bám đáy data surface");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        var viewports = new[]
        {
            (Width: 1366, Height: 768, Name: "desktop"),
            (Width: 1920, Height: 1080, Name: "desktop-wide"),
            (Width: 768, Height: 1024, Name: "tablet"),
            (Width: 390, Height: 844, Name: "mobile")
        };

        foreach (var viewport in viewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.WaitForTimeoutAsync(250);
            var mobileBackdrop = Page.Locator(".vpp-sidebar-backdrop:visible");
            if (await mobileBackdrop.CountAsync() > 0)
            {
                await mobileBackdrop.ClickAsync();
                await mobileBackdrop.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            }
            var hasHorizontalOverflow = await Page.EvaluateAsync<bool>(
                "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
            hasHorizontalOverflow.Should().BeFalse($"{viewport.Name} không được tràn ngang document");

            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Directory.CreateDirectory(evidenceDirectory);
                await Page.ScreenshotAsync(new()
                {
                    Path = Path.Combine(evidenceDirectory, $"pending-approval-{viewport.Name}.png"),
                    FullPage = false,
                    Animations = ScreenshotAnimations.Disabled
                });
            }
        }
    }

    [Fact]
    public async Task Employee_CancelsSupplement_ReopensCreateActionWithoutReload()
    {
        const string cancellationProbeReason = "Kiểm tra tạo lại đơn bổ sung sau khi hủy";

        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsAsync(TestAccounts.Employee);
        await CreateSupplementAsync(cancellationProbeReason, verifyRequiredReason: false);

        var orderPage = Page.Locator(".order-page:visible");
        var supplementCard = orderPage.GetByTestId("supplement-order-panel").Last;
        await supplementCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Hủy đơn ")
        }).Last.ClickAsync();

        var cancelDialog = Page.Locator(".rz-dialog:visible").Last;
        await cancelDialog.GetByText("Xác nhận hủy đơn", new() { Exact = false }).WaitForAsync();
        await cancelDialog.GetByRole(AriaRole.Button, new() { Name = "Hủy đơn", Exact = true }).ClickAsync();
        await Page.GetByText("Đã hủy đơn.", new() { Exact = false }).WaitForAsync();

        await supplementCard.GetByText("Đã hủy", new() { Exact = false }).WaitForAsync();
        var createSupplementButton = orderPage.GetByTestId("create-supplement");
        await createSupplementButton.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        (await createSupplementButton.IsEnabledAsync()).Should().BeTrue(
            "hủy đơn bổ sung phải refresh policy kỳ và mở lại CTA nếu vẫn còn quota tạo đơn");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "supplement-cta-after-cancel-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled
            });
        }
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
                await orderPage.Locator(".vpp-orders-view-selector")
                    .GetByRole(AriaRole.Button, new() { NameRegex = new Regex("Đơn bổ sung") })
                    .ClickAsync();
                await orderPage.GetByTestId("create-supplement").WaitForAsync();
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

        await (await GetInteractiveButtonAsync(wizard, "Thêm mặt hàng vào đơn")).ClickAsync();

        // Atlas M2: lý do đơn bổ sung nhập qua popover "Ghi chú" ngay ở bước chọn mặt hàng;
        // nút "Tiếp tục" bị khóa tới khi lý do hợp lệ (>= 5 ký tự) thay cho toast lúc gửi duyệt.
        var continueButton = wizard.GetByRole(AriaRole.Button, new() { Name = "Tiếp tục", Exact = true });
        if (verifyRequiredReason)
        {
            (await continueButton.IsDisabledAsync()).Should().BeTrue(
                "a supplement draft must not reach the review step before its reason is provided");
        }

        await wizard.GetByRole(AriaRole.Button, new() { Name = "Ghi chú", Exact = true }).ClickAsync();
        var reasonPopover = wizard.Locator(".vpp-order-note-popover--order");
        await reasonPopover.Locator("textarea").FillAsync(reason);
        await reasonPopover.GetByRole(AriaRole.Button, new() { Name = "Lưu ghi chú", Exact = true }).ClickAsync();
        // Popover ghi chú là native HTML popover — không bao giờ detach, chỉ tắt trạng thái
        // :popover-open. Chờ nó đóng hẳn trước khi bấm "Tiếp tục": lưu ghi chú re-render
        // footer wizard và click giữa transition sẽ trượt hit-target.
        await Page.WaitForFunctionAsync("""
            () => {
                const popover = document.querySelector('.vpp-order-note-popover--order');
                return !popover || !popover.matches(':popover-open');
            }
            """);

        await (await GetInteractiveButtonAsync(wizard, "Tiếp tục")).ClickAsync();
        if (verifyRequiredReason)
        {
            // Chờ bước xem lại render xong ("Gửi duyệt" chỉ có ở bước 2) rồi mới bấm
            // "Quay lại" — tránh trúng nút "Quay lại" của bước chọn mặt hàng còn trên DOM.
            await wizard.GetByRole(AriaRole.Button, new() { Name = "Gửi duyệt", Exact = true }).WaitForAsync();
            // Giữ coverage điều hướng lui/tới giữa hai bước như flow cũ.
            await wizard.GetByRole(AriaRole.Button, new() { Name = "Quay lại", Exact = true }).ClickAsync();
            await (await GetInteractiveButtonAsync(wizard, "Tiếp tục")).ClickAsync();
        }
        await (await GetInteractiveButtonAsync(wizard, "Gửi duyệt")).ClickAsync();

        await WaitForUrlMatchAsync(
            new Regex(".*/dashboard\\?tab=0.*", RegexOptions.IgnoreCase),
            TimeSpan.FromSeconds(30));
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0&orderView=supplement");
        var supplementCard = Page.Locator("[data-testid='supplement-order-panel']:visible").Last;
        await supplementCard.GetByText("Bổ sung", new() { Exact = true }).WaitForAsync();
        await supplementCard.GetByText("Chờ duyệt", new() { Exact = true }).WaitForAsync();
    }

    private async Task SelectQueueOrderByReasonAsync(ILocator grid, string reason)
    {
        var rows = grid.Locator("tbody tr");
        var count = await rows.CountAsync();
        for (var index = 0; index < count; index++)
        {
            await rows.Nth(index).ClickAsync();
            var detail = Page.Locator(".vpp-approval-detail:visible");
            await detail.WaitForAsync();
            await Page.GetByTestId("pending-approval-detail-data-surface").WaitForAsync();
            await Page.WaitForFunctionAsync(
                "() => !document.querySelector('.vpp-approval-detail .vpp-skeleton-page')");
            var detailText = await detail.Locator(".vpp-approval-reason").InnerTextAsync();
            if (detailText.Contains(reason, StringComparison.Ordinal))
            {
                return;
            }
        }

        throw new InvalidOperationException($"Không tìm thấy đơn bổ sung có lý do '{reason}' trong hàng chờ.");
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

    private async Task AssertSupplementEndStateAsync(
        QaTestAccount account,
        string expectedStatus,
        string creationReason,
        string expectedDecisionAction)
    {
        await SwitchUserAsync(account);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0&orderView=supplement");
        await Page.Locator(".order-page").WaitForAsync();
        var supplementCard = Page.Locator("[data-testid='supplement-order-panel']:visible").Last;
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
