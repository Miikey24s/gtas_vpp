using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class DashboardMyOrdersVisualTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task MyOrders_UsesOneResponsiveDataStoryWithoutRepeatedActions()
    {
        var browserErrors = new List<string>();
        var requestFailures = new List<string>();
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
            var isExpectedNavigationAssetAbort = uri is not null
                && (uri.AbsolutePath.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".woff", StringComparison.OrdinalIgnoreCase)
                    || uri.AbsolutePath.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                && request.Failure?.Contains("ERR_ABORTED", StringComparison.OrdinalIgnoreCase) == true;
            if (!isExpectedCircuitDisconnect && !isExpectedNavigationAssetAbort)
            {
                requestFailures.Add($"{request.Method} {request.Url}: {request.Failure}");
            }
        };

        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}set-language?culture=vi&returnUrl=%2Fdashboard%3Ftab%3D0");

        foreach (var viewport in new[]
                 {
                     new ViewportSize { Width = 390, Height = 844 },
                     new ViewportSize { Width = 768, Height = 1024 },
                     new ViewportSize { Width = 1366, Height = 768 },
                     new ViewportSize { Width = 1920, Height = 1080 }
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await Page.Locator(".vpp-orders-story").WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible
            });

            var audit = await Page.EvaluateAsync<int[]>("""
                () => {
                    const visible = element => {
                        const style = getComputedStyle(element);
                        return style.display !== 'none' && style.visibility !== 'hidden';
                    };
                    const orderPage = document.querySelector('.order-page');
                    const createActions = [...document.querySelectorAll('.order-page button')]
                        .filter(visible)
                        .filter(button => /Tạo đơn mới|Create Order/i.test(button.textContent ?? ''));
                    return [
                        document.documentElement.scrollWidth > window.innerWidth + 1 ? 1 : 0,
                        document.querySelectorAll('.vpp-orders-story').length,
                        document.querySelectorAll('.vpp-orders-evidence article').length,
                        document.querySelectorAll('.vpp-orders-section').length,
                        document.querySelectorAll('.vpp-orders-archive').length,
                        orderPage?.querySelectorAll('.rzi').length ?? -1,
                        document.querySelectorAll('.order-page .kpi-grid').length,
                        document.querySelectorAll('.vpp-orders-empty .vpp-empty-state-actions').length,
                        createActions.length
                    ];
                }
                """);

            audit[0].Should().Be(0, $"My Orders must not overflow at {viewport.Width}px");
            audit[1].Should().Be(1, "the period takeaway should appear exactly once");
            audit[2].Should().Be(4, "the round-six story should use four balanced evidence points");
            audit[3].Should().BeGreaterThanOrEqualTo(1, "the current regular-order section must remain visible");
            audit[4].Should().Be(1, "previous-period behavior should use one compact archive row");
            audit[5].Should().Be(0, "My Orders should use VppIcon instead of legacy rzi markup");
            audit[6].Should().Be(0, "the legacy four-card KPI strip should be removed");
            audit[7].Should().Be(0, "empty state must not repeat actions already shown in the story header");
            audit[8].Should().BeLessThanOrEqualTo(1, "the create-order action should have one source of truth");

            if (viewport.Width >= 1366)
            {
                var aboveFold = await Page.EvaluateAsync<double>("""
                    () => document.querySelector('.vpp-orders-section-header')?.getBoundingClientRect().bottom ?? Number.MAX_VALUE
                    """);
                aboveFold.Should().BeLessThan(viewport.Height, "the current-order heading should remain above the fold on desktop");
            }
        }

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.SetViewportSizeAsync(1366, 768);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
            await Page.Locator(".vpp-orders-story").WaitForAsync();
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "w1-dashboard-my-orders-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await Page.GotoAsync($"{BaseUrl}set-language?culture=en&returnUrl=%2Fdashboard%3Ftab%3D0");
        await Page.Locator(".vpp-orders-story").WaitForAsync();
        (await Page.GetByText("Current Order Cycle", new() { Exact = true }).CountAsync()).Should().BeGreaterThan(0);
        (await Page.GetByText("Total items", new() { Exact = true }).CountAsync()).Should().Be(1);

        browserErrors.Should().BeEmpty();
        requestFailures.Should().BeEmpty();
    }
}
