using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ShellResponsiveTests : TestBase, IAuthenticatedUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_THESIS_SCREENSHOT_DIR";

    [Fact]
    public async Task AuthenticatedShell_IsVietnameseBrandedAccessibleAndResponsive()
    {
        var screenshotDirectory = ResolveScreenshotDirectory();
        if (screenshotDirectory is not null)
        {
            await Page.SetViewportSizeAsync(1920, 1080);
            await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin");
            await Page.GotoAsync($"{BaseUrl}Account/Login");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Đăng nhập", Exact = true }).WaitForAsync();
            await CaptureScreenshotAsync(screenshotDirectory, "ui-login.png");
        }

        await LoginAsDefaultUserAsync();
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
        var notFoundResponses = new List<string>();
        Page.Console += (_, message) =>
        {
            if (string.Equals(message.Type, "error", StringComparison.OrdinalIgnoreCase))
            {
                browserErrors.Add(message.Text);
            }
        };
        Page.PageError += (_, error) => browserErrors.Add(error);
        Page.Response += (_, response) =>
        {
            if (response.Status == 404)
            {
                notFoundResponses.Add($"{response.Request.Method} {response.Url}");
            }
        };
        Page.RequestFailed += (_, request) =>
        {
            var isExpectedCircuitDisconnect = Uri.TryCreate(request.Url, UriKind.Absolute, out var uri)
                && uri.AbsolutePath.Equals("/_blazor/disconnect", StringComparison.OrdinalIgnoreCase)
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            var isExpectedNavigationAssetAbort = uri is not null
                && (uri.AbsolutePath.Contains("/favicon.", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect && !isExpectedNavigationAssetAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        var coreRoutes = new[]
        {
            "dashboard?tab=0",
            "dashboard/order-create",
            "dashboard?tab=5&periodTab=review",
            "library?tab=2",
            "permission?tab=0",
            "report"
        };

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            foreach (var route in coreRoutes)
            {
                await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.DOMContentLoaded
                });
                // Chờ shell interactive chốt trạng thái (data-shell-ready bật sau first
                // render + router guard). Audit chạy giữa lúc circuit thay DOM prerender
                // từng bắt nhầm trang trung gian không có #main-content/role=main.
                await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Attached
                });
                await Page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible
                });
                // Double-rAF: chờ layout flush xong trước khi audit đọc geometry/DOM.
                await WaitForRenderSettleAsync();

                var audit = await Page.EvaluateAsync<int[]>("""
                () => {
                    const visible = element => {
                        const style = getComputedStyle(element);
                        return style.display !== 'none' && style.visibility !== 'hidden';
                    };
                    const main = document.querySelector('#main-content');
                    const nav = document.querySelector('nav[aria-label]');
                    const menu = document.querySelector('.user-menu-trigger');
                    const brand = document.querySelector('.vpp-sidebar-expanded-brand');
                    const visibleButtons = [...document.querySelectorAll('button')].filter(visible);
                    const unlabeledButtons = visibleButtons.filter(button =>
                        !button.getAttribute('aria-label')
                        && !button.textContent.trim()
                        && !button.querySelector('[aria-label]'));
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        main && main.getAttribute('role') === 'main' ? 0 : 1,
                        nav ? 0 : 1,
                        menu && menu.getAttribute('aria-haspopup') === 'dialog' ? 0 : 1,
                        brand && /GTAS VPP/i.test(brand.textContent) && !/PPJ/i.test(brand.textContent) ? 0 : 1,
                        document.querySelector('.vpp-header-logo') ? 1 : 0,
                        unlabeledButtons.length
                    ];
                }
                """);

                audit[0].Should().Be(0, $"{route} must not overflow at {viewport.Width}px");
                audit[1].Should().Be(0, $"the content landmark should be semantic on {route} at {viewport.Width}px");
                audit[2].Should().Be(0, $"primary navigation should have an accessible name on {route} at {viewport.Width}px");
                audit[3].Should().Be(0, $"the account summary trigger should expose its dialog popup semantics on {route} at {viewport.Width}px");
                audit[4].Should().Be(0,
                    $"the shell should display the independent GTAS VPP brand on {route} at {viewport.Width}px");
                audit[5].Should().Be(0, "the legacy PPJ logo must not be visible in the shell");
                audit[6].Should().Be(0, $"icon-only buttons on {route} need accessible names");

            }
        }

        if (screenshotDirectory is not null)
        {
            await CaptureThesisScreenshotsAsync(screenshotDirectory);
            // Persona switching for screenshot capture intentionally performs
            // several logout/login navigations; its transient SignalR
            // negotiation noise is outside the shell assertion itself.
            browserErrors.Clear();
            requestFailures.Clear();
            notFoundResponses.Clear();
        }

        notFoundResponses.Should().BeEmpty("the authenticated shell should not request missing assets or endpoints");
        browserErrors.Should().BeEmpty("the authenticated shell should not emit browser errors");
        requestFailures.Should().BeEmpty("the authenticated shell should not issue failed network requests");
    }

    private static string? ResolveScreenshotDirectory()
    {
        var configured = Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(configured))
        {
            return null;
        }

        var directory = Path.GetFullPath(configured);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private async Task CaptureThesisScreenshotsAsync(string directory)
    {
        await Page.SetViewportSizeAsync(1920, 1080);

        await CaptureRouteAsync(directory, "ui-users.png", "permission?tab=0", ".vpp-atlas-user-workspace");
        await CaptureRouteAsync(directory, "ui-permission-groups.png", "permission?tab=1", ".vpp-permission-matrix");
        await CaptureRouteAsync(directory, "ui-report.png", "report", ".vpp-report-page");

        await SwitchUserAsync(TestAccounts.Procurement);
        await CaptureRouteAsync(directory, "ui-period-review.png", "dashboard?tab=5&periodTab=review", "[data-testid='period-review-data-surface']");
        await CaptureRouteAsync(directory, "ui-supplement-approval.png", "dashboard?tab=5&periodTab=pending", ".vpp-section");
        await CaptureRouteAsync(directory, "ui-department-summary.png", "dashboard?tab=3&managementTab=department", ".vpp-history-page");
        await CaptureRouteAsync(directory, "ui-period-demand.png", "dashboard?tab=5&periodTab=demand", "[data-testid='period-demand-data-surface']");
        await CaptureRouteAsync(directory, "ui-supply-allocation.png", "dashboard?tab=5&periodTab=supply", "[data-testid='period-supply-data-surface']");
        await CaptureRouteAsync(directory, "ui-settlement-flow.png", "dashboard?tab=5&periodTab=settle", "[data-testid='period-settlement-data-surface']");
        await CaptureRouteAsync(directory, "ui-library-items.png", "library?tab=2", ".vpp-atlas-admin-workspace");
        await CaptureRouteAsync(directory, "ui-price-lists.png", "library?tab=6&pricingTab=price-lists", ".vpp-price-list-workspace");

        await SwitchUserAsync(TestAccounts.Employee);
        await CaptureRouteAsync(directory, "ui-dashboard-my-orders.png", "dashboard?tab=0", ".vpp-orders-workspace");
        await CaptureRouteAsync(directory, "ui-order-create.png", "dashboard/order-create", ".vpp-order-create-page");
        await CaptureRouteAsync(directory, "ui-order-history.png", "dashboard?tab=1", ".vpp-history-page");
        await CaptureRouteAsync(directory, "ui-product-catalog.png", "dashboard?tab=2", ".vpp-catalog-workspace");

        await SwitchUserAsync(TestAccounts.SystemAdmin);
        await CaptureSystemStatesAsync(directory);
    }

    private async Task CaptureRouteAsync(string directory, string fileName, string route, string? focusSelector = null)
    {
        await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached
        });
        if (!string.IsNullOrWhiteSpace(focusSelector))
        {
            var focusTarget = Page.Locator(focusSelector).First;
            await focusTarget.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            await focusTarget.ScrollIntoViewIfNeededAsync();
        }
        if (string.Equals(fileName, "ui-price-lists.png", StringComparison.Ordinal))
        {
            // The empty QA price-book state can briefly show a transient toast
            // while the LocalDB fixture finishes its read-only refresh; return as
            // soon as every Radzen notification has dismissed instead of a fixed sleep.
            await Page.WaitForFunctionAsync(
                "() => [...document.querySelectorAll('.rz-notification-item')].every(el => el.getClientRects().length === 0)",
                null,
                new PageWaitForFunctionOptions { Timeout = 20_000 });
        }
        await CaptureScreenshotAsync(directory, fileName);
    }

    private async Task CaptureSystemStatesAsync(string directory)
    {
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("#main-content").WaitForAsync();
        await Page.Locator(".user-menu-trigger").First.ClickAsync();
        var notificationButton = Page.Locator("#user-menu-dropdown .vpp-header-notification-button");
        await notificationButton.ClickAsync();
        await Page.Locator("#vpp-notification-panel").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        await CaptureScreenshotAsync(directory, "ui-system-states.png");
    }

    private async Task CaptureScreenshotAsync(string directory, string fileName)
    {
        var loader = Page.Locator(".vpp-global-loader");
        if (await loader.CountAsync() > 0)
        {
            await loader.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = 60_000
            });
        }

        await Page.WaitForFunctionAsync(
            "() => [...document.querySelectorAll('.vpp-skeleton-page')].every(element => element.getClientRects().length === 0)",
            null,
            new PageWaitForFunctionOptions { Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            "() => !document.documentElement.classList.contains('vpp-page-entering')");

        // Let the post-loader render flush and web fonts finish before capturing pixels.
        await WaitForRenderSettleAsync();
        await Page.EvaluateAsync("() => document.fonts.ready");
        await Page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = "*,*::before,*::after{animation:none!important;transition:none!important;caret-color:transparent!important}"
        });
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }
}
