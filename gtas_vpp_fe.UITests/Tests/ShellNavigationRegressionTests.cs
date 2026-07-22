using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class ShellNavigationRegressionTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task ActiveChildIndicator_HidesAfterItsParentCollapses_AndTabsUseTrimmedFontMetrics()
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
                    titleCenter: title.top + title.height / 2
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
    }
}
