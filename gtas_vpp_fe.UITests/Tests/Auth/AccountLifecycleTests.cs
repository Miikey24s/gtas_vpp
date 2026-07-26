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
        await Page.WaitForTimeoutAsync(1000);
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
        await Page.WaitForTimeoutAsync(1500);

        Page.Url.Should().Contain("/Account/Login");
    }
}
