using Microsoft.Playwright;
using System;
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
            _usernameInput = _page.Locator("input[name='Username']").First;
            _passwordInput = _page.Locator("input[name='Password'], input[name='PasswordText']").First;
            _loginButton = _page.Locator(".vpp-login-btn, button[type='submit']").First;
        }

        public async Task GotoAsync(string baseUrl)
        {
            await _page.GotoAsync($"{baseUrl}Account/Login", new PageGotoOptions
            {
                Timeout = 120000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await _loginButton.WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = 120000,
                State = WaitForSelectorState.Visible
            });
            await _page.WaitForTimeoutAsync(750);
        }

        public async Task WaitForDashboardAsync()
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < timeoutAt)
            {
                if (!Regex.IsMatch(_page.Url, @".*/Account/Login.*", RegexOptions.IgnoreCase))
                {
                    await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    return;
                }

                await Task.Delay(250);
            }

            throw new TimeoutException($"Timed out waiting for post-login redirect. Last URL: {_page.Url}");
        }

        public async Task LoginAsync(string username, string password)
        {
            await _page.WaitForTimeoutAsync(750);
            await _usernameInput.FillAsync(username);
            await _passwordInput.FillAsync(password);

            await _loginButton.ClickAsync();
        }
    }
}
