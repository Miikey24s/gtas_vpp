using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ShellNavigationRegressionTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task CompactPrimaryTabs_KeepReadableHoverTextAndHeaderDoesNotRepeatRole()
    {
        await Page.SetViewportSizeAsync(700, 900);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        (await Page.Locator(".vpp-header-role-badge").CountAsync())
            .Should().Be(0, "the permission group belongs to the sidebar identity, not the shared header");

        var primaryTabs = Page.Locator("""
            .vpp-admin-tabs > .rz-tabview-nav-container > .rz-tabview-nav > li > button[role='tab'],
            .vpp-admin-tabs > .rz-tabview-nav > li > button[role='tab']
            """);
        await primaryTabs.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var activeTab = Page.Locator("""
            .vpp-admin-tabs > .rz-tabview-nav-container > .rz-tabview-nav > li > button[role='tab'][aria-selected='true'],
            .vpp-admin-tabs > .rz-tabview-nav > li > button[role='tab'][aria-selected='true']
            """).First;
        var inactiveTab = Page.Locator("""
            .vpp-admin-tabs > .rz-tabview-nav-container > .rz-tabview-nav > li > button[role='tab'][aria-selected='false'],
            .vpp-admin-tabs > .rz-tabview-nav > li > button[role='tab'][aria-selected='false']
            """).First;

        await activeTab.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await inactiveTab.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await inactiveTab.HoverAsync();
        await Page.WaitForFunctionAsync("""
            () => {
                const tabs = [...document.querySelectorAll('.vpp-admin-tabs > .rz-tabview-nav-container > .rz-tabview-nav > li > button[role="tab"], .vpp-admin-tabs > .rz-tabview-nav > li > button[role="tab"]')];
                const active = tabs.find(tab => tab.getAttribute('aria-selected') === 'true');
                const hovered = tabs.find(tab => tab.matches(':hover') && tab.getAttribute('aria-selected') !== 'true');
                const textColor = tab => getComputedStyle(tab.querySelector('.rz-tabview-title') ?? tab).color;
                return active && hovered && textColor(active) === textColor(hovered);
            }
            """);

        var colors = await Page.EvaluateAsync<string[]>("""
            () => {
                const tabs = [...document.querySelectorAll('.vpp-admin-tabs > .rz-tabview-nav-container > .rz-tabview-nav > li > button[role="tab"], .vpp-admin-tabs > .rz-tabview-nav > li > button[role="tab"]')];
                const active = tabs.find(tab => tab.getAttribute('aria-selected') === 'true');
                const hovered = tabs.find(tab => tab.matches(':hover') && tab.getAttribute('aria-selected') !== 'true');
                const textColor = tab => tab ? getComputedStyle(tab.querySelector('.rz-tabview-title') ?? tab).color : '';
                return [textColor(active), textColor(hovered)];
            }
            """);

        colors[0].Should().NotBeNullOrWhiteSpace();
        colors[1].Should().Be(colors[0], "hovered inactive tabs must use the same readable primary text as the selected tab");
        colors[1].Should().NotBe("rgb(255, 255, 255)", "light-theme hover text must not disappear on the pale surface");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "header-compact-tabs-700x900.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }
    }

    [Fact]
    public async Task DesktopPrimaryHeaderTabs_KeepReadableInactiveText()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.WaitForFunctionAsync(
            "() => !document.documentElement.classList.contains('vpp-page-entering')");

        var headerTabs = Page.Locator(".vpp-layout-header .vpp-header-tabs .vpp-header-tab");
        await headerTabs.First.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        (await headerTabs.CountAsync()).Should().BeGreaterThan(1,
            "the dashboard area must expose both selected and unselected desktop header tabs");

        var headerTabColors = await Page.EvaluateAsync<string[]>(
            """
            () => {
                const resolveColor = token => {
                    const probe = document.createElement('span');
                    probe.style.color = `var(${token})`;
                    document.body.appendChild(probe);
                    const color = getComputedStyle(probe).color;
                    probe.remove();
                    return color;
                };
                const active = document.querySelector('.vpp-layout-header .vpp-header-tab.is-active');
                const inactive = document.querySelector('.vpp-layout-header .vpp-header-tab:not(.is-active)');
                return [
                    getComputedStyle(active).color,
                    getComputedStyle(inactive).color,
                    resolveColor('--vpp-text-primary'),
                    resolveColor('--vpp-text-secondary')
                ];
            }
            """);

        headerTabColors[0].Should().Be(headerTabColors[2],
            "the selected desktop header tab must use the primary text token");
        headerTabColors[1].Should().Be(headerTabColors[3],
            "unselected desktop header tabs must use the readable secondary text token");
        headerTabColors[1].Should().NotBe("rgb(255, 255, 255)",
            "unselected desktop header text must not disappear on the light navigation surface");

        var inactiveHeaderTab = Page.Locator(
            ".vpp-layout-header .vpp-header-tabs .vpp-header-tab:not(.is-active)").First;
        await inactiveHeaderTab.HoverAsync();
        await Page.WaitForFunctionAsync(
            """
            element => {
                if (!element.matches(':hover')) return false;
                const probe = document.createElement('span');
                probe.style.backgroundColor = 'var(--vpp-navigation-item-hover-bg)';
                document.body.appendChild(probe);
                const expected = getComputedStyle(probe).backgroundColor;
                probe.remove();
                return getComputedStyle(element, '::before').backgroundColor === expected;
            }
            """,
            await inactiveHeaderTab.ElementHandleAsync(),
            new() { Timeout = 5_000 });
        var hoverSurface = await inactiveHeaderTab.EvaluateAsync<string[]>(
            """
            element => {
                const probe = document.createElement('span');
                probe.style.position = 'absolute';
                probe.style.backgroundColor = 'var(--vpp-navigation-item-hover-bg)';
                probe.style.borderRadius = 'var(--vpp-radius-md)';
                probe.style.top = 'var(--vpp-navigation-surface-cross-inset)';
                document.body.appendChild(probe);
                const expected = getComputedStyle(probe);
                const surface = getComputedStyle(element, '::before');
                const values = [
                    surface.backgroundColor,
                    expected.backgroundColor,
                    surface.borderRadius,
                    expected.borderRadius,
                    surface.top,
                    expected.top,
                    surface.bottom,
                    element.matches(':hover').toString()
                ];
                probe.remove();
                return values;
            }
            """);
        hoverSurface[7].Should().Be("true",
            "the browser must keep the inactive header tab hovered while its surface is measured");
        hoverSurface[0].Should().Be(hoverSurface[1],
            "desktop header hover must use the shared navigation surface token");
        hoverSurface[2].Should().Be(hoverSurface[3],
            "desktop header hover must use the shared medium radius token");
        hoverSurface[4].Should().Be(hoverSurface[5],
            "the hover surface must stay inset from the top of the header");
        hoverSurface[6].Should().Be(hoverSurface[5],
            "the hover surface must stay equally inset from the bottom of the header");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "desktop-header-readable-tabs-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }
    }

    [Fact]
    public async Task UserMenu_OpensWithoutAnchorDriftOrWidthPulse()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync();
        await Page.WaitForFunctionAsync("""
            () => ['.vpp-layout', '.vpp-sidebar']
                .map(selector => document.querySelector(selector))
                .filter(Boolean)
                .flatMap(element => element.getAnimations({ subtree: true }))
                .every(animation => animation.playState !== 'running' && animation.playState !== 'pending')
            """);

        var sidebar = Page.Locator(".vpp-sidebar");
        if (await sidebar.EvaluateAsync<bool>("element => element.classList.contains('sidebar-collapsed')"))
        {
            await sidebar.Locator(".vpp-sidebar-collapsed-brand").ClickAsync();
            await Page.WaitForFunctionAsync(
                "() => !document.querySelector('.vpp-sidebar')?.classList.contains('sidebar-collapsed')");
        }

        var trigger = Page.Locator(".vpp-sidebar-user-menu .user-menu-trigger");
        await trigger.ClickAsync();
        var menu = Page.Locator(".vpp-sidebar-user-menu .user-dropdown");
        await menu.WaitForAsync(new() { State = WaitForSelectorState.Visible });

        var geometryAffectingKeyframes = await menu.EvaluateAsync<string[]>("""
            element => (element.getAnimations()[0]?.effect.getKeyframes() ?? [])
                .flatMap(frame => ['transform', 'translate', 'scale']
                    .filter(property => frame[property] && frame[property] !== 'none')
                    .map(property => `${property}:${frame[property]}`))
            """);
        geometryAffectingKeyframes.Should().BeEmpty(
            "the account menu and anchored filter surfaces must share one non-shifting reveal contract");

        var frames = await menu.EvaluateAsync<double[][]>("""
            element => new Promise(resolve => {
                const frames = [];
                const sample = () => {
                    const rect = element.getBoundingClientRect();
                    frames.push([rect.left, rect.top, rect.width, rect.height]);
                    if (frames.length < 10) {
                        requestAnimationFrame(sample);
                        return;
                    }
                    resolve(frames);
                };
                requestAnimationFrame(sample);
            })
            """);
        (frames.Max(frame => frame[0]) - frames.Min(frame => frame[0])).Should().BeLessThan(0.75,
            "the user menu left edge must remain anchored while revealing");
        (frames.Max(frame => frame[2]) - frames.Min(frame => frame[2])).Should().BeLessThan(0.75,
            "the user menu width must remain visually stable while revealing");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "f4-user-menu-motion-settled.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Allow,
                Caret = ScreenshotCaret.Hide
            });
        }

        await Page.Locator(".user-dropdown-backdrop").ClickAsync(new() { Force = true });
        await menu.WaitForAsync(new() { State = WaitForSelectorState.Detached });

        if (!await sidebar.EvaluateAsync<bool>("element => element.classList.contains('sidebar-collapsed')"))
        {
            await Page.Locator(".vpp-sidebar-toggle").ClickAsync();
        }
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.vpp-sidebar')?.classList.contains('sidebar-collapsed')");

        await trigger.ClickAsync();
        await menu.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        var collapsedGeometry = await Page.EvaluateAsync<CollapsedUserMenuGeometry>(
            """
            () => {
                const sidebar = document.querySelector('.vpp-sidebar').getBoundingClientRect();
                const menu = document.querySelector('.vpp-sidebar-user-menu .user-dropdown').getBoundingClientRect();
                return {
                    sidebarRight: sidebar.right,
                    menuLeft: menu.left,
                    menuRight: menu.right,
                    menuBottom: menu.bottom,
                    viewportWidth: window.innerWidth,
                    viewportHeight: window.innerHeight
                };
            }
            """);
        collapsedGeometry.MenuLeft.Should().BeGreaterThan(collapsedGeometry.SidebarRight,
            "the expanded account surface must open beside the collapsed rail instead of covering it");
        (collapsedGeometry.MenuLeft - collapsedGeometry.SidebarRight).Should().BeInRange(6.5, 8.5,
            "the rail edge can include Radzen's one-pixel runtime box, but the visual gap must stay one compact token");
        (collapsedGeometry.ViewportHeight - collapsedGeometry.MenuBottom).Should().BeInRange(6.5, 8.5);
        collapsedGeometry.MenuRight.Should().BeLessThanOrEqualTo(collapsedGeometry.ViewportWidth - 8);

        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "f4-user-menu-collapsed-settled.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

        await Page.Locator(".user-dropdown-backdrop").ClickAsync(new() { Force = true });
        await menu.WaitForAsync(new() { State = WaitForSelectorState.Detached });
    }

    private sealed class CollapsedUserMenuGeometry
    {
        public double SidebarRight { get; set; }
        public double MenuLeft { get; set; }
        public double MenuRight { get; set; }
        public double MenuBottom { get; set; }
        public double ViewportWidth { get; set; }
        public double ViewportHeight { get; set; }
    }

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

        var headerTabs = Page.Locator(".vpp-layout-header .vpp-header-tabs .vpp-header-tab");
        (await headerTabs.CountAsync()).Should().BeGreaterThan(1,
            "the dashboard area must expose more than one header tab");
        var headerIndicator = Page.Locator(".vpp-layout-header .vpp-header-tabs > .vpp-tab-shared-indicator");
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.vpp-header-tabs > .vpp-tab-shared-indicator')?.classList.contains('is-ready') === true");
        (await headerIndicator.EvaluateAsync<string>("element => getComputedStyle(element).height"))
            .Should().Be("3px", "the horizontal header line must reuse the shared navigation indicator");

        var secondHeaderTab = headerTabs.Nth(1);
        var secondHeaderTabPath = await secondHeaderTab.GetAttributeAsync("href");
        secondHeaderTabPath.Should().NotBeNullOrWhiteSpace("header tabs must navigate through real routes");

        // Giữ route hiện tại trong một click để quan sát một indicator thật trượt
        // ngang; sau đó click lần hai để kiểm tra navigation như bình thường.
        await secondHeaderTab.EvaluateAsync("element => element.addEventListener('click', event => event.preventDefault(), { once: true })");
        var headerIndicatorSamples = await SampleHorizontalIndicatorMotionAsync(headerIndicator, secondHeaderTab);
        AssertSmoothHorizontalMotion(headerIndicatorSamples,
            "the header line must travel directly between adjacent primary tabs");
        var headerIndicatorDuration = await headerIndicator.EvaluateAsync<double>(
            "element => element.getAnimations()[0]?.effect.getTiming().duration ?? 0");
        headerIndicatorDuration.Should().Be(200,
            "the header line must use the same 200ms motion token as sidebar expansion/indicator");
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('.vpp-header-tabs > .vpp-tab-shared-indicator')?.getAnimations().length === 0");

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
        // Chờ rail thu gọn ĐẠT ĐÚNG BẤT BIẾN sẽ được assert bên dưới (logo/icon/avatar
        // thẳng hàng trục X): bề rộng về 72px xong, brand định vị tuyệt đối vẫn còn một
        // nhịp recalc containing-block — chỉ chính phép đo cuối mới là tín hiệu đáng tin.
        // Nếu rail thật sự lệch, wait này timeout và test fail chính đáng.
        await Page.WaitForFunctionAsync(
            """
            () => {
                const sidebarNode = document.querySelector('.vpp-sidebar');
                if (!sidebarNode || !sidebarNode.classList.contains('sidebar-collapsed')) {
                    return false;
                }
                const centreX = element => {
                    const rect = element.getBoundingClientRect();
                    return rect.left + rect.width / 2;
                };
                const logo = document.querySelector('.vpp-sidebar-collapsed-logo');
                const icon = document.querySelector('.vpp-sidebar-nav .rz-panel-menu > .rz-navigation-item .rz-navigation-item-icon');
                const avatar = document.querySelector('.vpp-sidebar-user-menu .user-avatar');
                if (!logo || !icon || !avatar || logo.getClientRects().length === 0) {
                    return false;
                }
                return sidebarNode.getBoundingClientRect().width <= 80
                    && Math.abs(centreX(logo) - centreX(icon)) <= 0.1
                    && Math.abs(centreX(avatar) - centreX(icon)) <= 0.1;
            }
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

    private async Task<List<double>> SampleHorizontalIndicatorMotionAsync(ILocator indicator, ILocator target)
    {
        var samplingTask = indicator.EvaluateAsync<double[]>(
            """
            element => new Promise(resolve => {
                const samples = [];
                const sampleFrame = () => {
                    samples.push(element.getBoundingClientRect().left);
                    if (samples.length < 8) {
                        requestAnimationFrame(sampleFrame);
                        return;
                    }
                    resolve(samples);
                };
                requestAnimationFrame(sampleFrame);
            })
            """);
        await target.ClickAsync();
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

    private static void AssertSmoothHorizontalMotion(IReadOnlyList<double> samples, string because)
    {
        var deltas = samples.Zip(samples.Skip(1), (current, next) => next - current).ToArray();
        var directedMoves = deltas.Count(delta => delta > 0.2);
        var totalDistance = samples[^1] - samples[0];
        var largestStep = deltas.Select(Math.Abs).DefaultIfEmpty(0).Max();

        directedMoves.Should().BeGreaterThanOrEqualTo(3, $"{because} must produce multiple visible frames");
        totalDistance.Should().BeGreaterThan(12, $"{because} must move the line a meaningful distance during the sampled animation window");
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
