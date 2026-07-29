using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class Ds3WorkflowTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task PeriodReview_UsesCanonicalToolbarAndServerPagedSurface()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=review");

        var flowbar = Page.Locator(".vpp-period-flowbar:visible");
        await flowbar.WaitForAsync(new() { Timeout = 60_000 });
        (await flowbar.Locator(".vpp-workflow-step").CountAsync()).Should().Be(4);
        (await flowbar.Locator(".vpp-period-target .vpp-filter-select").CountAsync()).Should().Be(2);
        await Page.Locator(".vpp-period-workspace .vpp-skeleton-page").WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });
        await CaptureAsync("ds3-period-review-loaded-1366x768.png");

        var toolbar = Page.Locator("[data-testid='period-review-data-surface'] .vpp-period-data-toolbar:visible");
        await toolbar.WaitForAsync();
        (await toolbar.Locator(".vpp-filter-search").CountAsync()).Should().Be(1);
        (await toolbar.Locator(".vpp-filter-select").CountAsync()).Should().Be(2);
        var surface = Page.Locator("[data-testid='period-review-data-surface']:visible");
        if (await surface.CountAsync() > 0)
        {
            (await surface.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
            if (await surface.Locator(".rz-data-grid:visible").CountAsync() > 0)
            {
                (await surface.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
            }
            else
            {
                (await surface.Locator(".vpp-content-state:visible").CountAsync()).Should().BeGreaterThan(0);
            }
        }
        else
        {
            (await Page.Locator(".vpp-empty-state:visible, .vpp-content-state:visible").CountAsync())
                .Should().BeGreaterThan(0, "an empty period must use the canonical content state instead of a blank grid");
        }

        var orderTypeFilter = toolbar.Locator(".vpp-filter-select-trigger").First;
        await orderTypeFilter.ClickAsync();
        var popup = Page.Locator(".vpp-filter-select-popover:popover-open").First;
        await popup.WaitForAsync();
        (await popup.GetAttributeAsync("class")).Should().Contain("vpp-transient-surface");
        await Page.Keyboard.PressAsync("Escape");

        var viewportContract = await Page.EvaluateAsync<string>("""
            () => `${document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1}`
                + `|${document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1}`
            """);
        viewportContract.Should().Be("true|true", "period review must stay inside the application main-content scroller");
        await CaptureAsync("ds3-period-review-1366x768.png");

        await AssertPeriodStepAsync("demand", "period-demand-data-surface", "ds3-period-demand-1366x768.png");
        await AssertPeriodStepAsync("supply", "period-supply-data-surface", "ds3-period-supply-1366x768.png");
        await AssertPeriodStepAsync("settle", "period-settlement-data-surface", "ds3-period-settlement-1366x768.png");
    }

    private async Task AssertPeriodStepAsync(string step, string testId, string screenshot)
    {
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab={step}");
        var surface = Page.Locator($"[data-testid='{testId}']:visible");
        await surface.WaitForAsync(new() { Timeout = 60_000 });
        await surface.Locator(".vpp-skeleton-page").WaitForAsync(new()
        {
            State = WaitForSelectorState.Hidden,
            Timeout = 60_000
        });
        (await Page.Locator(".vpp-workflow-step.is-active").CountAsync()).Should().Be(1);
        var viewportContract = await Page.EvaluateAsync<string>("""
            () => `${document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1}`
                + `|${document.documentElement.scrollHeight <= document.documentElement.clientHeight + 1}`
            """);
        viewportContract.Should().Be("true|true");
        await CaptureAsync(screenshot);
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
