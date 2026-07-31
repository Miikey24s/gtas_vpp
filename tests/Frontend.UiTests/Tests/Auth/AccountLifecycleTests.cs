using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests.Auth;

public sealed class AccountLifecycleTests : TestBase, IMutatingUiTest
{
    [Fact]
    public async Task Registration_CreatesPendingAccountThatCannotSignInBeforeApproval()
    {
        const string username = "e2e.pending";
        const string password = "Pending-Pass1!";
        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync(
                $"{BaseUrl}Account/Register",
                new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Page.GetByRole(AriaRole.Button, new() { Name = "Tạo tài khoản" })
                .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var accessibility = await Page.EvaluateAsync<int[]>("""
                () => {
                    const visible = element => {
                        const style = getComputedStyle(element);
                        return style.display !== 'none' && style.visibility !== 'hidden';
                    };
                    const controls = [...document.querySelectorAll('input, select, textarea')]
                        .filter(visible)
                        .filter(control => control.type !== 'hidden');
                    const unlabeled = controls.filter(control => {
                        const id = control.id;
                        return !control.getAttribute('aria-label')
                            && !control.getAttribute('aria-labelledby')
                            && !(id && document.querySelector(`label[for="${CSS.escape(id)}"]`))
                            && !control.closest('label');
                    });
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        unlabeled.length
                    ];
                }
                """);
            accessibility.Should().Equal(0, 0);
        }

        await Page.GetByRole(AriaRole.Button, new() { Name = "Tạo tài khoản" })
            .WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        // Chờ circuit interactive render xong nút submit (không click) trước khi điền form, tránh race prerender.
        await GetInteractiveButtonAsync(Page.Locator("body"), "Tạo tài khoản");
        await Page.Locator("input[name='Username']").FillAsync(username);
        await Page.Locator("input[name='FullName']").FillAsync("E2E Pending User");
        await Page.Locator("input[name='Email']").FillAsync("e2e.pending@example.test");
        // Atlas account-register có đúng 5 field: không còn EmployeeCode (optional trong DTO, backend tự bỏ qua).
        await Page.Locator("input[name='Password']").FillAsync(password);
        await Page.Locator("input[name='ConfirmPassword']").FillAsync(password);

        await Page.GetByRole(AriaRole.Button, new() { Name = "Tạo tài khoản" }).ClickAsync();

        try
        {
            await Page.GetByText("đang chờ quản trị viên phê duyệt", new() { Exact = false })
                .WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 15000
                });
        }
        catch (TimeoutException exception)
        {
            var body = await Page.Locator("body").InnerTextAsync();
            throw new InvalidOperationException(
                $"Registration did not reach PendingApproval. Visible page text: {body}",
                exception);
        }

        var loginPage = new LoginPage(Page);
        await loginPage.GotoAsync(BaseUrl);
        await loginPage.LoginAsync(username, password);
        // Tài khoản pending bị từ chối đăng nhập: chờ thông báo lỗi hiện trong validation slot
        // ([role=alert], cùng pattern LoginFeedbackTests) rồi mới assert URL không đổi.
        await Page.Locator(".vpp-login-password-wrapper + .vpp-validation-slot [role=alert]")
            .WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 15_000
            });

        Page.Url.Should().Contain("/Account/Login");

        await loginPage.LoginAsync(TestUsername, TestPassword);
        await loginPage.WaitForDashboardAsync();
        await Page.GotoAsync(
            $"{BaseUrl}permission?tab=0",
            new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        var search = Page.GetByPlaceholder("Tìm tài khoản, họ tên hoặc email");
        await search.WaitForAsync();
        await search.FillAsync(username);
        var pendingRow = Page.Locator(".permission-user-grid tbody tr")
            .Filter(new LocatorFilterOptions { HasText = username });
        await pendingRow.WaitForAsync();
        await Page.WaitForFunctionAsync(
            "() => !document.querySelector('.permission-user-grid')?.classList.contains('rz-datatable-loading')");

        var approveButton = pendingRow.GetByRole(AriaRole.Button, new()
        {
            Name = "Chọn nhóm quyền và phòng ban, sau đó nhấn Duyệt.",
            Exact = true
        });
        (await approveButton.IsDisabledAsync()).Should().BeTrue(
            "a pending account must receive both assignments before approval");
        (await pendingRow.Locator(".vpp-admin-user-access-switch").CountAsync()).Should().Be(0,
            "the access switch is not an actionable control before account approval");
        await CaptureIfRequestedAsync("user-pending-approval.png");

        var assignmentSelects = pendingRow.Locator(".vpp-admin-inline-select");
        await assignmentSelects.Nth(0).ClickAsync();
        var groupPopup = Page.Locator(".rz-dropdown-panel:visible").Last;
        await groupPopup.GetByText("Nhân viên", new() { Exact = true }).ClickAsync();

        await assignmentSelects.Nth(1).ClickAsync();
        var departmentPopup = Page.Locator(".rz-dropdown-panel:visible").Last;
        await departmentPopup.GetByText("QA Department Alpha", new() { Exact = true }).ClickAsync();

        approveButton = pendingRow.GetByRole(AriaRole.Button, new()
        {
            Name = "Duyệt tài khoản",
            Exact = true
        });
        await approveButton.WaitForAsync();
        (await approveButton.IsEnabledAsync()).Should().BeTrue();
        await CaptureIfRequestedAsync("user-ready-to-approve.png");
        await approveButton.ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Kích hoạt", Exact = true }).ClickAsync();

        await pendingRow.GetByText("Hoạt động", new() { Exact = true })
            .WaitForAsync(new LocatorWaitForOptions { Timeout = 30_000 });
        (await pendingRow.Locator(".vpp-admin-user-access-switch").CountAsync()).Should().Be(1,
            "the two-way access switch becomes available only after activation");
        await CaptureIfRequestedAsync("user-approved.png");
    }

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory)) return;

        Directory.CreateDirectory(directory);
        await WaitForRenderSettleAsync();
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
