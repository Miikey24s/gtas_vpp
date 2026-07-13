using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class AccessibilitySmokeTests : TestBase
{
    [Fact]
    public async Task Login_Is_Responsive_And_Keyboard_Accessible()
    {
        var browserErrors = new List<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}Account/Login", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await Page.Locator(".vpp-login-card").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible
            });

            var audit = await Page.EvaluateAsync<int[]>("""
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
                        unlabeled.length,
                        document.querySelectorAll('.vpp-password-toggle-btn[aria-label]').length
                    ];
                }
                """);

            audit[0].Should().Be(0, $"the login page must not overflow at {viewport.Width}px");
            audit[1].Should().Be(0, "every visible login control must have an accessible label");
            audit[2].Should().Be(1, "the password visibility action needs an accessible name");
        }

        browserErrors.Should().BeEmpty("the login page should not emit browser errors");
    }
}
