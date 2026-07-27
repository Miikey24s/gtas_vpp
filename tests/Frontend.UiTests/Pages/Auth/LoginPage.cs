using Microsoft.Playwright;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gtas_vpp_fe.UITests.Pages.Auth
{
    public class LoginPage
    {
        // Blazor stamps "_bl_" ElementReference attributes on Radzen component roots
        // only after the live interactive circuit has rendered the subtree. Filling the
        // form earlier races the first interactive re-render, which two fixed 750ms
        // waits used to paper over (removed by REFACTOR-001-R0 S4). The probe re-queries
        // the DOM on every poll so it survives node replacement during hydration.
        private const string InteractiveLoginFormProbe =
            """
            () => {
                const button = document.querySelector('.vpp-login-btn, button[type=submit]');
                if (!button) {
                    return false;
                }
                for (let node = button; node; node = node.parentElement) {
                    if (Array.from(node.attributes).some(attribute => attribute.name.startsWith('_bl_'))) {
                        return true;
                    }
                }
                return false;
            }
            """;

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
            await WaitForInteractiveFormAsync();
        }

        public async Task WaitForDashboardAsync()
        {
            var timeoutAt = DateTime.UtcNow.AddSeconds(60);

            while (DateTime.UtcNow < timeoutAt)
            {
                if (Regex.IsMatch(_page.Url, @".*/dashboard(?:[/?#].*)?$", RegexOptions.IgnoreCase))
                {
                    await _page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
                    {
                        Timeout = 60_000,
                        State = WaitForSelectorState.Visible
                    });
                    return;
                }

                await Task.Delay(250);
            }

            throw new TimeoutException($"Timed out waiting for post-login redirect. Last URL: {_page.Url}");
        }

        public async Task LoginAsync(string username, string password)
        {
            // Some callers navigate on their own instead of using GotoAsync, so the
            // interactivity gate is re-checked here; it returns near-instantly when
            // GotoAsync already confirmed the live circuit.
            await WaitForInteractiveFormAsync();
            await _usernameInput.FillAsync(username);
            await _passwordInput.FillAsync(password);

            await _loginButton.ClickAsync();
        }

        private async Task WaitForInteractiveFormAsync()
        {
            await _loginButton.WaitForAsync(new LocatorWaitForOptions
            {
                Timeout = 120000,
                State = WaitForSelectorState.Visible
            });
            await _page.WaitForFunctionAsync(InteractiveLoginFormProbe);
        }
    }
}
