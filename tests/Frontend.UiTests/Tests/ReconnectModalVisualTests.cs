using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ReconnectModalVisualTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task ReconnectModal_PresentsEachCircuitStateWithoutViewportOverflow()
    {
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2Fdashboard%3Ftab%3D0");
        await Page.Locator("#components-reconnect-modal").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });

        var states = new[]
        {
            new ReconnectState("components-reconnect-show", "Đang kết nối lại", false),
            new ReconnectState("components-reconnect-retrying", "Vẫn đang kết nối", false),
            new ReconnectState("components-reconnect-failed", "Không thể kết nối", true),
            new ReconnectState("components-reconnect-paused", "Phiên làm việc đã tạm dừng", true),
            new ReconnectState("components-reconnect-resume-failed", "Không thể khôi phục phiên", true)
        };

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 1366, Height = 768 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);

            foreach (var state in states)
            {
                await PresentStateAsync(state.CssClass);

                var dialog = Page.Locator("#components-reconnect-modal");
                await dialog.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
                (await dialog.Locator("h2:visible").InnerTextAsync()).Should().Be(state.Title);

                var audit = await dialog.EvaluateAsync<double[]>("""
                    modal => {
                        const rect = modal.getBoundingClientRect();
                        return [
                            document.documentElement.scrollWidth - window.innerWidth,
                            rect.left,
                            rect.right - window.innerWidth,
                            Math.abs((rect.left + rect.width / 2) - window.innerWidth / 2),
                            [...modal.querySelectorAll('button')].filter(button => getComputedStyle(button.parentElement).display !== 'none').length
                        ];
                    }
                    """);

                audit[0].Should().BeLessThanOrEqualTo(1, $"the page must not overflow at {viewport.Width}px");
                audit[1].Should().BeGreaterThanOrEqualTo(0, "the reconnect dialog must stay inside the viewport");
                audit[2].Should().BeLessThanOrEqualTo(0, "the reconnect dialog must stay inside the viewport");
                audit[3].Should().BeLessThanOrEqualTo(8, "the reconnect dialog should remain centered within the browser layout viewport");
                audit[4].Should().Be(state.HasAction ? 1 : 0, "only actionable states should display a button");
            }
        }

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.SetViewportSizeAsync(1366, 768);
            await PresentStateAsync("components-reconnect-failed");
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "reconnect-failed-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    private async Task PresentStateAsync(string cssClass)
    {
        await Page.EvaluateAsync("""
            cssClass => {
                const modal = document.querySelector('#components-reconnect-modal');
                const stateClasses = [
                    'components-reconnect-show',
                    'components-reconnect-retrying',
                    'components-reconnect-failed',
                    'components-reconnect-paused',
                    'components-reconnect-resume-failed'
                ];
                modal.classList.remove(...stateClasses);
                modal.classList.add(cssClass);
                const countdown = modal.querySelector('#components-seconds-to-next-attempt');
                if (countdown) countdown.textContent = '4';
                if (!modal.open) modal.showModal();
            }
            """, cssClass);
    }

    private sealed record ReconnectState(string CssClass, string Title, bool HasAction);
}
