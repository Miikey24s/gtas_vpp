using Deque.AxeCore.Playwright;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AccessibilitySmokeTests : TestBase
{
    [Fact]
    public async Task Login_Is_Responsive_And_Keyboard_Accessible()
    {
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);
        Page.RequestFailed += (_, request) =>
        {
            var isExpectedCircuitDisconnect = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && uri.AbsolutePath.Equals("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase)
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            var isExpectedViewportAssetAbort = uri is not null
                && uri.AbsolutePath.EndsWith("/images/login-bg-optimized.jpeg", StringComparison.OrdinalIgnoreCase)
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect && !isExpectedViewportAssetAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

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
                    const serverControls = document.querySelectorAll(
                        '[name="Server"], [data-testid="server-selector"]');
                    const environmentOptions = [...document.querySelectorAll(
                        'label, [role="option"], .rz-dropdown-label')]
                        .filter(visible)
                        .filter(element => /^(test|live)$/i.test(element.textContent.trim()));
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        unlabeled.length,
                        document.querySelectorAll('.vpp-password-toggle-btn[aria-label]').length,
                        serverControls.length,
                        environmentOptions.length
                    ];
                }
                """);

            audit[0].Should().Be(0, $"the login page must not overflow at {viewport.Width}px");
            audit[1].Should().Be(0, "every visible login control must have an accessible label");
            audit[2].Should().Be(1, "the password visibility action needs an accessible name");
            audit[3].Should().Be(0, "deployment environment must not be a login control");
            audit[4].Should().Be(0, "Test/Live environment options must not be rendered");

            var axeResult = await Page.RunAxe();
            var blockingViolations = axeResult.Violations
                .Where(violation =>
                    string.Equals(violation.Impact, "critical", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(violation.Impact, "serious", StringComparison.OrdinalIgnoreCase))
                .SelectMany(violation => violation.Nodes.Select(node =>
                    $"{violation.Id} ({violation.Impact}): {violation.Help}; target={string.Join(" ", node.Target)}; html={node.Html} — {violation.HelpUrl}"))
                .ToArray();
            blockingViolations.Should().BeEmpty(
                $"the login page must have no critical/serious axe violations at {viewport.Width}px");

            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
            {
                Directory.CreateDirectory(evidenceDirectory);
                await Page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(
                        evidenceDirectory,
                        $"env001-login-{viewport.Width}x{viewport.Height}.png"),
                    FullPage = true
                });
            }
        }

        browserErrors.Should().BeEmpty("the login page should not emit browser errors");
        requestFailures.Should().BeEmpty("the login page should not issue failed network requests");
    }
}
