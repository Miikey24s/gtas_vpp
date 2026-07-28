using System.Collections.Concurrent;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class DataSurfaceFoundationTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PagedAndVirtualizedConsumers_ExposeTypedFoundationWithoutScrollRequests()
    {
        await LoginAsAsync(TestAccounts.Employee);

        foreach (var viewport in new[]
                 {
                     (Width: 1920, Height: 1080),
                     (Width: 1366, Height: 768),
                     (Width: 768, Height: 1024),
                     (Width: 390, Height: 844)
                 })
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.GotoAsync($"{BaseUrl}dashboard?tab=1");
            await WaitForGridRowsAsync(".vpp-history-grid");

            var pagedSurface = Page.GetByTestId("history-orders-data-surface");
            await pagedSurface.WaitForAsync();
            (await pagedSurface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
            (await pagedSurface.GetAttributeAsync("data-vpp-data-density")).Should().Be("compact");
            (await pagedSurface.Locator(".vpp-data-toolbar").CountAsync()).Should().Be(0,
                "the History toolbar remains outside the inner data frame during DS1");
            (await Page.Locator(".vpp-history-orders-card [data-vpp-data-toolbar='true']").CountAsync()).Should().Be(1);
            (await pagedSurface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
            await AssertNoDocumentOverflowAsync(viewport.Width);
            await CaptureAsync($"ds1-history-paged-{viewport.Width}x{viewport.Height}.png");

            if (viewport.Width >= 1000)
            {
                await AssertDesktopRhythmAsync();
                var codeTrigger = pagedSurface.Locator(".vpp-cell-value-popover-code .vpp-cell-value-popover-trigger").First;
                await codeTrigger.ClickAsync();
                var panel = pagedSurface.Locator(".vpp-cell-value-popover-panel").First;
                await panel.WaitForAsync();
                (await panel.GetAttributeAsync("class")).Should().Contain("vpp-transient-surface");
                await CaptureAsync($"ds1-history-cell-popover-{viewport.Width}x{viewport.Height}.png");
                await Page.Keyboard.PressAsync("Escape");
            }

            if (viewport.Width < 1000)
            {
                continue;
            }

            await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
            var virtualizedSurface = Page.GetByTestId("current-order-panel-data-surface");
            await virtualizedSurface.WaitForAsync(new() { Timeout = 60_000 });
            await WaitForGridRowsAsync(".vpp-order-items-grid");
            (await virtualizedSurface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("client-snapshot-virtualized");
            (await virtualizedSurface.GetAttributeAsync("data-vpp-data-density")).Should().Be("rich-two-line");
            (await virtualizedSurface.Locator("[data-vpp-data-footer-mode='virtualized']").CountAsync()).Should().Be(1);
            (await virtualizedSurface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().Be(0);

            var renderedRows = await virtualizedSurface.Locator("tbody tr").CountAsync();
            renderedRows.Should().BeLessThanOrEqualTo(40, "virtualization must keep the mounted DOM bounded");

            var apiRequests = new ConcurrentQueue<string>();
            void ObserveRequest(object? _, IRequest request)
            {
                if (request.Url.Contains("/api/VPPRequest/", StringComparison.OrdinalIgnoreCase))
                {
                    apiRequests.Enqueue(request.Url);
                }
            }

            Page.Request += ObserveRequest;
            await virtualizedSurface.Locator(".rz-data-grid-data").EvaluateAsync("element => element.scrollTop = element.scrollHeight");
            await WaitForRenderSettleAsync();
            Page.Request -= ObserveRequest;

            apiRequests.Should().BeEmpty("client snapshot virtualization must not fetch another window while scrolling");
            await AssertNoDocumentOverflowAsync(viewport.Width);
            await CaptureAsync($"ds1-order-items-virtualized-{viewport.Width}x{viewport.Height}.png");

            if (viewport.Width == 1366)
            {
                await SetDarkModeAsync(true);
                await CaptureAsync("ds1-order-items-virtualized-dark-1366x768.png");
                await SetDarkModeAsync(false);
            }
        }
    }

    private async Task AssertDesktopRhythmAsync()
    {
        var rhythm = await Page.EvaluateAsync<double[]>("""
            () => {
                const toolbar = document.querySelector('.vpp-history-orders-toolbar');
                const header = document.querySelector('.vpp-history-grid thead th');
                const row = document.querySelector('.vpp-history-grid tbody tr');
                const footer = document.querySelector('.vpp-history-grid .rz-paginator, .vpp-history-grid .rz-pager');
                return [toolbar, header, row, footer].map(element => element?.getBoundingClientRect().height ?? 0);
            }
            """);

        rhythm[0].Should().BeApproximately(42, 1);
        rhythm[1].Should().BeApproximately(40, 1);
        rhythm[2].Should().BeApproximately(40, 1);
        rhythm[3].Should().BeApproximately(42, 1);
    }

    private async Task AssertNoDocumentOverflowAsync(int viewportWidth)
    {
        var geometry = await Page.EvaluateAsync<double[]>("""
            () => [document.documentElement.scrollWidth, document.documentElement.clientWidth]
            """);
        geometry[0].Should().BeLessThanOrEqualTo(geometry[1] + 1,
            $"the {viewportWidth}px viewport must not gain document-level horizontal scroll");
    }

    private async Task WaitForGridRowsAsync(string selector)
    {
        await Page.WaitForFunctionAsync(
            $"() => document.querySelectorAll('{selector} tbody tr td').length > 1",
            null,
            new() { Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            "() => !document.querySelector('.rz-datatable-loading')",
            null,
            new() { Timeout = 60_000 });
    }

    private async Task SetDarkModeAsync(bool darkMode)
    {
        var currentMode = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('rz-theme-dark')");
        if (currentMode == darkMode)
        {
            return;
        }

        var dropdown = Page.Locator("#user-menu-dropdown");
        if (!await dropdown.IsVisibleAsync())
        {
            await Page.Locator(".user-menu-trigger").First.ClickAsync();
        }
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            darkMode
                ? "() => document.documentElement.classList.contains('rz-theme-dark')"
                : "() => !document.documentElement.classList.contains('rz-theme-dark')");
        if (await dropdown.IsVisibleAsync())
        {
            await Page.Locator(".user-dropdown-backdrop").ClickAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await WaitForRenderSettleAsync();
    }

    private async Task CaptureAsync(string fileName)
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(evidenceDirectory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(evidenceDirectory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }
}
