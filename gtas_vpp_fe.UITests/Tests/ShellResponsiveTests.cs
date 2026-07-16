using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class ShellResponsiveTests : TestBase, IAuthenticatedUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_THESIS_SCREENSHOT_DIR";

    [Fact]
    public async Task AuthenticatedShell_IsVietnameseBrandedAccessibleAndResponsive()
    {
        var screenshotDirectory = ResolveScreenshotDirectory();
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

        if (screenshotDirectory is not null)
        {
            await Page.SetViewportSizeAsync(1920, 1080);
            await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2FAccount%2FLogin");
            await Page.GotoAsync($"{BaseUrl}Account/Login");
            await Page.GetByRole(AriaRole.Button, new() { Name = "Đăng nhập", Exact = true }).WaitForAsync();
            await CaptureScreenshotAsync(screenshotDirectory, "ui-login.png");
        }

        await LoginAsDefaultUserAsync();
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
                await Page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible
                });
                await Page.WaitForTimeoutAsync(350);

                var audit = await Page.EvaluateAsync<int[]>("""
                () => {
                    const visible = element => {
                        const style = getComputedStyle(element);
                        return style.display !== 'none' && style.visibility !== 'hidden';
                    };
                    const main = document.querySelector('#main-content');
                    const nav = document.querySelector('nav[aria-label]');
                    const menu = document.querySelector('.user-menu-trigger');
                    const brand = document.querySelector('.vpp-brand-lockup');
                    const visibleButtons = [...document.querySelectorAll('button')].filter(visible);
                    const unlabeledButtons = visibleButtons.filter(button =>
                        !button.getAttribute('aria-label')
                        && !button.textContent.trim()
                        && !button.querySelector('[aria-label]'));
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        main && main.getAttribute('role') === 'main' ? 0 : 1,
                        nav ? 0 : 1,
                        menu && menu.getAttribute('aria-haspopup') === 'menu' ? 0 : 1,
                        brand && /GTAS VPP/i.test(brand.textContent) && !/PPJ/i.test(brand.textContent) ? 0 : 1,
                        document.querySelector('.vpp-header-logo') ? 1 : 0,
                        unlabeledButtons.length
                    ];
                }
                """);

                audit[0].Should().Be(0, $"{route} must not overflow at {viewport.Width}px");
                audit[1].Should().Be(0, "the content landmark should be semantic");
                audit[2].Should().Be(0, "primary navigation should have an accessible name");
                audit[3].Should().Be(0, "the account menu trigger should expose menu semantics");
                audit[4].Should().Be(0, "the shell should display the independent GTAS VPP brand");
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

        await CaptureRouteAsync(directory, "ui-permission-groups.png", "permission?tab=1");

        await SwitchUserAsync(TestAccounts.Procurement);
        await CaptureRouteAsync(directory, "ui-period-operations.png", "dashboard?tab=5&periodTab=review");
        await CaptureRouteAsync(directory, "ui-all-orders-summary.png", "dashboard?tab=3&managementTab=all");
        await CaptureRouteAsync(directory, "ui-library-items.png", "library?tab=2");
        await CaptureRouteAsync(directory, "ui-price-lists.png", "library?tab=6&pricingTab=price-lists");

        await SwitchUserAsync(TestAccounts.Employee);
        await CaptureRouteAsync(directory, "ui-dashboard-my-orders.png", "dashboard?tab=0");
        await CaptureRouteAsync(directory, "ui-order-create.png", "dashboard/order-create");
        await CaptureRouteAsync(directory, "ui-order-history.png", "dashboard?tab=1");
    }

    private async Task CaptureRouteAsync(string directory, string fileName, string route)
    {
        await Page.GotoAsync($"{BaseUrl}{route}", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("#main-content").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
        if (string.Equals(fileName, "ui-price-lists.png", StringComparison.Ordinal))
        {
            // The empty QA price-book state can briefly show a transient toast
            // while the LocalDB fixture finishes its read-only refresh.
            await Page.WaitForTimeoutAsync(5_500);
        }
        await CaptureScreenshotAsync(directory, fileName);
    }

    private async Task CaptureScreenshotAsync(string directory, string fileName)
    {
        var loader = Page.Locator(".vpp-global-loader");
        if (await loader.CountAsync() > 0)
        {
            try
            {
                await loader.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Hidden,
                    Timeout = 30_000
                });
            }
            catch (TimeoutException)
            {
                // The screenshot remains useful even if a background refresh keeps the shared loader alive.
            }
        }

        await Page.WaitForTimeoutAsync(800);
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
