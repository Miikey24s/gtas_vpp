using Deque.AxeCore.Playwright;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AtlasFullRuntimeTests : TestBase, IAuthenticatedUiTest
{
    private string _diagnosticContext = "startup";
    private static readonly ViewportSize[] RequiredViewports =
    [
        new() { Width = 390, Height = 844 },
        new() { Width = 768, Height = 1024 },
        new() { Width = 1366, Height = 768 },
        new() { Width = 1920, Height = 1080 }
    ];

    private static readonly AtlasRuntimeScreen[] AnonymousScreens =
    [
        new("login", "Account/Login", ".vpp-login-card"),
        new("forgot-password", "Account/ForgotPassword", "#forgot-password-title"),
        new("reset-password", "Account/ResetPassword", "#reset-password-title"),
        new("register", "Account/Register", "#register-title")
    ];

    private static readonly AtlasRuntimeScreen[] AuthenticatedScreens =
    [
        new("shell-system", "dashboard?tab=0", ".vpp-sidebar[data-shell-ready='true']"),
        new("change-password", "Account/ChangePassword", "#change-password-title"),
        new("my-orders", "dashboard?tab=0", ".vpp-orders-workspace"),
        new("order-create", "dashboard/order-create", ".vpp-order-create-page"),
        new("history", "dashboard?tab=1", ".vpp-history-page"),
        new("catalog", "dashboard?tab=2", ".vpp-catalog-workspace"),
        new("department-summary", "dashboard?tab=3&managementTab=department", ".vpp-history-page"),
        new("supplement-approval", "dashboard?tab=5&periodTab=pending", ".vpp-approval-operation-workspace"),
        new("period-review", "dashboard?tab=5&periodTab=review", "[data-testid='period-settlement-data-surface']"),
        new("period-demand", "dashboard?tab=5&periodTab=demand", "[data-testid='period-settlement-data-surface']"),
        new("supply-allocation", "dashboard?tab=5&periodTab=supply", "[data-testid='period-settlement-data-surface']"),
        new("settlement-flow", "dashboard?tab=5&periodTab=settle", "[data-testid='period-settlement-data-surface']"),
        new("classes", "library?tab=0", ".vpp-admin-class-split"),
        new("categories", "library?tab=1", ".vpp-admin-data-surface"),
        new("items", "library?tab=2", ".vpp-admin-data-surface"),
        new("departments", "library?tab=5", ".vpp-admin-data-surface"),
        new("suppliers", "library?tab=3", ".vpp-admin-data-surface"),
        new("price-lists", "library?tab=6&pricingTab=price-lists", ".vpp-price-list-workspace"),
        new("prices", "library?tab=6&pricingTab=prices", ".vpp-price-workspace"),
        new("users", "permission?tab=0", "[data-testid='permission-users-data-surface']"),
        new("permissions", "permission?tab=1", "[data-testid='permission-groups-data-surface']"),
        new("reports", "report", ".vpp-report-page"),
        new("system-states", "dashboard?tab=0", "#components-reconnect-modal")
    ];

    [Fact]
    public async Task All28AtlasScreens_RenderWithoutPageOverflowOrBrowserFailures()
    {
        (AnonymousScreens.Length + AuthenticatedScreens.Length + 1).Should().Be(28);

        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        AttachBrowserDiagnostics(browserErrors, requestFailures);

        foreach (var viewport in RequiredViewports)
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            foreach (var screen in AnonymousScreens)
            {
                await AuditScreenAsync(screen, viewport);
            }

            await LoginAsDefaultUserAsync();
            foreach (var screen in AuthenticatedScreens)
            {
                await AuditScreenAsync(screen, viewport);
                (await Page.Locator(".vpp-state-panel-denied:visible").CountAsync()).Should().Be(
                    0,
                    $"{screen.Id} must be accessible to the canonical SystemAdmin fixture");
            }

            await Page.GotoAsync($"{BaseUrl}logoutprocess", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await Page.WaitForURLAsync("**/Account/Login**", new PageWaitForURLOptions
            {
                Timeout = 30_000,
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await Page.Locator(".vpp-login-card").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
            Page.Url.Should().Contain("/Account/Login", "the Atlas logout screen must complete its redirect");
        }

        browserErrors.Should().BeEmpty("the 28-screen runtime matrix must not emit browser errors");
        requestFailures.Should().BeEmpty("the 28-screen runtime matrix must not issue failed network requests");
    }

    [Fact]
    public async Task RepresentativeRoutes_PassDarkPrintAndAxeHardening()
    {
        await LoginAsDefaultUserAsync();
        await Page.SetViewportSizeAsync(1366, 768);

        var userMenu = Page.Locator(".user-menu-trigger").First;
        await userMenu.ClickAsync();
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.documentElement.classList.contains('rz-theme-dark')");

        foreach (var screen in new[]
                 {
                     new AtlasRuntimeScreen("my-orders", "dashboard?tab=0", ".vpp-orders-workspace"),
                     new AtlasRuntimeScreen("supply-allocation", "dashboard?tab=5&periodTab=supply", "[data-testid='period-settlement-data-surface']"),
                     new AtlasRuntimeScreen("items", "library?tab=2", ".vpp-admin-data-surface"),
                     new AtlasRuntimeScreen("security-audit", "permission?tab=2", "[data-testid='security-audit-data-surface']"),
                     new AtlasRuntimeScreen("reports", "report", ".vpp-report-page")
                 })
        {
            await AuditScreenAsync(screen, new ViewportSize { Width = 1366, Height = 768 });
            (await Page.EvaluateAsync<bool>("() => document.documentElement.classList.contains('rz-theme-dark')"))
                .Should().BeTrue($"dark theme must persist on {screen.Id}");

            var axeResult = await Page.RunAxe();
            var blocking = axeResult.Violations
                .Where(violation => violation.Impact is "critical" or "serious")
                .SelectMany(violation => violation.Nodes.Select(node =>
                    $"{screen.Id}: {violation.Id} ({violation.Impact}) node={System.Text.Json.JsonSerializer.Serialize(node)}"))
                .ToArray();
            blocking.Should().BeEmpty($"{screen.Id} must have no critical/serious axe violations");
        }

        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { Media = Media.Print });
        await AuditScreenAsync(new AtlasRuntimeScreen("reports-print", "report", ".vpp-report-page"), new ViewportSize { Width = 1366, Height = 768 });
        var printAudit = await Page.EvaluateAsync<int[]>("""
            () => {
                const hidden = selector => {
                    const element = document.querySelector(selector);
                    return !element || getComputedStyle(element).display === 'none';
                };
                return [
                    hidden('.vpp-sidebar') ? 0 : 1,
                    hidden('.vpp-header-glass') ? 0 : 1,
                    getComputedStyle(document.body).backgroundColor === 'rgb(255, 255, 255)' ? 0 : 1,
                    document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0
                ];
            }
            """);
        printAudit.Should().Equal(0, 0, 0, 0);
    }

    private async Task AuditScreenAsync(AtlasRuntimeScreen screen, ViewportSize viewport)
    {
        _diagnosticContext = $"{screen.Id}@{viewport.Width}x{viewport.Height}";
        await Page.GotoAsync($"{BaseUrl}{screen.Route}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        var attachedOnly = screen.Id is "shell-system" or "system-states";
        await Page.Locator(screen.ReadySelector).First.WaitForAsync(new LocatorWaitForOptions
        {
            State = attachedOnly ? WaitForSelectorState.Attached : WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await Page.WaitForFunctionAsync(
            "() => !document.documentElement.classList.contains('vpp-page-entering')");
        await WaitForRenderSettleAsync();

        var hasPageOverflow = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1");
        hasPageOverflow.Should().BeFalse($"{screen.Id} must fit {viewport.Width}x{viewport.Height}");
    }

    private void AttachBrowserDiagnostics(List<string> browserErrors, List<string> requestFailures)
    {
        Page.Console += (_, message) =>
        {
            var expectedNavigationAbort = message.Text.Contains("Failed to complete negotiation with the server", StringComparison.OrdinalIgnoreCase)
                && message.Text.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase);
            if (message.Type.Equals("error", StringComparison.OrdinalIgnoreCase) && !expectedNavigationAbort)
            {
                browserErrors.Add($"{_diagnosticContext}: {message.Text}");
            }
        };
        Page.PageError += (_, error) => browserErrors.Add($"{_diagnosticContext}: {error}");
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
                requestFailures.Add($"{_diagnosticContext}: {request.Method} {request.Url}: {request.Failure}");
            }
        };
    }

    private sealed record AtlasRuntimeScreen(string Id, string Route, string ReadySelector);
}
