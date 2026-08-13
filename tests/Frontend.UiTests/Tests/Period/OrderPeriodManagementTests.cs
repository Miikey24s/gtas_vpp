using System.Globalization;
using System.Diagnostics;
using Deque.AxeCore.Playwright;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Period;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class OrderPeriodManagementTests : TestBase, IAuthenticatedUiTest
{
    private const string ScreenshotDirectoryEnvironmentVariable = "GTAS_ORDER_PERIOD_SCREENSHOT_DIR";
    private readonly ITestOutputHelper output;

    public OrderPeriodManagementTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData(390, 844)]
    [InlineData(768, 1024)]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public async Task OrderPeriodManagement_ShowsRollingSettingsAcrossResponsiveViewports(
        int width,
        int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=periods");
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        var periodSurface = Page.Locator("[data-testid='order-period-management-table']:visible");
        await Assertions.Expect(Page.Locator(".vpp-header-sub-tab[aria-current='page']").First)
            .ToHaveTextAsync("Các kỳ đặt hàng");
        await Assertions.Expect(periodSurface).ToBeVisibleAsync();
        Assert.Equal("Các kỳ đặt hàng", await periodSurface.GetAttributeAsync("aria-label"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mở đủ số kỳ", Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mở một kỳ rời rạc", Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mở cấu hình đặt hàng", Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByText("Cấu hình đang áp dụng", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(periodSurface.GetByRole(AriaRole.Button, new() { Name = "Xem chi tiết", Exact = true }).First)
            .ToBeVisibleAsync();
        var periodRows = periodSurface.Locator("tbody");
        await Assertions.Expect(periodRows.GetByText("Đang mở", new() { Exact = true }).First).ToBeVisibleAsync();
        await Assertions.Expect(periodRows.GetByText("Đang chốt", new() { Exact = true }).First).ToBeVisibleAsync();
        await Assertions.Expect(periodSurface.GetByText("Đang nhận đơn", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(periodSurface.GetByText("Đang chốt kỳ", new() { Exact = true })).ToHaveCountAsync(0);
        await Assertions.Expect(periodSurface.GetByText("Đã chốt kỳ", new() { Exact = true })).ToHaveCountAsync(0);
        if (width == 1366)
        {
            var statusFilter = periodSurface.GetByRole(
                AriaRole.Button,
                new() { Name = "Lọc theo trạng thái kỳ", Exact = true });
            await statusFilter.ClickAsync();
            await Page.GetByRole(AriaRole.Option, new() { Name = "Đang mở", Exact = true }).ClickAsync();
            var filteredRows = periodSurface.Locator("tbody tr");
            (await filteredRows.CountAsync()).Should().BeGreaterThan(0);
            await Assertions.Expect(filteredRows.GetByText("Đang mở", new() { Exact = true }))
                .ToHaveCountAsync(await filteredRows.CountAsync());
            await periodSurface.GetByRole(
                    AriaRole.Button,
                    new() { Name = "Xóa bộ lọc", Exact = true })
                .ClickAsync();

            var dataRows = periodSurface.Locator("tbody tr");
            var actionMenus = periodSurface.GetByTestId("period-row-more-actions");
            (await actionMenus.CountAsync()).Should().Be(await dataRows.CountAsync(),
                "mọi kỳ phải luôn có menu Xem chi tiết");

            var settledRows = dataRows.Filter(new LocatorFilterOptions
            {
                Has = periodSurface.GetByText("Đã chốt", new() { Exact = true })
            });
            if (await settledRows.CountAsync() > 0)
            {
                var settledRow = settledRows.First;
                await Assertions.Expect(settledRow.GetByRole(
                        AriaRole.Button,
                        new() { Name = "Xem chi tiết", Exact = true }))
                    .ToBeEnabledAsync();
                await Assertions.Expect(settledRow.GetByTestId("period-row-more-actions"))
                    .ToBeVisibleAsync();
            }

            var openRowWithOrders = dataRows.Filter(new LocatorFilterOptions
            {
                HasTextString = "Đang mở"
            }).Last;
            await openRowWithOrders.GetByTestId("period-row-more-actions").ClickAsync();
            var capabilityMenu = Page.Locator(".rz-context-menu:visible");
            await Assertions.Expect(capabilityMenu).ToBeVisibleAsync();
            await Assertions.Expect(capabilityMenu.GetByText("Chốt kỳ", new() { Exact = true })
                    .Locator("xpath=ancestor::li[1]"))
                .ToHaveAttributeAsync("aria-disabled", "true");
            await Assertions.Expect(capabilityMenu.GetByText("Gia hạn kỳ", new() { Exact = true })
                    .Locator("xpath=ancestor::li[1]"))
                .Not.ToHaveAttributeAsync("aria-disabled", "true");
            await Assertions.Expect(capabilityMenu.GetByText("Sửa lịch", new() { Exact = true })
                    .Locator("xpath=ancestor::li[1]"))
                .ToHaveAttributeAsync("aria-disabled", "true");
            await openRowWithOrders.GetByTestId("period-row-more-actions").ClickAsync();
            await Assertions.Expect(capabilityMenu).ToBeHiddenAsync();
        }
        var headerColorContract = await periodSurface.Locator("thead th").First.EvaluateAsync<string>("""
            element => {
                const probe = document.createElement('span');
                probe.style.background = 'var(--vpp-surface-grid-header)';
                document.body.appendChild(probe);
                const tokenColor = getComputedStyle(probe).backgroundColor;
                probe.remove();
                return `${getComputedStyle(element).backgroundColor}|${tokenColor}`;
            }
            """);
        var headerColors = headerColorContract.Split('|');
        headerColors.Should().HaveCount(2);
        headerColors[0].Should().Be(headerColors[1], "grid quản trị phải dùng màu header canonical");
        var pageSizeSelect = periodSurface.Locator(".rz-paginator .rz-dropdown, .rz-pager .rz-dropdown").First;
        await Assertions.Expect(pageSizeSelect).ToBeVisibleAsync();
        var pageSizeGeometry = await pageSizeSelect.EvaluateAsync<string>("""
            element => {
                const rect = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                const compact = Math.abs(rect.width - 56) <= 1
                    && Math.abs(rect.height - 32) <= 1
                    && style.outlineStyle === 'none';
                return `${compact}|${rect.width}|${rect.height}|${style.outlineStyle}|${style.boxShadow}`;
            }
            """);
        pageSizeGeometry.Should().StartWith("true", "page-size trigger dùng chrome compact riêng của pager");
        await pageSizeSelect.ClickAsync();
        var pageSizePanel = Page.Locator(".rz-dropdown-panel.vpp-page-size-panel:visible").Last;
        await Assertions.Expect(pageSizePanel).ToBeVisibleAsync();
        var pageSizePanelGeometry = await pageSizePanel.EvaluateAsync<string>("""
            element => {
                const rect = element.getBoundingClientRect();
                const options = [...element.querySelectorAll('.rz-dropdown-item, .rz-dropdown-items > li')];
                const centered = options.every(option => getComputedStyle(option).justifyContent === 'center');
                const animation = getComputedStyle(element).animationName;
                return `${Math.abs(rect.width - 56) <= 1 && centered}|${rect.width}|${centered}|${animation}`;
            }
            """);
        pageSizePanelGeometry.Should().StartWith("true", "dropup page-size dùng cùng nhịp compact với trigger");
        pageSizePanelGeometry.Should().Contain("vpp-transient-enter-", "page-size dùng motion transient canonical");
        await Assertions.Expect(pageSizePanel).ToHaveAttributeAsync("data-vpp-dropdown-positioned", "true");
        var pageSizeTriggerBox = await pageSizeSelect.BoundingBoxAsync();
        var pageSizePanelBox = await pageSizePanel.BoundingBoxAsync();
        Assert.NotNull(pageSizeTriggerBox);
        Assert.NotNull(pageSizePanelBox);
        Assert.False(RectanglesOverlap(pageSizeTriggerBox, pageSizePanelBox),
            $"Page-size popup đang chồng trigger tại viewport {width}x{height}.");
        var pageSizeGap = pageSizePanelBox.Y + pageSizePanelBox.Height <= pageSizeTriggerBox.Y
            ? pageSizeTriggerBox.Y - (pageSizePanelBox.Y + pageSizePanelBox.Height)
            : pageSizePanelBox.Y - (pageSizeTriggerBox.Y + pageSizeTriggerBox.Height);
        Assert.InRange(pageSizeGap, 3, 6);
        await Page.WaitForTimeoutAsync(80);
        var settledPageSizePanelBox = await pageSizePanel.BoundingBoxAsync();
        Assert.NotNull(settledPageSizePanelBox);
        AssertStablePopupGeometry(pageSizePanelBox, settledPageSizePanelBox, "page-size");
        var pageSizeScreenshotDirectory = Environment.GetEnvironmentVariable(
            ScreenshotDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(pageSizeScreenshotDirectory))
        {
            var directory = Path.GetFullPath(pageSizeScreenshotDirectory);
            Directory.CreateDirectory(directory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, $"order-period-page-size-{width}x{height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await pageSizeSelect.ClickAsync();
        await Assertions.Expect(pageSizePanel).ToHaveClassAsync(
            new System.Text.RegularExpressions.Regex(@"\brz-close\b"));
        await Page.WaitForTimeoutAsync(250);
        for (var toggleIndex = 0; toggleIndex < 4; toggleIndex++)
        {
            await pageSizeSelect.ClickAsync();
            pageSizePanel = Page.Locator(".rz-dropdown-panel.vpp-page-size-panel:visible").Last;
            await Assertions.Expect(pageSizePanel).ToBeVisibleAsync();
            await Assertions.Expect(pageSizePanel).ToHaveAttributeAsync("data-vpp-dropdown-positioned", "true");
            await pageSizeSelect.ClickAsync();
            await Assertions.Expect(pageSizePanel).ToHaveClassAsync(
                new System.Text.RegularExpressions.Regex(@"\brz-close\b"));
            await Page.WaitForTimeoutAsync(250);
        }
        var periodMoreActions = periodSurface.GetByTestId("period-row-more-actions").First;
        await Assertions.Expect(periodMoreActions).ToBeVisibleAsync();
        await periodMoreActions.ClickAsync();
        var periodMenu = Page.Locator(".rz-context-menu:visible");
        await Assertions.Expect(periodMenu).ToBeVisibleAsync();
        await periodMoreActions.ClickAsync();
        await Page.WaitForTimeoutAsync(350);
        await Assertions.Expect(periodMenu).ToBeHiddenAsync();
        await periodMoreActions.ClickAsync();
        periodMenu = Page.Locator(".rz-context-menu:visible");
        await Assertions.Expect(periodMenu).ToBeVisibleAsync();
        await Assertions.Expect(periodMenu).ToHaveAttributeAsync(
            "data-vpp-admin-action-menu-positioned",
            "true");
        var actionMenuAnimation = await periodMenu.EvaluateAsync<string>(
            "element => getComputedStyle(element).animationName");
        actionMenuAnimation.Should().Contain("vpp-transient-enter-",
            "overflow menu dùng motion transient canonical");
        var settleAction = periodMenu.GetByText("Chốt kỳ", new() { Exact = true });
        var extendAction = periodMenu.GetByText("Gia hạn kỳ", new() { Exact = true });
        var editAction = periodMenu.GetByText("Sửa lịch", new() { Exact = true });
        await Assertions.Expect(settleAction).ToBeVisibleAsync();
        await Assertions.Expect(extendAction).ToBeVisibleAsync();
        await Assertions.Expect(editAction).ToBeVisibleAsync();
        await Assertions.Expect(periodMenu.GetByText("Xem chi tiết", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(periodMenu.GetByText("Đóng nhận đơn sớm", new() { Exact = true }))
            .ToHaveCountAsync(0);
        var menuItem = settleAction.Locator("xpath=ancestor::li[1]");
        await Assertions.Expect(menuItem).ToBeVisibleAsync();
        var initialMenuItemBackground = await menuItem.EvaluateAsync<string>(
            "element => getComputedStyle(element).backgroundColor");
        initialMenuItemBackground.Should().Be("rgba(0, 0, 0, 0)",
            "menu lệnh không tự bôi xanh mục đầu tiên như một lựa chọn đã chọn");
        var editMenuItem = editAction.Locator("xpath=ancestor::li[1]");
        await editMenuItem.HoverAsync();
        var menuHtml = await periodMenu.EvaluateAsync<string>("element => element.outerHTML");
        var innerMenuChrome = await periodMenu.Locator(":scope > .rz-menu, :scope > .rz-menu-list")
            .First
            .EvaluateAsync<string>("""
                element => {
                    const style = getComputedStyle(element);
                    return `${style.borderTopWidth}|${style.outlineStyle}|${style.boxShadow}|${style.backgroundColor}`;
                }
                """);
        innerMenuChrome.Should().Be("0px|none|none|rgba(0, 0, 0, 0)",
            "context menu chỉ được có một surface ở portal ngoài");
        var menuItemStyle = await editMenuItem.EvaluateAsync<MenuItemStyle>("""
            element => {
                const style = getComputedStyle(element);
                return { borderRadius: style.borderRadius, backgroundColor: style.backgroundColor };
            }
            """);
        Assert.True(menuItemStyle.BorderRadius != "0px", menuHtml);
        Assert.DoesNotContain(menuItemStyle.BackgroundColor, new[] { "transparent", "rgba(0, 0, 0, 0)" });
        var actionAnchorBox = await periodMoreActions.BoundingBoxAsync();
        var actionMenuBox = await periodMenu.BoundingBoxAsync();
        Assert.NotNull(actionAnchorBox);
        Assert.NotNull(actionMenuBox);
        Assert.False(
            RectanglesOverlap(actionAnchorBox, actionMenuBox),
            $"Menu action đang che nút gốc tại viewport {width}x{height}.");
        Assert.True(actionMenuBox.X >= 7, $"Menu vượt cạnh trái: x={actionMenuBox.X}.");
        Assert.True(
            actionMenuBox.X + actionMenuBox.Width <= width - 7,
            $"Menu vượt cạnh phải: right={actionMenuBox.X + actionMenuBox.Width}, viewport={width}.");
        await Page.WaitForTimeoutAsync(80);
        var settledActionMenuBox = await periodMenu.BoundingBoxAsync();
        Assert.NotNull(settledActionMenuBox);
        AssertStablePopupGeometry(actionMenuBox, settledActionMenuBox, "overflow action");
        var actionScreenshotDirectory = Environment.GetEnvironmentVariable(
            ScreenshotDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(actionScreenshotDirectory))
        {
            var directory = Path.GetFullPath(actionScreenshotDirectory);
            Directory.CreateDirectory(directory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, $"order-period-action-menu-{width}x{height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await editMenuItem.ClickAsync();
        await Assertions.Expect(Page.Locator("#period-action-panel")).ToHaveCountAsync(0);
        var editScheduleDialog = Page.GetByTestId("order-period-action-dialog");
        await Assertions.Expect(editScheduleDialog.GetByText("Sửa lịch kỳ", new() { Exact = true })).ToBeVisibleAsync();
        var datePickerAlignment = await editScheduleDialog.Locator(".rz-datepicker").EvaluateAllAsync<bool>("""
            elements => elements.length > 0 && elements.every(datePicker => {
                const trigger = datePicker.querySelector('.rz-datepicker-field-button');
                const icon = trigger?.querySelector('.rzi-calendar, .rzi-time');
                if (!trigger || !icon) {
                    return false;
                }

                const pickerBox = datePicker.getBoundingClientRect();
                const triggerBox = trigger.getBoundingClientRect();
                const iconBox = icon.getBoundingClientRect();
                const pickerCenter = pickerBox.top + pickerBox.height / 2;
                const triggerCenter = triggerBox.top + triggerBox.height / 2;
                const iconCenter = iconBox.top + iconBox.height / 2;
                return Math.abs(triggerCenter - pickerCenter) <= 1
                    && Math.abs(iconCenter - triggerCenter) <= 1;
            })
            """);
        datePickerAlignment.Should().BeTrue("icon lịch phải nằm đúng tâm dọc của input và trigger");
        var interactiveDatePicker = editScheduleDialog.Locator(".rz-datepicker:not(.rz-state-disabled)").First;
        var datePickerTrigger = interactiveDatePicker.Locator(".rz-datepicker-field-button");
        var triggerBeforeHover = await datePickerTrigger.BoundingBoxAsync();
        Assert.NotNull(triggerBeforeHover);
        await datePickerTrigger.HoverAsync();
        var triggerAfterHover = await datePickerTrigger.BoundingBoxAsync();
        Assert.NotNull(triggerAfterHover);
        AssertStablePopupGeometry(triggerBeforeHover, triggerAfterHover, "date-picker trigger hover");
        await datePickerTrigger.ClickAsync();
        var datePickerPopup = Page.Locator(".rz-datepicker-popup-container:visible").Last;
        await Assertions.Expect(datePickerPopup).ToBeVisibleAsync();
        var triggerAfterClick = await datePickerTrigger.BoundingBoxAsync();
        Assert.NotNull(triggerAfterClick);
        AssertStablePopupGeometry(triggerBeforeHover, triggerAfterClick, "date-picker trigger click");
        if (!string.IsNullOrWhiteSpace(actionScreenshotDirectory))
        {
            var directory = Path.GetFullPath(actionScreenshotDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, $"order-period-date-picker-open-{width}x{height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
        await datePickerPopup.GetByRole(AriaRole.Button, new() { Name = "Ok", Exact = true }).ClickAsync();
        await Assertions.Expect(datePickerPopup).ToBeHiddenAsync();
        await editScheduleDialog.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).ClickAsync();
        await Assertions.Expect(editScheduleDialog).ToHaveCountAsync(0);

        await periodMoreActions.ClickAsync();
        periodMenu = Page.Locator(".rz-context-menu:visible");
        await periodMenu.GetByText("Gia hạn kỳ", new() { Exact = true }).ClickAsync();
        await Assertions.Expect(Page.Locator(".rz-context-menu:visible")).ToHaveCountAsync(0);
        var periodActionDialog = Page.GetByTestId("order-period-action-dialog");
        await Assertions.Expect(periodActionDialog.GetByText("Gia hạn kỳ", new() { Exact = true })).ToBeVisibleAsync();
        await periodActionDialog.GetByRole(AriaRole.Button, new() { Name = "Hủy", Exact = true }).ClickAsync();
        await Assertions.Expect(periodActionDialog).ToHaveCountAsync(0);

        await Assertions.Expect(periodSurface.Locator("tbody tr td:nth-child(2) strong")).ToHaveCountAsync(0);
        await Assertions.Expect(periodSurface.GetByText("Chưa có ghi chú", new() { Exact = true })).ToHaveCountAsync(0);
        var periodLabels = (await periodSurface.Locator("tbody tr td:nth-child(2) .rz-cell-data").AllTextContentsAsync())
            .Select(label => label.Trim())
            .ToArray();
        periodLabels.Should().Equal(periodLabels
            .OrderByDescending(label => int.Parse(label[3..], CultureInfo.InvariantCulture))
            .ThenByDescending(label => int.Parse(label[..2], CultureInfo.InvariantCulture)),
            "danh mục kỳ phải hiển thị kỳ mới nhất trước");
        await Assertions.Expect(Page.GetByText("Ngày 29–31 tự co về ngày cuối tháng", new() { Exact = false }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByTestId("post-settlement-corrections"))
            .ToHaveCountAsync(0);

        var screenshotDirectory = Environment.GetEnvironmentVariable(
            ScreenshotDirectoryEnvironmentVariable);
        var layoutBody = Page.Locator(".vpp-layout-body");
        var scrollGeometry = await layoutBody.EvaluateAsync<ScrollGeometry>("""
            element => ({
                scrollHeight: element.scrollHeight,
                clientHeight: element.clientHeight,
                overflowY: getComputedStyle(element).overflowY
            })
            """);
        var hasScrollRange = scrollGeometry.ScrollHeight > scrollGeometry.ClientHeight + 2;
        if (hasScrollRange)
        {
            Assert.Contains(scrollGeometry.OverflowY, new[] { "auto", "scroll" });
        }

        await layoutBody.EvaluateAsync("element => element.scrollTop = 0");
        if (hasScrollRange)
        {
            await periodSurface.HoverAsync();
            await Page.Mouse.WheelAsync(0, 900);
            await Page.WaitForFunctionAsync(
                "element => element.scrollTop > 0",
                await layoutBody.ElementHandleAsync(),
                new PageWaitForFunctionOptions { Timeout = 10_000 });
        }

        await layoutBody.EvaluateAsync("element => element.scrollTop = 0");

        var bodyWidth = await Page.Locator("body").EvaluateAsync<double>("body => body.scrollWidth");
        Assert.True(bodyWidth <= width + 1, $"Page overflowed: body={bodyWidth}, viewport={width}.");

        if (width >= 1440 && height >= 800)
        {
            var contentBox = await Page.Locator(".vpp-content").BoundingBoxAsync();
            var surfaceBox = await periodSurface.BoundingBoxAsync();
            Assert.NotNull(contentBox);
            Assert.NotNull(surfaceBox);
            var bottomGap = (contentBox.Y + contentBox.Height) - (surfaceBox.Y + surfaceBox.Height);
            Assert.InRange(bottomGap, 0, 2);
        }

        if (!string.IsNullOrWhiteSpace(screenshotDirectory))
        {
            var directory = Path.GetFullPath(screenshotDirectory);
            Directory.CreateDirectory(directory);
            await WaitForRenderSettleAsync();
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, $"order-period-management-{width}x{height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    [Fact]
    public async Task OrderPeriodManagement_KeepsAutomationControlsInSystemAdministrationAndPassesAxeGate()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=periods");
        await Page.Locator(".vpp-sidebar[data-shell-ready='true']").WaitForAsync(
            new LocatorWaitForOptions { State = WaitForSelectorState.Attached });

        var settingsPanel = Page.Locator("#period-settings-panel");
        var manualPanel = Page.Locator("#manual-period-panel");
        await Assertions.Expect(settingsPanel).ToHaveCountAsync(0);
        await Assertions.Expect(manualPanel).ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByText("Cấu hình đang áp dụng", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByText("Duy trì 3 kỳ đang mở", new() { Exact = false }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mở đủ số kỳ", Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Mở một kỳ rời rạc", Exact = true }))
            .ToHaveCountAsync(0);

        var periodSurface = Page.Locator("[data-testid='order-period-management-table']:visible");
        await Assertions.Expect(periodSurface.GetByRole(
                AriaRole.Button,
                new() { Name = "Xem chi tiết", Exact = true }).First)
            .ToBeVisibleAsync();

        var axeResult = await Page.RunAxe();
        var blocking = axeResult.Violations
            .Where(violation => violation.Impact is "critical" or "serious")
            .SelectMany(violation => violation.Nodes.Select(node =>
                $"{violation.Id} ({violation.Impact}): {violation.Help}; target={string.Join(" ", node.Target)}; html={node.Html}"))
            .ToArray();
        Assert.True(blocking.Length == 0, string.Join(Environment.NewLine, blocking));
    }

    [Fact]
    public async Task PeriodSettlement_LoadingStateFillsTheAvailableWorkspace()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsDefaultUserAsync();
        await Page.RouteAsync("**/api/VPPRequest/period-info*", async route =>
        {
            await Task.Delay(3_000);
            await route.ContinueAsync();
        });

        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=review");
        var workspace = Page.Locator(".vpp-period-operations-workspace:visible");
        var skeleton = workspace.Locator(".vpp-skeleton-page.is-fill-available:visible");
        var skeletonGrid = skeleton.Locator(".vpp-skeleton-page-grid");
        var lastSkeletonRow = skeleton.Locator(".vpp-skeleton-grid-row").Last;
        await Assertions.Expect(skeleton).ToBeVisibleAsync();

        var workspaceBox = await workspace.BoundingBoxAsync();
        var skeletonBox = await skeleton.BoundingBoxAsync();
        var skeletonGridBox = await skeletonGrid.BoundingBoxAsync();
        var lastSkeletonRowBox = await lastSkeletonRow.BoundingBoxAsync();
        Assert.NotNull(workspaceBox);
        Assert.NotNull(skeletonBox);
        Assert.NotNull(skeletonGridBox);
        Assert.NotNull(lastSkeletonRowBox);
        Assert.InRange(
            (workspaceBox.Y + workspaceBox.Height) - (skeletonBox.Y + skeletonBox.Height),
            0,
            2);
        Assert.InRange(
            (workspaceBox.Y + workspaceBox.Height) - (skeletonGridBox.Y + skeletonGridBox.Height),
            0,
            2);
        Assert.InRange(
            (skeletonGridBox.Y + skeletonGridBox.Height) - (lastSkeletonRowBox.Y + lastSkeletonRowBox.Height),
            14,
            20);

        var screenshotDirectory = Environment.GetEnvironmentVariable(
            ScreenshotDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(screenshotDirectory))
        {
            var directory = Path.GetFullPath(screenshotDirectory);
            Directory.CreateDirectory(directory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(directory, "order-period-loading-1920x1080.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    [Fact]
    public async Task PeriodSettlement_InitialDataLoadStaysWithinInteractiveBudget()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();

        var stopwatch = Stopwatch.StartNew();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=review");
        var surface = Page.GetByTestId("period-settlement-data-surface");
        await Assertions.Expect(surface).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(surface.Locator(".vpp-skeleton-page"))
            .ToBeHiddenAsync(new() { Timeout = 15_000 });
        stopwatch.Stop();

        output.WriteLine("Period settlement initial data ready in {0} ms.", stopwatch.ElapsedMilliseconds);
        stopwatch.Elapsed.Should().BeLessThan(
            TimeSpan.FromSeconds(6),
            "the settlement route should keep the default grid on the lightweight critical path");
    }

    [Theory]
    [InlineData(1366, 768)]
    [InlineData(1920, 1080)]
    public async Task PeriodSettlement_UsesClearFinancialKpisAndAlignedDataColumns(int width, int height)
    {
        await Page.SetViewportSizeAsync(width, height);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=review");

        var surface = Page.GetByTestId("period-settlement-data-surface");
        await Assertions.Expect(surface).ToBeVisibleAsync(new() { Timeout = 15_000 });
        await Assertions.Expect(surface.Locator(".vpp-skeleton-page"))
            .ToBeHiddenAsync(new() { Timeout = 15_000 });

        var kpiCards = Page.Locator(".vpp-settlement-kpi-card:visible");
        await Assertions.Expect(kpiCards).ToHaveCountAsync(4);
        await Assertions.Expect(Page.GetByText("Chọn nhà cung cấp", new() { Exact = true }).First).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("Chọn bảng giá", new() { Exact = true }).First).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("Trước VAT và thuế VAT", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("Tổng giá trị", new() { Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Theo phòng ban", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Theo người đặt", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Theo mặt hàng", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(surface.GetByRole(AriaRole.Columnheader, new() { Name = "Tạm tính sort", Exact = true }))
            .ToBeVisibleAsync();

        var beforeVatCard = Page.Locator(".vpp-settlement-kpi-card").Nth(2);
        var totalCard = Page.Locator(".vpp-settlement-kpi-card.is-total");
        var beforeVat = ParseVnd(await beforeVatCard.Locator("strong").InnerTextAsync());
        var vatAmount = ParseVnd(await beforeVatCard.Locator("small").InnerTextAsync());
        var grandTotal = ParseVnd(await totalCard.Locator("strong").InnerTextAsync());
        (beforeVat + vatAmount).Should().Be(grandTotal,
            "tổng giá trị phải bằng giá trị trước VAT cộng thuế GTGT");

        var numericAlignment = await surface.Locator(".vpp-settlement-number")
            .EvaluateAllAsync<bool>("elements => elements.length > 0 && elements.every(element => getComputedStyle(element).textAlign === 'right')");
        numericAlignment.Should().BeTrue("mọi cột số phải dùng cùng một trục căn phải");

        var evidenceDirectory = Environment.GetEnvironmentVariable(ScreenshotDirectoryEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, $"period-settlement-kpis-{width}x{height}.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }
    }

    [Fact]
    public async Task SettlementAction_DeepLinksToTheSelectedPeriod()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=periods");

        var surface = Page.Locator("[data-testid='order-period-management-table']:visible");
        await Assertions.Expect(surface).ToBeVisibleAsync();
        var settlementButton = surface.Locator("button:not(:disabled)")
            .Filter(new LocatorFilterOptions { HasText = "Xem chi tiết" })
            .First;
        var settlementRow = settlementButton.Locator("xpath=ancestor::tr[1]");
        await Assertions.Expect(settlementRow).ToBeVisibleAsync();
        var periodLabel = (await settlementRow.Locator("td:nth-child(2)").InnerTextAsync()).Trim();
        var parts = periodLabel.Split('/');

        await settlementButton.ClickAsync();

        var expectedQuery =
            $"tab=5&periodTab=review&periodYear={parts[1]}&periodMonth={int.Parse(parts[0], CultureInfo.InvariantCulture)}";
        await Page.WaitForFunctionAsync(
            "expected => window.location.search.includes(expected)",
            expectedQuery,
            new PageWaitForFunctionOptions { Timeout = 10_000 });
        Page.Url.Should().Contain(expectedQuery);
        await Assertions.Expect(Page.Locator(".vpp-header-sub-tab[aria-current='page']").First)
            .ToHaveTextAsync("Chốt kỳ");
        await Assertions.Expect(Page.GetByText($"Kỳ {periodLabel}", new() { Exact = true }).First)
            .ToBeVisibleAsync();
    }

    private sealed class ScrollGeometry
    {
        public double ScrollHeight { get; init; }
        public double ClientHeight { get; init; }
        public string OverflowY { get; init; } = string.Empty;
    }

    private static long ParseVnd(string value) => long.Parse(
        new string(value.Where(char.IsDigit).ToArray()),
        CultureInfo.InvariantCulture);

    private sealed class MenuItemStyle
    {
        public string BorderRadius { get; init; } = string.Empty;
        public string BackgroundColor { get; init; } = string.Empty;
    }

    private static bool RectanglesOverlap(
        LocatorBoundingBoxResult first,
        LocatorBoundingBoxResult second)
        => first.X < second.X + second.Width
            && first.X + first.Width > second.X
            && first.Y < second.Y + second.Height
            && first.Y + first.Height > second.Y;

    private static void AssertStablePopupGeometry(
        LocatorBoundingBoxResult first,
        LocatorBoundingBoxResult second,
        string popupName)
    {
        Math.Abs(first.X - second.X).Should().BeLessThanOrEqualTo(1, $"{popupName} không được nhảy ngang sau khi hiện");
        Math.Abs(first.Y - second.Y).Should().BeLessThanOrEqualTo(1, $"{popupName} không được nhảy dọc sau khi hiện");
        Math.Abs(first.Width - second.Width).Should().BeLessThanOrEqualTo(1, $"{popupName} không được đổi chiều rộng sau khi hiện");
        Math.Abs(first.Height - second.Height).Should().BeLessThanOrEqualTo(1, $"{popupName} không được đổi chiều cao sau khi hiện");
    }
}
