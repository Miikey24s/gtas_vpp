using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class ShellNavigationRegressionTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task ActiveChildIndicator_FollowsSiblingExpansion_HidesWithItsParent_AndAlignsWithBrand()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();

        await Page.GotoAsync($"{BaseUrl}library?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var sidebar = Page.Locator(".vpp-sidebar");
        var nav = sidebar.Locator(".vpp-sidebar-nav");
        if (await sidebar.EvaluateAsync<bool>("element => element.classList.contains('sidebar-collapsed')"))
        {
            await sidebar.Locator(".vpp-sidebar-collapsed-brand").ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }

        var activeChild = nav.Locator("a[href='/library?tab=0']").First;
        var libraryParent = nav.Locator(".rz-panel-menu > li[title='Quản trị danh mục']");
        var libraryParentToggle = libraryParent.Locator(":scope > .rz-navigation-item-wrapper");
        var indicator = nav.Locator(":scope > .vpp-sidebar-shared-indicator");

        await activeChild.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        await indicator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 30_000
        });

        (await indicator.EvaluateAsync<bool>(
            "element => element.classList.contains('is-ready') && getComputedStyle(element).opacity === '1'"))
            .Should().BeTrue("the active library child starts visible");

        var dashboardParent = nav.Locator(".rz-panel-menu > li[title='Bảng điều khiển']");
        var dashboardParentToggle = dashboardParent.Locator(":scope > .rz-navigation-item-wrapper");
        if (string.Equals(await dashboardParent.GetAttributeAsync("aria-expanded"), "true", StringComparison.OrdinalIgnoreCase))
        {
            await dashboardParentToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }

        var collapsedDashboardTop = await ReadIndicatorTopAsync(indicator);
        await dashboardParentToggle.ClickAsync();
        var expandingTops = await SampleIndicatorMotionAsync(indicator);
        AssertSmoothMotion(expandingTops, movingDown: true, "opening the preceding dashboard group");
        expandingTops[^1].Should().BeGreaterThan(collapsedDashboardTop + 40,
            "the active library child should move down when the preceding group opens");

        await dashboardParentToggle.ClickAsync();
        var collapsingTops = await SampleIndicatorMotionAsync(indicator);
        AssertSmoothMotion(collapsingTops, movingDown: false, "closing the preceding dashboard group");
        collapsingTops[^1].Should().BeLessThan(expandingTops[^1] - 40,
            "the active library child should move back up when the preceding group closes");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await libraryParentToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var collapsedState = await indicator.EvaluateAsync<IndicatorState>(
                """
                element => ({
                    ready: element.classList.contains('is-ready'),
                    opacity: Number.parseFloat(getComputedStyle(element).opacity),
                    top: element.getBoundingClientRect().top
                })
                """);
            collapsedState.Ready.Should().BeFalse($"collapsed attempt {attempt + 1} must remove the active indicator state");
            collapsedState.Opacity.Should().BeApproximately(0, 0.001, $"collapsed attempt {attempt + 1} must hide the line");

            await libraryParentToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            var expandedState = await indicator.EvaluateAsync<IndicatorState>(
                """
                element => ({
                    ready: element.classList.contains('is-ready'),
                    opacity: Number.parseFloat(getComputedStyle(element).opacity),
                    top: element.getBoundingClientRect().top
                })
                """);
            expandedState.Ready.Should().BeTrue($"expanded attempt {attempt + 1} must restore the active indicator");
            expandedState.Opacity.Should().BeApproximately(1, 0.001, $"expanded attempt {attempt + 1} must show the line");
        }

        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        var primaryTabTitle = Page.GetByText("Đơn hàng của tôi", new PageGetByTextOptions
        {
            Exact = true
        }).First;
        await primaryTabTitle.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        var tabGeometry = await primaryTabTitle.EvaluateAsync<TabTitleGeometry>(
            """
            element => {
                const title = element.getBoundingClientRect();
                const nav = element.closest('.rz-tabview-nav').getBoundingClientRect();
                const styles = getComputedStyle(element);
                return {
                    supportsTextBox: CSS.supports('text-box', 'trim-both cap alphabetic'),
                    textBoxTrim: styles.textBoxTrim,
                    textBoxEdge: styles.textBoxEdge,
                    centerDelta: (title.top + title.height / 2) - (nav.top + nav.height / 2),
                    navTop: nav.top,
                    navHeight: nav.height,
                    titleCenter: title.top + title.height / 2,
                    brandCenter: (() => {
                        const brand = document.querySelector('.vpp-sidebar-product').getBoundingClientRect();
                        return brand.top + brand.height / 2;
                    })(),
                    logoCenter: (() => {
                        const logo = document.querySelector('.vpp-sidebar-expanded-logo').getBoundingClientRect();
                        return logo.top + logo.height / 2;
                    })()
                };
            }
            """);

        tabGeometry.SupportsTextBox.Should().BeTrue("the project Chromium runtime supports CSS text-box metrics");
        tabGeometry.TextBoxTrim.Should().Be("trim-both");
        tabGeometry.TextBoxEdge.Should().Be("cap alphabetic");
        Math.Abs(tabGeometry.CenterDelta).Should().BeLessThan(0.1, "the trimmed visible text box must be centred in the full header");
        tabGeometry.NavTop.Should().BeApproximately(0, 0.1, "the full-bleed header must start at the viewport top");
        tabGeometry.NavHeight.Should().BeApproximately(72, 0.1, "the header must match the collapsed sidebar width");
        tabGeometry.TitleCenter.Should().BeApproximately(36, 0.1, "the visible text must centre against the outer 72px header");
        tabGeometry.BrandCenter.Should().BeApproximately(tabGeometry.TitleCenter, 0.1,
            "GTAS VPP must share the same visual centre as every primary tab label");
        tabGeometry.LogoCenter.Should().BeApproximately(tabGeometry.TitleCenter, 0.1,
            "the brand mark must share the same visual centre as the header labels");
    }

    private async Task<double> ReadIndicatorTopAsync(ILocator indicator)
    {
        return await indicator.EvaluateAsync<double>("element => element.getBoundingClientRect().top");
    }

    private async Task<List<double>> SampleIndicatorMotionAsync(ILocator indicator)
    {
        var samples = new List<double>();
        for (var frame = 0; frame < 16; frame++)
        {
            await Page.WaitForTimeoutAsync(16);
            samples.Add(await ReadIndicatorTopAsync(indicator));
        }

        return samples;
    }

    private static void AssertSmoothMotion(IReadOnlyList<double> samples, bool movingDown, string because)
    {
        var deltas = samples.Zip(samples.Skip(1), (current, next) => next - current).ToArray();
        var directedMoves = movingDown
            ? deltas.Count(delta => delta > 0.2)
            : deltas.Count(delta => delta < -0.2);
        var totalDistance = Math.Abs(samples[^1] - samples[0]);
        var largestStep = deltas.Select(Math.Abs).DefaultIfEmpty(0).Max();

        directedMoves.Should().BeGreaterThanOrEqualTo(3, $"{because} must produce multiple visible frames");
        totalDistance.Should().BeGreaterThan(20, $"{because} must move the line a meaningful distance");
        largestStep.Should().BeLessThan(totalDistance * 0.75,
            $"{because} must not teleport the line directly to its final position");
    }

    private sealed class IndicatorState
    {
        public bool Ready { get; set; }

        public double Opacity { get; set; }

        public double Top { get; set; }
    }

    private sealed class TabTitleGeometry
    {
        public bool SupportsTextBox { get; set; }

        public string TextBoxTrim { get; set; } = string.Empty;

        public string TextBoxEdge { get; set; } = string.Empty;

        public double CenterDelta { get; set; }

        public double NavTop { get; set; }

        public double NavHeight { get; set; }

        public double TitleCenter { get; set; }

        public double BrandCenter { get; set; }

        public double LogoCenter { get; set; }
    }
}
