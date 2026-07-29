using Deque.AxeCore.Playwright;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class UiSystemF1ThemeTests : TestBase, IAuthenticatedUiTest
{
    private static readonly RouteArchetype[] RepresentativeRoutes =
    [
        new("my-orders", "dashboard?tab=0", ".vpp-orders-story"),
        new("history", "dashboard?tab=1", ".vpp-history-page", ".vpp-history-loading-state"),
        new("library-departments", "library?tab=5", ".vpp-admin-data-surface", ".vpp-global-loader"),
        new("permission", "permission?tab=1", ".vpp-permission-matrix", ".vpp-global-loader")
    ];

    [Fact]
    public async Task SemanticTokensAndRadzenBridge_RemainHealthyInLightAndDarkModes()
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        var httpFailures = new List<string>();
        AttachBrowserDiagnostics(browserErrors, requestFailures, httpFailures);

        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);

        string[]? lightContract = null;
        string[]? darkContract = null;
        foreach (var darkMode in new[] { false, true })
        {
            await SetDarkModeAsync(darkMode);
            foreach (var route in RepresentativeRoutes)
            {
                await AuditRouteAsync(route, darkMode, evidenceDirectory);
            }

            var contract = await ReadResolvedThemeContractAsync();
            AssertBridgePairs(contract, darkMode);
            if (darkMode)
            {
                darkContract = contract;
            }
            else
            {
                lightContract = contract;
            }

            // Permission giữ một DataGrid legacy chưa opt-in normalization; F1 không
            // mở rộng sang retrofit accessibility của route đó. Dùng My Orders là
            // representative đã được W-H khóa cho gate semantic theme toàn cục.
            await AuditRouteAsync(RepresentativeRoutes[0], darkMode, evidenceDirectory: null);
            var axeResult = await Page.RunAxe();
            var blockingViolations = axeResult.Violations
                .Where(violation => violation.Impact is "critical" or "serious")
                .SelectMany(violation => violation.Nodes.Select(node =>
                    $"{violation.Id} ({violation.Impact}) node={System.Text.Json.JsonSerializer.Serialize(node)}"))
                .ToArray();
            blockingViolations.Should().BeEmpty($"the F1 {ModeName(darkMode)} representative must pass axe");
        }

        lightContract.Should().NotBeNull();
        darkContract.Should().NotBeNull();
        lightContract![0].Should().NotBe(darkContract![0], "the primary action role is theme-aware");
        lightContract[2].Should().NotBe(darkContract[2], "the canvas role is theme-aware");

        browserErrors.Should().BeEmpty("F1 representative routes must not emit browser errors");
        requestFailures.Should().BeEmpty("F1 representative routes must not issue failed network requests");
        httpFailures.Should().BeEmpty("F1 representative routes must not return failed documents, stylesheets or scripts");
    }

    private async Task AuditRouteAsync(
        RouteArchetype route,
        bool darkMode,
        string? evidenceDirectory)
    {
        await Page.GotoAsync($"{BaseUrl}{route.Path}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator(route.ReadySelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 60_000
        });
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
        geometry[0].Should().BeGreaterThan(0, $"{route.Id} needs measurable width");
        geometry[1].Should().BeGreaterThan(0, $"{route.Id} needs measurable height");
        geometry[2].Should().BeLessThanOrEqualTo(1, $"{route.Id} must not overflow horizontally");
        (await Page.EvaluateAsync<bool>(
                "() => document.documentElement.classList.contains('rz-theme-dark')"))
            .Should().Be(darkMode, $"{route.Id} must preserve {ModeName(darkMode)} mode");

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(
                    evidenceDirectory,
                    $"f1-{ModeName(darkMode)}-{route.Id}-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    private async Task SetDarkModeAsync(bool darkMode)
    {
        var currentMode = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('rz-theme-dark')");
        if (currentMode == darkMode)
        {
            return;
        }

        await Page.Locator(".user-menu-trigger").First.ClickAsync();
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            darkMode
                ? "() => document.documentElement.classList.contains('rz-theme-dark')"
                : "() => !document.documentElement.classList.contains('rz-theme-dark')");
        await WaitForRenderSettleAsync();
    }

    private async Task<string[]> ReadResolvedThemeContractAsync()
        => await Page.EvaluateAsync<string[]>(
            """
            () => {
                const resolve = (token, property) => {
                    const probe = document.createElement('span');
                    probe.style.position = 'fixed';
                    probe.style.inset = '-9999px auto auto -9999px';
                    probe.style[property] = `var(${token})`;
                    document.body.appendChild(probe);
                    const value = getComputedStyle(probe)[property];
                    probe.remove();
                    return value;
                };
                return [
                    resolve('--vpp-action-primary', 'color'),
                    resolve('--rz-primary', 'color'),
                    resolve('--vpp-surface-canvas', 'backgroundColor'),
                    resolve('--rz-base-background-color', 'backgroundColor'),
                    resolve('--vpp-surface-raised', 'backgroundColor'),
                    resolve('--rz-card-background-color', 'backgroundColor'),
                    resolve('--vpp-surface-grid-header', 'backgroundColor'),
                    resolve('--rz-grid-header-background-color', 'backgroundColor'),
                    getComputedStyle(document.body).backgroundColor
                ];
            }
            """);

    private static void AssertBridgePairs(string[] contract, bool darkMode)
    {
        contract.Should().HaveCount(9);
        contract[1].Should().Be(contract[0], $"Radzen primary must resolve from the VPP {ModeName(darkMode)} action role");
        contract[3].Should().Be(contract[2], $"Radzen base must resolve from the VPP {ModeName(darkMode)} canvas role");
        contract[5].Should().Be(contract[4], $"Radzen card must resolve from the VPP {ModeName(darkMode)} raised role");
        contract[7].Should().Be(contract[6], $"Radzen grid header must resolve from the VPP {ModeName(darkMode)} grid-header role");
        contract[8].Should().Be(contract[2], $"the document body must use the VPP {ModeName(darkMode)} canvas role");
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
            if (response.Status >= 400
                && response.Request.ResourceType is "document" or "stylesheet" or "script")
            {
                httpFailures.Add($"{response.Status} {response.Request.ResourceType} {response.Url}");
            }
        };
    }

    private static string ModeName(bool darkMode) => darkMode ? "dark" : "light";

    private sealed record RouteArchetype(
        string Id,
        string Path,
        string ReadySelector,
        string? LoadingSelector = null);
}
