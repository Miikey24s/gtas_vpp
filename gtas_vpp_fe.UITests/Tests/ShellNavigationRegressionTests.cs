using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
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
        // Chờ shell chốt trạng thái mở/thu (storage + viewport) trước khi đọc class —
        // sidebar desktop nay mặc định mở rộng sau first render (D12).
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(new LocatorWaitForOptions
        {
            Timeout = 30_000
        });
        if (await sidebar.EvaluateAsync<bool>("element => element.classList.contains('sidebar-collapsed')"))
        {
            await sidebar.Locator(".vpp-sidebar-collapsed-brand").ClickAsync();
            // Chờ sidebar thực sự bỏ class sidebar-collapsed thay vì ngủ cứng.
            await Page.WaitForFunctionAsync(
                "() => { const el = document.querySelector('.vpp-sidebar'); return !!el && !el.classList.contains('sidebar-collapsed'); }");
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

        // Animation shell-enter/sidebar dùng transform nên getBoundingClientRect trả
        // tọa độ sub-pixel khi đo giữa chừng (thấy 7.35 thay vì 8 trên circuit lạnh).
        // Chờ hết page-entering và hình học logo đứng yên qua 2 khung rAF liên tiếp.
        await Page.WaitForFunctionAsync("() => !document.documentElement.classList.contains('vpp-page-entering')");
        await Page.WaitForFunctionAsync("""
            () => new Promise(resolve => {
                const measure = () => document.querySelector('.vpp-sidebar-expanded-logo')?.getBoundingClientRect().left ?? -1;
                const first = measure();
                requestAnimationFrame(() => requestAnimationFrame(() => resolve(first >= 0 && measure() === first)));
            })
            """);

        var hierarchy = await Page.EvaluateAsync<SidebarHierarchyGeometry>(
            """
            () => {
                const item = title => {
                    const node = [...document.querySelectorAll('.vpp-sidebar-nav .rz-navigation-item')]
                        .find(element => element.title === title);
                    if (!node) {
                        throw new Error(`Sidebar item not found: ${title}`);
                    }

                    return node;
                };
                const parts = title => {
                    const wrapper = item(title).querySelector(':scope > .rz-navigation-item-wrapper');
                    return {
                        icon: wrapper.querySelector(':scope > .rz-navigation-item-link > .rz-navigation-item-icon').getBoundingClientRect(),
                        text: wrapper.querySelector(':scope > .rz-navigation-item-link > .rz-navigation-item-text').getBoundingClientRect()
                    };
                };
                // Nhãn theo resx hiện hành (Atlas W-C): ClassDefinitions = "Loại danh mục",
                // Prices = "Giá mặt hàng".
                const root = parts('Quản trị danh mục');
                const child = parts('Loại danh mục');
                const childParent = parts('Bảng giá');
                const grandchild = parts('Giá mặt hàng');
                const indicator = document.querySelector('.vpp-sidebar-shared-indicator').getBoundingClientRect();
                return {
                    rootIconLeft: root.icon.left,
                    rootTextLeft: root.text.left,
                    childIconLeft: child.icon.left,
                    childIconRight: child.icon.right,
                    childTextLeft: child.text.left,
                    childParentTextLeft: childParent.text.left,
                    grandchildIconLeft: grandchild.icon.left,
                    activeIndicatorLeft: indicator.left,
                    logoLeft: document.querySelector('.vpp-sidebar-expanded-logo').getBoundingClientRect().left,
                    avatarLeft: document.querySelector('.vpp-sidebar-user-menu .user-avatar').getBoundingClientRect().left
                };
            }
            """);
        hierarchy.RootIconLeft.Should().BeApproximately(8, 0.1,
            "the expanded content rail should sit close to the sidebar edge like Apple Music");
        hierarchy.LogoLeft.Should().BeApproximately(hierarchy.RootIconLeft, 0.1);
        hierarchy.AvatarLeft.Should().BeApproximately(hierarchy.RootIconLeft, 0.1);
        hierarchy.ChildIconLeft.Should().BeApproximately(hierarchy.RootTextLeft, 0.1,
            "a child icon must start on the same column as its parent label");
        hierarchy.GrandchildIconLeft.Should().BeApproximately(hierarchy.ChildParentTextLeft, 0.1,
            "a grandchild icon must start on the same column as its parent label");
        (hierarchy.ChildTextLeft - hierarchy.ChildIconRight).Should().BeApproximately(12, 0.1,
            "Apple-style icon and label spacing should use one 12px rhythm");
        (hierarchy.ChildIconLeft - hierarchy.ActiveIndicatorLeft).Should().BeApproximately(12, 0.1,
            "the child rail must sit immediately before the child icon");

        var idleNestedBackgrounds = await Page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll(
                    '.vpp-sidebar:not(.sidebar-collapsed) .rz-navigation-item.ppjsidebarmenu.submenu > .rz-navigation-item-wrapper')]
                .filter(wrapper => !wrapper.classList.contains('rz-navigation-item-wrapper-active')
                    && !wrapper.querySelector(':scope > .rz-navigation-item-link.active'))
                .map(wrapper => getComputedStyle(wrapper).backgroundColor)
            """);
        idleNestedBackgrounds.Should().NotBeEmpty("the expanded menu should expose inactive child or grandchild rows");
        idleNestedBackgrounds.Should().OnlyContain(
            color => color == "rgba(0, 0, 0, 0)",
            "idle child and grandchild wrappers should reveal the white sidebar instead of the old gray surface");

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
            // Chờ nhóm dashboard báo aria-expanded=false, rồi indicator đứng yên
            // qua 2 khung rAF trước khi đọc top làm mốc.
            await Page.WaitForFunctionAsync(
                """
                () => document.querySelector(".vpp-sidebar-nav .rz-panel-menu > li[title='Bảng điều khiển']")?.getAttribute('aria-expanded') === 'false'
                """);
            await Page.WaitForFunctionAsync(
                """
                () => new Promise(resolve => {
                    const measure = () => document.querySelector('.vpp-sidebar-nav > .vpp-sidebar-shared-indicator')?.getBoundingClientRect().top ?? -1;
                    const first = measure();
                    requestAnimationFrame(() => requestAnimationFrame(() => resolve(first >= 0 && measure() === first)));
                })
                """);
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
            // Chờ đúng điều kiện được đọc bên dưới: indicator bỏ is-ready và opacity về 0.
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const el = document.querySelector('.vpp-sidebar-nav > .vpp-sidebar-shared-indicator');
                    return !!el && !el.classList.contains('is-ready')
                        && Number.parseFloat(getComputedStyle(el).opacity) === 0;
                }
                """);

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
            // Đối xứng: chờ indicator có lại is-ready và opacity về 1.
            await Page.WaitForFunctionAsync(
                """
                () => {
                    const el = document.querySelector('.vpp-sidebar-nav > .vpp-sidebar-shared-indicator');
                    return !!el && el.classList.contains('is-ready')
                        && Number.parseFloat(getComputedStyle(el).opacity) === 1;
                }
                """);

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

        // W-B.2b: the primary section tabs live in the shared shell header, not in
        // the page body; the sidebar also lists "Đơn hàng của tôi", so target the
        // active header tab by class instead of by text.
        var activeHeaderTab = Page.Locator(".vpp-layout-header .vpp-header-tabs .vpp-header-tab.is-active");
        await activeHeaderTab.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });

        var tabGeometry = await activeHeaderTab.EvaluateAsync<TabTitleGeometry>(
            """
            element => {
                const range = document.createRange();
                range.selectNodeContents(element);
                const title = range.getBoundingClientRect();
                const nav = element.closest('.vpp-layout-header').getBoundingClientRect();
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
        Math.Abs(tabGeometry.CenterDelta).Should().BeLessThanOrEqualTo(1,
            "the visible label must be centred in the full header within its hairline bottom border");
        tabGeometry.NavTop.Should().BeApproximately(0, 0.1, "the shell header row must start at the viewport top");
        tabGeometry.NavHeight.Should().BeApproximately(72, 0.1, "the header must match the collapsed sidebar width");
        tabGeometry.TitleCenter.Should().BeApproximately(36, 1, "the visible text must centre against the outer 72px header");
        tabGeometry.BrandCenter.Should().BeApproximately(tabGeometry.TitleCenter, 1,
            "GTAS VPP must share the same visual centre as every primary tab label");
        tabGeometry.LogoCenter.Should().BeApproximately(tabGeometry.TitleCenter, 1,
            "the brand mark must share the same visual centre as the header labels");

        // The active underline is a CSS ::after transition, not a Web Animations
        // API animation, so read its transition duration instead of getAnimations().
        var headerTabUnderlineDuration = await activeHeaderTab.EvaluateAsync<string>(
            "element => getComputedStyle(element, '::after').transitionDuration");
        headerTabUnderlineDuration.Should().Contain("0.2s",
            "the header underline must use the same motion duration as the PanelMenu expansion");

        var headerTabs = Page.Locator(".vpp-layout-header .vpp-header-tabs .vpp-header-tab");
        (await headerTabs.CountAsync()).Should().BeGreaterThan(1,
            "the dashboard area must expose more than one header tab");
        var secondHeaderTab = headerTabs.Nth(1);
        var secondHeaderTabPath = await secondHeaderTab.GetAttributeAsync("href");
        secondHeaderTabPath.Should().NotBeNullOrWhiteSpace("header tabs must navigate through real routes");
        await secondHeaderTab.ClickAsync();
        await Page.WaitForURLAsync(
            url => url.EndsWith(secondHeaderTabPath!, StringComparison.OrdinalIgnoreCase),
            new PageWaitForURLOptions { Timeout = 30_000 });
        await Page.WaitForFunctionAsync(
            """
            () => {
                const tabs = document.querySelectorAll('.vpp-header-tabs .vpp-header-tab');
                return tabs.length > 1
                    && tabs[1].classList.contains('is-active')
                    && tabs[1].getAttribute('aria-current') === 'page'
                    && tabs[0].getAttribute('aria-current') !== 'page';
            }
            """,
            null,
            new PageWaitForFunctionOptions { Timeout = 30_000 });

        // Điều kiện tab active đã được chờ ở trên; chỉ cần xả render qua double-rAF.
        await WaitForRenderSettleAsync();
        var shellSidebar = Page.Locator(".vpp-sidebar");
        if (!await shellSidebar.EvaluateAsync<bool>("element => element.classList.contains('sidebar-collapsed')"))
        {
            await Page.EvaluateAsync("() => document.querySelector('.vpp-sidebar-toggle').click()");
        }
        // Chờ transition thu gọn CHẠM TRẠNG THÁI CUỐI: chỉ kiểm "2 khung rAF bằng nhau"
        // là chưa đủ vì hai mẫu có thể cùng rơi vào thời điểm transition chưa khởi động
        // (đo ra hình học sidebar còn mở rộng). Gate thêm bề rộng rail đã về ~72px.
        await Page.WaitForFunctionAsync(
            """
            () => new Promise(resolve => {
                const sidebarNode = document.querySelector('.vpp-sidebar');
                if (!sidebarNode || !sidebarNode.classList.contains('sidebar-collapsed')) {
                    resolve(false);
                    return;
                }
                const measure = () => {
                    const width = sidebarNode.getBoundingClientRect().width;
                    const logo = document.querySelector('.vpp-sidebar-collapsed-logo');
                    if (!logo || logo.getClientRects().length === 0) {
                        return null;
                    }
                    return { width, left: logo.getBoundingClientRect().left };
                };
                const first = measure();
                requestAnimationFrame(() => requestAnimationFrame(() => {
                    const second = measure();
                    resolve(!!first && !!second
                        && second.width <= 80
                        && second.width === first.width
                        && second.left === first.left);
                }));
            })
            """);
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
        public double RootIconLeft { get; set; }
        public double RootTextLeft { get; set; }
        public double ChildIconLeft { get; set; }
        public double ChildIconRight { get; set; }
        public double ChildTextLeft { get; set; }
        public double ChildParentTextLeft { get; set; }
        public double GrandchildIconLeft { get; set; }
        public double ActiveIndicatorLeft { get; set; }
        public double LogoLeft { get; set; }
        public double AvatarLeft { get; set; }
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
