using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class UiSystemF0RegressionTests : TestBase, IAuthenticatedUiTest
{
    private static readonly ViewportSize[] RequiredViewports =
    [
        new() { Width = 390, Height = 844 },
        new() { Width = 768, Height = 1024 },
        new() { Width = 1366, Height = 768 },
        new() { Width = 1920, Height = 1080 }
    ];

    private static readonly RouteArchetype Login =
        new("login", "Account/Login", ".vpp-login-card");

    private static readonly RouteArchetype[] AuthenticatedArchetypes =
    [
        new("history", "dashboard?tab=1", ".vpp-history-page", ".vpp-history-loading-state"),
        new("library-departments", "library?tab=5", ".vpp-admin-data-surface"),
        new("permission", "permission?tab=1", "[data-testid='permission-groups-data-surface']")
    ];

    [Fact]
    public async Task RepresentativeArchetypes_PreserveRuntimeGeometryAndBrowserHealth()
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        var httpFailures = new List<string>();
        AttachBrowserDiagnostics(browserErrors, requestFailures, httpFailures);

        foreach (var viewport in RequiredViewports)
        {
            await AuditRouteAsync(Login, viewport, evidenceDirectory);
        }
        await AssertRuntimeStylesheetOrderAsync();

        await LoginAsDefaultUserAsync();
        foreach (var viewport in RequiredViewports)
        {
            foreach (var route in AuthenticatedArchetypes)
            {
                await AuditRouteAsync(route, viewport, evidenceDirectory);
            }
        }

        browserErrors.Should().BeEmpty("F0 representative routes must not emit browser errors");
        requestFailures.Should().BeEmpty("F0 representative routes must not issue failed network requests");
        httpFailures.Should().BeEmpty("F0 representative routes must not return failed documents, stylesheets or scripts");
    }

    private async Task AuditRouteAsync(
        RouteArchetype route,
        ViewportSize viewport,
        string? evidenceDirectory)
    {
        await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
        await Page.GotoAsync($"{BaseUrl}{route.Path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator(route.ReadySelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        if (route.Id != Login.Id)
        {
            await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Attached,
                Timeout = 60_000
            });
        }
        if (!string.IsNullOrWhiteSpace(route.LoadingSelector))
        {
            await Page.Locator(route.LoadingSelector).WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Detached,
                Timeout = 60_000
            });
        }
        await Page.WaitForFunctionAsync(
            "() => !document.documentElement.classList.contains('vpp-page-entering')");
        await Page.EvaluateAsync("() => document.fonts.ready");
        await WaitForRenderSettleAsync();

        var geometry = await Page.Locator(route.ReadySelector).First.EvaluateAsync<double[]>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                return [
                    rect.width,
                    rect.height,
                    document.documentElement.scrollWidth - window.innerWidth
                ];
            }
            """);
        geometry[0].Should().BeGreaterThan(0, $"{route.Id} needs measurable width at {viewport.Width}px");
        geometry[1].Should().BeGreaterThan(0, $"{route.Id} needs measurable height at {viewport.Width}px");
        geometry[2].Should().BeLessThanOrEqualTo(1, $"{route.Id} must not overflow horizontally at {viewport.Width}px");

        if (route.Id != Login.Id && viewport.Width <= 768)
        {
            var shellGeometry = await Page.EvaluateAsync<double[]>(
                """
                () => {
                    const body = document.querySelector('.vpp-layout-body')?.getBoundingClientRect();
                    const toggle = document.querySelector('.vpp-mobile-sidebar-toggle');
                    const toggleStyle = toggle ? getComputedStyle(toggle) : null;
                    return [
                        body?.left ?? -1,
                        body ? window.innerWidth - body.right : -1,
                        toggleStyle && toggleStyle.display !== 'none' && toggleStyle.visibility !== 'hidden' ? 1 : 0
                    ];
                }
                """);
            shellGeometry[0].Should().BeApproximately(0, 1, $"{route.Id} must not reserve a hidden sidebar gutter at {viewport.Width}px");
            shellGeometry[1].Should().BeApproximately(0, 1, $"{route.Id} body must reach the viewport edge at {viewport.Width}px");
            shellGeometry[2].Should().Be(1, $"{route.Id} needs a visible navigation toggle at {viewport.Width}px");
        }

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(
                    evidenceDirectory,
                    $"f0-{route.Id}-{viewport.Width}x{viewport.Height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    private void AttachBrowserDiagnostics(
        List<string> browserErrors,
        List<string> requestFailures,
        List<string> httpFailures)
    {
        Page.Console += (_, message) =>
        {
            var expectedNavigationAbort = message.Text.Contains(
                    "Failed to complete negotiation with the server",
                    StringComparison.OrdinalIgnoreCase)
                && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
            if (message.Type.Equals("error", StringComparison.OrdinalIgnoreCase)
                && !expectedNavigationAbort)
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);
        Page.RequestFailed += (_, request) =>
        {
            var expectedAbort = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true
                && (uri.AbsolutePath.Equals("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.Equals("/_blazor/negotiate", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase));
            if (!expectedAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };
        Page.Response += (_, response) =>
        {
            if (response.Status < 400)
            {
                return;
            }

            var resourceType = response.Request.ResourceType;
            if (resourceType is "document" or "stylesheet" or "script")
            {
                httpFailures.Add($"{response.Status} {resourceType} {response.Url}");
            }
        };
    }

    private async Task AssertRuntimeStylesheetOrderAsync()
    {
        var hrefs = await Page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll('link[rel="stylesheet"]')]
                .map(link => link.href)
            """);
        var radzenBaseIndex = Array.FindIndex(
            hrefs,
            href => href.Contains("_content/Radzen.Blazor/css/material-base.css", StringComparison.Ordinal));
        var tokenIndex = Array.FindIndex(hrefs, href => href.Contains("vpp-tokens", StringComparison.Ordinal));
        var bridgeIndex = Array.FindIndex(hrefs, href => href.Contains("vpp-radzen-theme", StringComparison.Ordinal));

        radzenBaseIndex.Should().BeGreaterThanOrEqualTo(0, "the explicit Radzen base stylesheet must load");
        tokenIndex.Should().BeGreaterThan(radzenBaseIndex, "VPP tokens must override the Radzen base");
        bridgeIndex.Should().BeGreaterThan(tokenIndex, "the Radzen bridge must load after VPP tokens");
    }

    private sealed record RouteArchetype(
        string Id,
        string Path,
        string ReadySelector,
        string? LoadingSelector = null);
}
