using Microsoft.Playwright;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Auth
{
    public class LoginPage
    {
        private readonly IPage _page;
        private readonly ILocator _usernameInput;
        private readonly ILocator _passwordInput;
        private readonly ILocator _loginButton;

        public LoginPage(IPage page)
        {
            _page = page;
            _usernameInput = _page.GetByRole(AriaRole.Textbox).First;
            _passwordInput = _page.GetByRole(AriaRole.Textbox).Nth(1);
            _loginButton = _page.GetByRole(AriaRole.Button, new() { Name = "LOGIN" });
        }

        public async Task GotoAsync(string baseUrl)
        {
            await _page.GotoAsync($"{baseUrl}Account/Login");
            await _loginButton.WaitForAsync();
        }

        public async Task LoginWithDefaultCredentialsAsync()
        {
            await LoginAsync("google", "abc*123@");
        }

        public async Task WaitForDashboardAsync()
        {
            await _page.WaitForURLAsync(new Regex(".*dashboard.*"), new PageWaitForURLOptions { Timeout = 15000 });
            await _page.GetByRole(AriaRole.Tab, new() { Name = "My Orders" }).WaitForAsync();
        }

        public async Task LoginAsync(string username, string password, string serverName = "Test")
        {
            await _usernameInput.FillAsync(username);
            await _passwordInput.FillAsync(password);
            
            // Xử lý chọn Server nếu DropDown hiển thị
            var serverDropdown = _page.Locator(".rz-dropdown").First;
            if (await serverDropdown.CountAsync() > 0 && await serverDropdown.IsVisibleAsync())
            {
                await serverDropdown.ClickAsync();
                // Click option from popup
                var option = _page.Locator(".rz-dropdown-item").GetByText(serverName, new LocatorGetByTextOptions { Exact = true });
                await option.WaitForAsync();
                await option.ClickAsync();
            }

            await _loginButton.ClickAsync();
        }
    }
}
