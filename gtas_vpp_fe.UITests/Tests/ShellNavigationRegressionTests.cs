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

        var hierarchy = await Page.EvaluateAsync<SidebarHierarchyGeometry>(
            """
            () => {
                const item = title => [...document.querySelectorAll('.vpp-sidebar-nav .rz-navigation-item')]
                    .find(element => element.title === title);
                const parts = title => {
                    const wrapper = item(title).querySelector(':scope > .rz-navigation-item-wrapper');
                    return {
                        icon: wrapper.querySelector(':scope > .rz-navigation-item-link > .rz-navigation-item-icon').getBoundingClientRect(),
                        text: wrapper.querySelector(':scope > .rz-navigation-item-link > .rz-navigation-item-text').getBoundingClientRect()
                    };
                };
                const root = parts('Quản trị danh mục');
                const child = parts('Định nghĩa lớp');
                const childParent = parts('Bảng giá');
                const grandchild = parts('Giá');
                const indicator = document.querySelector('.vpp-sidebar-shared-indicator').getBoundingClientRect();
                return {
                    rootTextLeft: root.text.left,
                    childIconLeft: child.icon.left,
                    childIconRight: child.icon.right,
                    childTextLeft: child.text.left,
                    childParentTextLeft: childParent.text.left,
                    grandchildIconLeft: grandchild.icon.left,
                    activeIndicatorLeft: indicator.left
                };
            }
            """);
        hierarchy.ChildIconLeft.Should().BeApproximately(hierarchy.RootTextLeft, 0.1,
            "a child icon must start on the same column as its parent label");
        hierarchy.GrandchildIconLeft.Should().BeApproximately(hierarchy.ChildParentTextLeft, 0.1,
            "a grandchild icon must start on the same column as its parent label");
        (hierarchy.ChildTextLeft - hierarchy.ChildIconRight).Should().BeApproximately(12, 0.1,
            "Apple-style icon and label spacing should use one 12px rhythm");
        (hierarchy.ChildIconLeft - hierarchy.ActiveIndicatorLeft).Should().BeApproximately(12, 0.1,
            "the child rail must sit immediately before the child icon");

        var dashboardParent = nav.Locator(".rz-panel-menu > li[title='Bảng điều khiển']");
        var dashboardParentToggle = dashboardParent.Locator(":scope > .rz-navigation-item-wrapper");
        var dashboardExpander = dashboardParent.Locator(":scope > .rz-expander");
        var dashboardMotion = await dashboardExpander.EvaluateAsync<NavigationMotionTiming>(
            """
            element => {
                const styles = getComputedStyle(element);
                return {
                    duration: styles.transitionDuration,
                    easing: styles.transitionTimingFunction
                };
            }
            """);
        dashboardMotion.Duration.Should().Be("0.2s");
        dashboardMotion.Easing.Should().Be("cubic-bezier(0.32, 0.72, 0, 1)");
        if (string.Equals(await dashboardParent.GetAttributeAsync("aria-expanded"), "true", StringComparison.OrdinalIgnoreCase))
        {
            await dashboardParentToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }

        var collapsedDashboardTop = await ReadIndicatorTopAsync(indicator);
        var expandingTops = await SampleIndicatorMotionAsync(indicator, dashboardParentToggle);
        AssertSmoothMotion(expandingTops, movingDown: true, "opening the preceding dashboard group");
        expandingTops[^1].Should().BeGreaterThan(collapsedDashboardTop + 40,
            "the active library child should move down when the preceding group opens");

        var collapsingTops = await SampleIndicatorMotionAsync(indicator, dashboardParentToggle);
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

        var headerIndicatorDuration = await Page.EvaluateAsync<double>(
            """
            () => {
                const tabs = document.querySelectorAll('.vpp-admin-tabs [role="tab"]');
                const indicator = document.querySelector('.vpp-admin-tabs .vpp-tab-shared-indicator');
                tabs[1].click();
                const animation = indicator.getAnimations()[0];
                return animation ? Number(animation.effect.getTiming().duration) : 0;
            }
            """);
        headerIndicatorDuration.Should().BeApproximately(200, 0.1,
            "the header line must use the same motion duration as the PanelMenu expansion");

        await Page.WaitForTimeoutAsync(300);
        var shellSidebar = Page.Locator(".vpp-sidebar");
        if (!await shellSidebar.EvaluateAsync<bool>("element => element.classList.contains('sidebar-collapsed')"))
        {
            await Page.EvaluateAsync("() => document.querySelector('.vpp-sidebar-toggle').click()");
        }
        await Page.WaitForTimeoutAsync(300);
        var collapsedRail = await Page.EvaluateAsync<CollapsedRailGeometry>(
            """
            () => {
                const rect = selector => document.querySelector(selector).getBoundingClientRect();
                const centreX = value => value.left + value.width / 2;
                const centreY = value => value.top + value.height / 2;
                const logo = rect('.vpp-sidebar-collapsed-logo');
                const brand = rect('.vpp-sidebar-collapsed-brand');
                const icon = rect('.vpp-sidebar-nav .rz-panel-menu > .rz-navigation-item .rz-navigation-item-icon');
                const iconRow = rect('.vpp-sidebar-nav .rz-panel-menu > .rz-navigation-item > .rz-navigation-item-wrapper');
                const avatar = rect('.vpp-sidebar-user-menu .user-avatar');
                const userTrigger = rect('.vpp-sidebar-user-menu .user-menu-trigger');
                return {
                    logoCenterX: centreX(logo),
                    iconCenterX: centreX(icon),
                    avatarCenterX: centreX(avatar),
                    logoRowCenterY: centreY(brand),
                    logoCenterY: centreY(logo),
                    iconRowCenterY: centreY(iconRow),
                    iconCenterY: centreY(icon),
                    userRowCenterY: centreY(userTrigger),
                    avatarCenterY: centreY(avatar)
                };
            }
            """);
        collapsedRail.LogoCenterX.Should().BeApproximately(collapsedRail.IconCenterX, 0.1);
        collapsedRail.AvatarCenterX.Should().BeApproximately(collapsedRail.IconCenterX, 0.1);
        collapsedRail.LogoCenterY.Should().BeApproximately(collapsedRail.LogoRowCenterY, 0.1);
        collapsedRail.IconCenterY.Should().BeApproximately(collapsedRail.IconRowCenterY, 0.1);
        collapsedRail.AvatarCenterY.Should().BeApproximately(collapsedRail.UserRowCenterY, 0.1);
    }

    private async Task<double> ReadIndicatorTopAsync(ILocator indicator)
    {
        return await indicator.EvaluateAsync<double>("element => element.getBoundingClientRect().top");
    }

    private async Task<List<double>> SampleIndicatorMotionAsync(ILocator indicator, ILocator toggle)
    {
        var samplingTask = indicator.EvaluateAsync<double[]>(
            """
            element => new Promise(resolve => {
                const samples = [];
                const sampleFrame = () => {
                    samples.push(element.getBoundingClientRect().top);
                    if (samples.length < 24) {
                        requestAnimationFrame(sampleFrame);
                        return;
                    }
                    resolve(samples);
                };
                requestAnimationFrame(sampleFrame);
            })
            """);
        await toggle.ClickAsync();
        return [.. await samplingTask];
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

    private sealed class NavigationMotionTiming
    {
        public string Duration { get; set; } = string.Empty;

        public string Easing { get; set; } = string.Empty;
    }

    private sealed class SidebarHierarchyGeometry
    {
        public double RootTextLeft { get; set; }
        public double ChildIconLeft { get; set; }
        public double ChildIconRight { get; set; }
        public double ChildTextLeft { get; set; }
        public double ChildParentTextLeft { get; set; }
        public double GrandchildIconLeft { get; set; }
        public double ActiveIndicatorLeft { get; set; }
    }

    private sealed class CollapsedRailGeometry
    {
        public double LogoCenterX { get; set; }
        public double IconCenterX { get; set; }
        public double AvatarCenterX { get; set; }
        public double LogoRowCenterY { get; set; }
        public double LogoCenterY { get; set; }
        public double IconRowCenterY { get; set; }
        public double IconCenterY { get; set; }
        public double UserRowCenterY { get; set; }
        public double AvatarCenterY { get; set; }
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
