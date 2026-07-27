using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class UserMenuVisualTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task AccountMenu_MatchesAtlasStructureAndKeepsLogoutNeutralUntilInteraction()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();

        // Shell dùng chung một asset brand SVG, render ở cả hai biến thể sidebar
        // (mở rộng + thu gọn) — đúng cấu trúc đã chốt trong SharedUiFoundationTests.
        var brandMark = Page.Locator(".vpp-brand-mark img[src$='vpp-app-icon.svg']");
        await brandMark.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 30_000
        });
        (await brandMark.CountAsync()).Should().Be(2, "the expanded and collapsed sidebar brands share one SVG asset");

        var trigger = Page.Locator(".user-menu-trigger");
        await trigger.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await trigger.ClickAsync();

        var dropdown = Page.Locator("#user-menu-dropdown");
        await dropdown.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await dropdown.GetAttributeAsync("role")).Should().Be("dialog", "the account menu is a dialog surface");
        (await dropdown.Locator(".user-dropdown-name").InnerTextAsync()).Trim().Should().NotBeNullOrWhiteSpace();
        (await dropdown.Locator(".user-dropdown-department").CountAsync())
            .Should().BeLessThanOrEqualTo(1, "the department should appear at most once in the identity block");

        // Thứ tự 4 hàng theo Atlas "Menu tài khoản": Ngôn ngữ → Giao diện → Thông báo → Đăng xuất.
        var actionLabels = await dropdown.Locator(".user-dropdown-action .user-dropdown-action-label")
            .AllInnerTextsAsync();
        actionLabels.Select(label => label.Trim()).Should().ContainInOrder(
            new[] { "Ngôn ngữ", "Giao diện", "Thông báo", "Đăng xuất" },
            "the account menu rows must follow the Atlas order");

        // Hàng Ngôn ngữ hiển thị tên đầy đủ của ngôn ngữ hiện hành, không viết tắt EN/VI.
        var languageTrailing = (await dropdown.Locator(".user-dropdown-action-trailing").First.InnerTextAsync()).Trim();
        languageTrailing.Should().MatchRegex("^(Tiếng Việt|Tiếng Anh|Vietnamese|English)$",
            "the language row should show the full language name per Atlas");

        var logout = Page.Locator(".user-dropdown-logout");
        var appearance = await logout.EvaluateAsync<string[]>("""
            element => {
                const style = getComputedStyle(element);
                return [style.backgroundColor, style.color];
            }
            """);
        appearance[0].Should().Be("rgba(0, 0, 0, 0)", "logout should be visually neutral before hover or focus");
        appearance[1].Should().NotBe("rgb(255, 255, 255)", "logout must remain readable on the menu surface");

        // Toggle giao diện từ trong menu phải đổi theme toàn cục và giữ menu mở.
        var themeRow = dropdown.Locator(".user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasTextRegex = new Regex("Giao diện") });
        await themeRow.ClickAsync();
        await Page.WaitForFunctionAsync("() => document.documentElement.classList.contains('rz-theme-dark')");
        await themeRow.ClickAsync();
        await Page.WaitForFunctionAsync("() => !document.documentElement.classList.contains('rz-theme-dark')");

        // Hàng Thông báo mở "Hộp thư thông báo" với icon VppIcon, không glyph Radzen sót.
        var notificationRow = dropdown.Locator(".vpp-header-notification-button");
        await notificationRow.ClickAsync();
        var notificationPanel = Page.Locator("#vpp-notification-panel");
        await notificationPanel.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await notificationPanel.Locator("header strong").InnerTextAsync()).Trim()
            .Should().MatchRegex("^(Hộp thư thông báo|Notification inbox)$", "the panel title must match Atlas copy");
        // Mở panel kích hoạt Inbox.RefreshAsync nên nội dung đi qua trạng thái loading
        // chỉ-có-chữ (1 icon Refresh ở footer) trước khi chốt empty/items/inline-status.
        // Một re-render (realtime inbox) giữa wait và CountAsync từng làm phép đếm rơi
        // trúng khung loading → đo 1 icon dù mọi trạng thái đã chốt đều có ≥2. Chụp số
        // đếm NGAY trong điều kiện "đã chốt" để wait và assert là một snapshot nguyên tử.
        var iconSnapshotHandle = await Page.WaitForFunctionAsync("""
            () => {
                const panel = document.querySelector('#vpp-notification-panel');
                if (!panel) {
                    return null;
                }

                const settled = panel.querySelector('.vpp-notification-empty .vpp-icon')
                    || panel.querySelector('.vpp-notification-item')
                    || panel.querySelector('.vpp-notification-inline-status .vpp-icon');
                if (!settled) {
                    return null;
                }

                return JSON.stringify({
                    rzi: panel.querySelectorAll('.rzi').length,
                    vppIcons: panel.querySelectorAll('.vpp-icon').length
                });
            }
            """);
        var iconSnapshot = System.Text.Json.JsonDocument
            .Parse(await iconSnapshotHandle.JsonValueAsync<string>())
            .RootElement;
        iconSnapshot.GetProperty("rzi").GetInt32().Should().Be(0, "the notification panel should not rely on missing Radzen icon glyphs");
        iconSnapshot.GetProperty("vppIcons").GetInt32().Should().BeGreaterThan(1, "notification states and actions should use visible VppIcon glyphs");

        // Item chưa đọc (nếu có) phải mang badge chữ "Mới" theo Atlas, không chấm tròn câm.
        var unreadItems = await notificationPanel.Locator(".vpp-notification-item.is-unread").CountAsync();
        if (unreadItems > 0)
        {
            (await notificationPanel.Locator(".vpp-notification-unread-badge").CountAsync())
                .Should().BeGreaterThan(0, "unread notifications must carry the visible 'Mới' badge");
        }

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "account-user-menu-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide,
                Scale = ScreenshotScale.Css
            });
        }

        await trigger.FocusAsync();
        await trigger.PressAsync("Escape");
        await dropdown.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached
        });
    }
}
