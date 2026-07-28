using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class WorkspacePatternTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task F4Patterns_RenderOnRealRoutesWithOneSharedPageInsetContract()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await AssertShellSeamAsync();

        var routes = new[]
        {
            new PatternRoute("analytics", "history", "dashboard?tab=1"),
            new PatternRoute("analytics", "department", "dashboard?tab=3"),
            new PatternRoute("collection", "catalog", "dashboard?tab=2"),
            new PatternRoute("collection", "prices", "library?tab=6&pricingTab=prices"),
            new PatternRoute("operation", "pending", "dashboard?tab=5&periodTab=pending"),
            new PatternRoute("operation", "period-review", "dashboard?tab=5&periodTab=review"),
            new PatternRoute("split-editor", "lookup", "library?tab=0"),
            new PatternRoute("split-editor", "order-create", "dashboard/order-create"),
            new PatternRoute("list-detail", "library", "library?tab=1"),
            new PatternRoute("list-detail", "users", "permission?tab=0")
        };
        PageInsetGeometry? baseline = null;

        foreach (var route in routes)
        {
            await Page.GotoAsync($"{BaseUrl}{route.Path}", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await Page.WaitForFunctionAsync(
                "() => !document.documentElement.classList.contains('vpp-page-entering')");

            var pattern = Page.Locator($"[data-vpp-workspace-pattern='{route.Pattern}']").First;
            await pattern.WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 60_000
            });
            await WaitForPatternContentAsync(pattern);

            var geometry = await Page.EvaluateAsync<PageInsetGeometry>(
                $$"""
                () => {
                    const body = document.querySelector('.vpp-layout-body');
                    const content = document.querySelector('.vpp-content');
                    const style = getComputedStyle(body);
                    const rect = body.getBoundingClientRect();
                    const contentRect = content.getBoundingClientRect();
                    const pattern = document.querySelector('[data-vpp-workspace-pattern="{{route.Pattern}}"]');
                    const patternRect = pattern.getBoundingClientRect();
                    return {
                        top: Number.parseFloat(style.paddingTop),
                        right: Number.parseFloat(style.paddingRight),
                        bottom: Number.parseFloat(style.paddingBottom),
                        left: Number.parseFloat(style.paddingLeft),
                        patternInsideInlineStart: patternRect.left + .5 >= rect.left + Number.parseFloat(style.paddingLeft),
                        patternInsideBlockStart: patternRect.top + .5 >= rect.top + Number.parseFloat(style.paddingTop),
                        contentTopGap: contentRect.top - rect.top,
                        contentRightGap: rect.right - contentRect.right,
                        contentBottomGap: rect.bottom - contentRect.bottom,
                        contentLeftGap: contentRect.left - rect.left,
                        hasDocumentOverflow: document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
                    };
                }
                """);

            geometry.PatternInsideInlineStart.Should().BeTrue($"{route.Pattern} must respect the shell inline inset");
            geometry.PatternInsideBlockStart.Should().BeTrue($"{route.Pattern} must respect the shell top inset");
            geometry.ContentLeftGap.Should().BeApproximately(geometry.Left, 0.1, "main content must start at the shell inline inset");
            geometry.ContentRightGap.Should().BeApproximately(geometry.Right, 0.1, "main content must end at the shell inline inset");
            geometry.ContentTopGap.Should().BeApproximately(geometry.Top, 0.1, "main content must start below the header by the shared block inset");
            geometry.ContentBottomGap.Should().BeApproximately(geometry.Bottom, 0.1, "main content must stop above the viewport edge by the shared block inset");
            geometry.ContentLeftGap.Should().BeApproximately(geometry.ContentRightGap, 0.1, "inline outer inset must be symmetric");
            geometry.HasDocumentOverflow.Should().BeFalse($"{route.Pattern} must not overflow the document horizontally");

            if (baseline is null)
            {
                baseline = geometry;
            }
            else
            {
                geometry.Top.Should().BeApproximately(baseline.Top, 0.1, "the same edge must use one page inset across routes");
                geometry.Right.Should().BeApproximately(baseline.Right, 0.1, "the same edge must use one page inset across routes");
                geometry.Bottom.Should().BeApproximately(baseline.Bottom, 0.1, "the same edge must use one page inset across routes");
                geometry.Left.Should().BeApproximately(baseline.Left, 0.1, "the same edge must use one page inset across routes");
            }

            await CaptureEvidenceAsync(route.EvidenceName);
        }

        await Page.GotoAsync($"{BaseUrl}perform-logout", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("[data-vpp-workspace-pattern='account']").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await CaptureEvidenceAsync("account-login");

        await Page.GotoAsync($"{BaseUrl}Account/ForgotPassword", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator("[data-vpp-workspace-pattern='account']").WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await CaptureEvidenceAsync("account-forgot");
    }

    private async Task AssertShellSeamAsync()
    {
        await AssertShellSeamStateAsync(expectedWidth: 286, collapsed: false);

        await Page.Locator(".vpp-sidebar-toggle").ClickAsync();
        await AssertShellSeamStateAsync(expectedWidth: 72, collapsed: true);

        await Page.Locator(".vpp-sidebar-collapsed-brand").ClickAsync();
        await AssertShellSeamStateAsync(expectedWidth: 286, collapsed: false);
    }

    private async Task AssertShellSeamStateAsync(double expectedWidth, bool collapsed)
    {
        await Page.WaitForFunctionAsync(
            """
            expected => {
                const sidebar = document.querySelector('.vpp-sidebar');
                const body = document.querySelector('.vpp-layout-body');
                if (!sidebar || !body) return false;
                const width = sidebar.getBoundingClientRect().width;
                return Math.abs(width - expected.width) <= .2
                    && sidebar.classList.contains('sidebar-collapsed') === expected.collapsed;
            }
            """,
            new { width = expectedWidth, collapsed });

        var seam = await Page.EvaluateAsync<ShellSeamGeometry>(
            """
            () => {
                const layout = document.querySelector('.vpp-layout').getBoundingClientRect();
                const sidebar = document.querySelector('.vpp-sidebar').getBoundingClientRect();
                const header = document.querySelector('.vpp-layout-header').getBoundingClientRect();
                const body = document.querySelector('.vpp-layout-body').getBoundingClientRect();
                return {
                    layoutLeft: layout.left,
                    sidebarLeft: sidebar.left,
                    sidebarRight: sidebar.right,
                    headerLeft: header.left,
                    bodyLeft: body.left,
                    bodyRight: body.right,
                    viewportRight: window.innerWidth
                };
            }
            """);

        seam.SidebarLeft.Should().BeApproximately(seam.LayoutLeft, 0.1);
        seam.HeaderLeft.Should().BeApproximately(seam.SidebarRight, 0.2,
            "header and sidebar must share one straight shell seam");
        seam.BodyLeft.Should().BeApproximately(seam.SidebarRight, 0.2,
            "body and sidebar must share one straight shell seam");
        seam.BodyRight.Should().BeApproximately(seam.ViewportRight, 0.2);
    }

    private async Task CaptureEvidenceAsync(string pattern)
    {
        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(evidenceDirectory);
        await Page.ScreenshotAsync(new()
        {
            Path = Path.Combine(evidenceDirectory, $"f4-{pattern}-1366x768.png"),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide
        });
    }

    private async Task WaitForPatternContentAsync(ILocator pattern)
    {
        if (await pattern.GetAttributeAsync("aria-busy") is null)
        {
            return;
        }

        await pattern.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await Page.WaitForFunctionAsync(
            "element => element.getAttribute('aria-busy') !== 'true'",
            await pattern.ElementHandleAsync(),
            new() { Timeout = 30_000 });
    }

    private sealed record PatternRoute(string Pattern, string EvidenceName, string Path);

    private sealed class PageInsetGeometry
    {
        public double Top { get; init; }
        public double Right { get; init; }
        public double Bottom { get; init; }
        public double Left { get; init; }
        public double ContentTopGap { get; init; }
        public double ContentRightGap { get; init; }
        public double ContentBottomGap { get; init; }
        public double ContentLeftGap { get; init; }
        public bool PatternInsideInlineStart { get; init; }
        public bool PatternInsideBlockStart { get; init; }
        public bool HasDocumentOverflow { get; init; }
    }

    private sealed class ShellSeamGeometry
    {
        public double LayoutLeft { get; init; }
        public double SidebarLeft { get; init; }
        public double SidebarRight { get; init; }
        public double HeaderLeft { get; init; }
        public double BodyLeft { get; init; }
        public double BodyRight { get; init; }
        public double ViewportRight { get; init; }
    }
}
