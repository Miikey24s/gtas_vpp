using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ResponsiveScrollOwnershipTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task WideDesktopStaysFixedWhileLaptopAndLongPagesUseTheCorrectScroller()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.Locator(".vpp-orders-workspace").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await EnsureSidebarExpandedAsync();

        var wideDesktop = await ReadShellGeometryAsync();
        wideDesktop.SidebarWidth.Should().BeApproximately(286, 1);
        wideDesktop.LayoutBodyScrollHeight.Should().BeLessThanOrEqualTo(wideDesktop.LayoutBodyClientHeight + 2);
        wideDesktop.DocumentHasHorizontalOverflow.Should().BeFalse();

        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}permission?tab=0", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var userSurface = Page.GetByTestId("permission-users-data-surface");
        await userSurface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await WaitForBusyStateAsync(userSurface.Locator("xpath=ancestor::*[@data-vpp-workspace-pattern='collection'][1]"));
        await EnsureSidebarExpandedAsync();

        var laptop = await Page.EvaluateAsync<LaptopGeometry>("""
            () => {
                const root = document.documentElement;
                const sidebar = document.querySelector('.vpp-sidebar');
                const workspace = document.querySelector('[data-vpp-workspace-pattern="collection"]');
                const shell = document.querySelector('.vpp-admin-tabs.vpp-bounded-shell');
                const panel = shell?.querySelector(':scope > .rz-tabview-panels > .rz-tabview-panel');
                const dataViewport = document.querySelector('[data-testid="permission-users-data-surface"] .rz-data-grid-data');
                return {
                    sidebarWidth: sidebar?.getBoundingClientRect().width ?? 0,
                    compactRowHeight: Number.parseFloat(getComputedStyle(root).getPropertyValue('--vpp-data-row-compact-height')),
                    workspaceScrollMode: workspace?.getAttribute('data-vpp-scroll-mode') ?? '',
                    panelOverflowY: panel ? getComputedStyle(panel).overflowY : '',
                    gridHasHorizontalOverflow: dataViewport instanceof HTMLElement
                        && dataViewport.scrollWidth > dataViewport.clientWidth + 1,
                    documentHasHorizontalOverflow: root.scrollWidth > root.clientWidth + 1
                };
            }
            """);

        laptop.SidebarWidth.Should().BeApproximately(248, 1);
        laptop.CompactRowHeight.Should().BeApproximately(38, 0.1);
        laptop.WorkspaceScrollMode.Should().Be("adaptive");
        laptop.PanelOverflowY.Should().Be("visible");
        laptop.GridHasHorizontalOverflow.Should().BeTrue("schema rộng phải cuộn trong grid, không kéo ngang toàn trang");
        laptop.DocumentHasHorizontalOverflow.Should().BeFalse();
        await CaptureEvidenceAsync("responsive-permission-users-1366x768.png");

        await Page.SetViewportSizeAsync(1280, 720);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=periods", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var periodWorkspace = Page.Locator(".vpp-order-period-workspace");
        await periodWorkspace.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await WaitForBusyStateAsync(periodWorkspace);

        var periodScroll = await ReadPageScrollGeometryAsync(".vpp-order-period-workspace");
        periodScroll.ScrollMode.Should().Be("page");
        periodScroll.OverflowY.Should().Be("auto");
        (await Page.GetByTestId("post-settlement-corrections").CountAsync())
            .Should().Be(0, "hàng chờ sửa/hủy sau chốt thuộc màn Chốt kỳ, không thuộc danh mục kỳ đặt hàng");
        (await IsVisibleInsideLayoutBodyAsync("[data-testid='order-period-management-table']"))
            .Should().BeTrue();
        await CaptureEvidenceAsync("responsive-period-page-top-1280x720.png");

        await Page.GotoAsync($"{BaseUrl}dashboard/order-create", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var orderBuilder = Page.Locator(".vpp-order-builder");
        await orderBuilder.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        var orderCreateScroll = await ReadPageScrollGeometryAsync(".vpp-order-create-page");
        orderCreateScroll.CanScroll.Should().BeTrue("wizard responsive phải mở page scroll khi workspace không còn đủ chiều cao");
        orderCreateScroll.OverflowY.Should().Be("auto");
        (await Page.Locator(".vpp-wizard-stage").EvaluateAsync<string>("element => getComputedStyle(element).overflow"))
            .Should().Be("visible");
        await ScrollLayoutBodyToBottomAsync();
        (await IsVisibleInsideLayoutBodyAsync(".vpp-order-draft-actions"))
            .Should().BeTrue("pane và action cuối wizard phải truy cập được trên laptop");
        await CaptureEvidenceAsync("responsive-order-create-bottom-1280x720.png");

        await Page.SetViewportSizeAsync(1366, 768);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=1", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var history = Page.Locator(".vpp-history-page");
        await history.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await WaitForBusyStateAsync(history);
        var historySkeleton = history.Locator(".vpp-history-loading-state");
        if (await historySkeleton.CountAsync() > 0)
        {
            await historySkeleton.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 60_000 });
        }
        var historyMode = await history.Locator("[data-vpp-workspace-pattern='analytics']")
            .GetAttributeAsync("data-vpp-scroll-mode");
        historyMode.Should().Be("internal");
        var laptopHistory = await Page.EvaluateAsync<LaptopHistoryGeometry>("""
            () => {
                const page = document.querySelector('.vpp-history-page');
                const orders = document.querySelector('.vpp-history-orders-card');
                const drawer = document.querySelector('.vpp-history-drawer');
                if (!(page instanceof HTMLElement)
                    || !(orders instanceof HTMLElement)
                    || !(drawer instanceof HTMLElement)) {
                    return { widthRatio: 0, drawerVisible: true };
                }

                return {
                    widthRatio: orders.getBoundingClientRect().width / page.getBoundingClientRect().width,
                    drawerVisible: drawer.getClientRects().length > 0
                };
            }
            """);
        laptopHistory.WidthRatio.Should().BeGreaterThan(0.95,
            "laptop phải dành toàn chiều ngang cho bảng lịch sử thay vì ép bảng 9 cột vào nửa màn hình");
        laptopHistory.DrawerVisible.Should().BeFalse("chi tiết chỉ mở khi người dùng chọn một đơn trên laptop");
        await CaptureEvidenceAsync("responsive-history-list-1366x768.png");

        await history.Locator(".vpp-history-grid tbody tr").First.ClickAsync();
        var historyDrawer = history.Locator(".vpp-history-drawer.is-open");
        await historyDrawer.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        (await historyDrawer.Locator(".vpp-history-drawer-close").IsVisibleAsync())
            .Should().BeTrue("drawer trên laptop phải có thao tác đóng rõ ràng");
        var historyShell = await ReadShellGeometryAsync();
        historyShell.LayoutBodyScrollHeight.Should().BeLessThanOrEqualTo(historyShell.LayoutBodyClientHeight + 2,
            "History vẫn giữ một viewport và giao scroll cho list/detail region");
        await CaptureEvidenceAsync("responsive-history-drawer-1366x768.png");
    }

    private Task<ShellGeometry> ReadShellGeometryAsync() => Page.EvaluateAsync<ShellGeometry>("""
        () => {
            const root = document.documentElement;
            const body = document.querySelector('.vpp-layout-body');
            const sidebar = document.querySelector('.vpp-sidebar');
            return {
                sidebarWidth: sidebar?.getBoundingClientRect().width ?? 0,
                layoutBodyScrollHeight: body?.scrollHeight ?? 0,
                layoutBodyClientHeight: body?.clientHeight ?? 0,
                documentHasHorizontalOverflow: root.scrollWidth > root.clientWidth + 1
            };
        }
        """);

    private Task<PageScrollGeometry> ReadPageScrollGeometryAsync(string selector) =>
        Page.EvaluateAsync<PageScrollGeometry>("""
            selector => {
                const body = document.querySelector('.vpp-layout-body');
                const workspace = document.querySelector(selector);
                return {
                    canScroll: body instanceof HTMLElement && body.scrollHeight > body.clientHeight + 2,
                    overflowY: body ? getComputedStyle(body).overflowY : '',
                    scrollMode: workspace?.getAttribute('data-vpp-scroll-mode') ?? ''
                };
            }
            """, selector);

    private Task ScrollLayoutBodyToBottomAsync() => Page.Locator(".vpp-layout-body").EvaluateAsync(
        "element => element.scrollTo({ top: element.scrollHeight, behavior: 'instant' })");

    private Task<bool> IsVisibleInsideLayoutBodyAsync(string selector) => Page.EvaluateAsync<bool>("""
        selector => {
            const body = document.querySelector('.vpp-layout-body');
            const target = document.querySelector(selector);
            if (!(body instanceof HTMLElement) || !(target instanceof HTMLElement)) return false;
            const bodyRect = body.getBoundingClientRect();
            const targetRect = target.getBoundingClientRect();
            return targetRect.bottom <= bodyRect.bottom + 2 && targetRect.bottom > bodyRect.top;
        }
        """, selector);

    private async Task WaitForBusyStateAsync(ILocator locator)
    {
        await locator.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            "element => element.getAttribute('aria-busy') !== 'true'",
            await locator.ElementHandleAsync(),
            new() { Timeout = 60_000 });
    }

    private async Task EnsureSidebarExpandedAsync()
    {
        var collapsed = Page.Locator(".vpp-sidebar.sidebar-collapsed");
        if (await collapsed.CountAsync() > 0)
        {
            await Page.Locator(".vpp-sidebar-collapsed-brand").EvaluateAsync("element => element.click()");
        }

        await Page.WaitForFunctionAsync("""
            () => {
                const sidebar = document.querySelector('.vpp-sidebar');
                const expected = Number.parseFloat(
                    getComputedStyle(document.documentElement).getPropertyValue('--vpp-sidebar-width'));
                return sidebar && !sidebar.classList.contains('sidebar-collapsed')
                    && Math.abs(sidebar.getBoundingClientRect().width - expected) <= 1;
            }
            """);
    }

    private async Task CaptureEvidenceAsync(string fileName)
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

    private sealed class ShellGeometry
    {
        public double SidebarWidth { get; init; }
        public double LayoutBodyScrollHeight { get; init; }
        public double LayoutBodyClientHeight { get; init; }
        public bool DocumentHasHorizontalOverflow { get; init; }
    }

    private sealed class LaptopGeometry
    {
        public double SidebarWidth { get; init; }
        public double CompactRowHeight { get; init; }
        public string WorkspaceScrollMode { get; init; } = string.Empty;
        public string PanelOverflowY { get; init; } = string.Empty;
        public bool GridHasHorizontalOverflow { get; init; }
        public bool DocumentHasHorizontalOverflow { get; init; }
    }

    private sealed class LaptopHistoryGeometry
    {
        public double WidthRatio { get; init; }
        public bool DrawerVisible { get; init; }
    }

    private sealed class PageScrollGeometry
    {
        public bool CanScroll { get; init; }
        public string OverflowY { get; init; } = string.Empty;
        public string ScrollMode { get; init; } = string.Empty;
    }
}
