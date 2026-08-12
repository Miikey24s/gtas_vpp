using System.Text;
using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

public sealed class PriceListImportTests : TestBase, IMutatingUiTest
{
    [Fact]
    public async Task PriceImport_PreviewsAtFourViewportsAndConfirmsOnlyAfterValidation()
    {
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}library?tab=6&pricingTab=prices", new()
        {
            WaitUntil = WaitUntilState.Load
        });
        var surface = Page.GetByTestId("prices-data-surface");
        await WaitForSurfaceAsync(surface);
        await Page.WaitForFunctionAsync(
            """
            () => {
                const button = [...document.querySelectorAll('button')]
                    .find(element => /import bảng giá/i.test(element.textContent || ''));
                const row = document.querySelector('[data-testid="prices-data-surface"] tbody tr .vpp-admin-two-line-cell small');
                return !!button && !button.disabled && !!row?.textContent?.trim();
            }
            """,
            null,
            new() { Timeout = 60_000 });
        var itemCode = (await surface.Locator("tbody tr .vpp-admin-two-line-cell small").First.InnerTextAsync()).Trim();

        var viewports = new[]
        {
            (Width: 390, Height: 844),
            (Width: 768, Height: 1024),
            (Width: 1366, Height: 768),
            (Width: 1920, Height: 1080)
        };
        foreach (var (viewport, index) in viewports.Select((value, index) => (value, index)))
        {
            await Page.SetViewportSizeAsync(viewport.Width, viewport.Height);
            await Page.WaitForTimeoutAsync(250);
            var sidebarBackdrop = Page.Locator(".vpp-sidebar-backdrop:visible");
            if (await sidebarBackdrop.CountAsync() > 0)
            {
                await sidebarBackdrop.ClickAsync();
                await sidebarBackdrop.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            }
            if (index == viewports.Length - 1)
            {
                await SetDarkModeAsync(true);
            }
            await Page.GetByRole(AriaRole.Button, new() { Name = "Import bảng giá" }).ClickAsync();
            var dialog = Page.GetByTestId("price-list-import-dialog");
            await dialog.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            var csv = index == 0
                ? $"Ma noi bo,Gia ban,Thue suat,Ghi chu rieng\n{itemCode},{20_000 + index},8,Kiểm thử import"
                : $"ItemCode,UnitPrice,VatRate,Note\n{itemCode},{20_000 + index},8,Kiểm thử import";
            await dialog.Locator("#price-list-import-file").SetInputFilesAsync(new FilePayload
            {
                Name = $"bang-gia-{viewport.Width}.csv",
                MimeType = "text/csv",
                Buffer = Encoding.UTF8.GetBytes(csv)
            });

            if (index == 0)
            {
                await dialog.Locator(".vpp-price-import-mapping-editor").WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 60_000
                });
                await Assertions.Expect(dialog.GetByText(
                        "Chưa nhận diện được cột Mã mặt hàng. Vui lòng chọn cột tương ứng.",
                        new() { Exact = true }))
                    .ToBeVisibleAsync();
                await Assertions.Expect(dialog.GetByText(
                        "Chưa nhận diện được cột ItemCode. Vui lòng chọn cột tương ứng.",
                        new() { Exact = true }))
                    .ToHaveCountAsync(0);
                await AssertDialogFooterVisibleAsync(dialog, viewport.Width, viewport.Height);
                await CaptureAsync("price-import-mapping-390x844.png");
                await SelectMappingAsync(dialog, 0, "Mã mặt hàng");
                await SelectMappingAsync(dialog, 1, "Đơn giá");
                await SelectMappingAsync(dialog, 2, "Thuế");
                await dialog.GetByRole(AriaRole.Button, new() { Name = "Kiểm tra dữ liệu" }).ClickAsync();
            }

            await dialog.Locator(".vpp-price-import-summary").WaitForAsync(new()
            {
                State = WaitForSelectorState.Visible,
                Timeout = 60_000
            });
            (await dialog.Locator(".vpp-price-import-mapping-list > span").CountAsync()).Should().BeGreaterThanOrEqualTo(2);
            (await dialog.Locator(".vpp-price-import-grid tbody tr").CountAsync()).Should().BeGreaterThan(0);
            var confirmButton = dialog.GetByRole(AriaRole.Button, new() { Name = "Xác nhận nhập" });
            await Assertions.Expect(confirmButton).ToBeEnabledAsync(new() { Timeout = 60_000 });
            (await dialog.Locator("input[type='file']").EvaluateAllAsync<string[]>(
                "elements => elements.map(element => getComputedStyle(element).opacity)"))
                .Should().OnlyContain(opacity => opacity == "0");
            await AssertNoDocumentOverflowAsync(viewport.Width, $"price import {viewport.Width}x{viewport.Height}");
            await CaptureAsync($"price-import-{viewport.Width}x{viewport.Height}.png");
            var geometry = await dialog.EvaluateAsync<double[]>(
                """
                element => {
                    const rect = element.getBoundingClientRect();
                    return [rect.left, rect.right, rect.top, rect.bottom, innerWidth, innerHeight];
                }
                """);
            geometry[0].Should().BeGreaterThanOrEqualTo(-1);
            geometry[1].Should().BeLessThanOrEqualTo(geometry[4] + 1);
            geometry[2].Should().BeGreaterThanOrEqualTo(-1);
            geometry[3].Should().BeLessThanOrEqualTo(geometry[5] + 1);
            await AssertDialogFooterVisibleAsync(dialog, viewport.Width, viewport.Height);

            if (index == viewports.Length - 1)
            {
                await confirmButton.ClickAsync();
                await dialog.GetByText("Đã nhập bảng giá", new() { Exact = true }).WaitForAsync(new()
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = 60_000
                });
                var axeResult = await Page.RunAxe(new AxeRunContext
                {
                    Include = [new AxeSelector("[data-testid='price-list-import-dialog']")]
                });
                var blocking = axeResult.Violations
                    .Where(violation => violation.Impact is "critical" or "serious")
                    .ToArray();
                blocking.Should().BeEmpty("dialog import must pass the critical/serious accessibility gate");
                await dialog.GetByRole(AriaRole.Button, new() { Name = "Đóng" }).ClickAsync();
                await SetDarkModeAsync(false);
            }
            else
            {
                await dialog.GetByRole(AriaRole.Button, new() { Name = "Hủy" }).ClickAsync();
            }
        }
    }

    private async Task WaitForSurfaceAsync(ILocator surface)
    {
        await surface.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        await Page.WaitForFunctionAsync(
            """
            () => [...document.querySelectorAll('.rz-datatable-loading')].every(element => {
                const style = getComputedStyle(element);
                const rect = element.getBoundingClientRect();
                return style.display === 'none'
                    || style.visibility === 'hidden'
                    || Number.parseFloat(style.opacity || '1') === 0
                    || rect.width === 0
                    || rect.height === 0;
            })
            """,
            null,
            new() { Timeout = 60_000 });
        await WaitForRenderSettleAsync();
    }

    private async Task AssertNoDocumentOverflowAsync(int viewportWidth, string route)
    {
        var geometry = await Page.EvaluateAsync<double[]>(
            "() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        geometry[0].Should().BeLessThanOrEqualTo(geometry[1] + 1,
            $"{route} must keep wide data inside its own surface at {viewportWidth}px");
    }

    private static async Task AssertDialogFooterVisibleAsync(ILocator dialog, int width, int height)
    {
        var contract = await dialog.Locator(".vpp-adaptive-dialog-footer").EvaluateAsync<string>(
            """
            element => {
                const rect = element.getBoundingClientRect();
                const style = getComputedStyle(element);
                const shell = element.closest('.vpp-adaptive-dialog-shell');
                const body = shell?.querySelector('.vpp-adaptive-dialog-body');
                const content = element.closest('.rz-dialog-content');
                const radzenDialog = element.closest('.rz-dialog');
                const title = radzenDialog?.querySelector('.rz-dialog-titlebar');
                const shellRect = shell?.getBoundingClientRect();
                const bodyRect = body?.getBoundingClientRect();
                const contentRect = content?.getBoundingClientRect();
                const dialogRect = radzenDialog?.getBoundingClientRect();
                const titleRect = title?.getBoundingClientRect();
                const pointX = Math.min(innerWidth - 1, Math.max(0, rect.left + rect.width / 2));
                const pointY = Math.min(innerHeight - 1, Math.max(0, rect.top + rect.height / 2));
                const hit = document.elementFromPoint(pointX, pointY);
                const visible = style.display !== 'none'
                    && style.visibility !== 'hidden'
                    && Number.parseFloat(style.opacity || '1') > 0
                    && rect.height > 40
                    && rect.top >= 0
                    && rect.bottom <= innerHeight + 1
                    && !!hit
                    && (hit === element || element.contains(hit));
                return [
                    visible,
                    `footer=${rect.top},${rect.bottom},${rect.height},padding:${style.padding},display:${style.display}`,
                    `shell=${shellRect?.top},${shellRect?.bottom},${shellRect?.height},rows:${shell ? getComputedStyle(shell).gridTemplateRows : ''}`,
                    `body=${bodyRect?.top},${bodyRect?.bottom},${bodyRect?.height},overflow:${body ? getComputedStyle(body).overflow : ''}`,
                    `content=${contentRect?.top},${contentRect?.bottom},${contentRect?.height},flex:${content ? getComputedStyle(content).flex : ''}`,
                    `dialog=${dialogRect?.top},${dialogRect?.bottom},${dialogRect?.height}`,
                    `title=${titleRect?.top},${titleRect?.bottom},${titleRect?.height}`,
                    `viewport=${innerHeight}`,
                    `hit=${hit?.className || hit?.tagName}`
                ].join('|');
            }
            """);
        contract.Should().StartWith("true",
            $"price import footer must remain visible and clickable at {width}x{height}; actual={contract}");
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
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }

    private async Task SelectMappingAsync(ILocator dialog, int columnIndex, string optionLabel)
    {
        var row = dialog.GetByTestId($"price-import-mapping-row-{columnIndex}");
        var trigger = row.Locator(".vpp-decision-select-trigger");
        await trigger.ClickAsync();
        var popoverId = await trigger.GetAttributeAsync("aria-controls");
        popoverId.Should().NotBeNullOrWhiteSpace();
        var popover = Page.Locator($"#{popoverId}");
        await popover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await popover.GetByRole(AriaRole.Option, new() { Name = optionLabel, Exact = true }).ClickAsync();
        await popover.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
    }

    private async Task SetDarkModeAsync(bool darkMode)
    {
        var currentMode = await Page.EvaluateAsync<bool>(
            "() => document.documentElement.classList.contains('rz-theme-dark')");
        if (currentMode == darkMode)
        {
            return;
        }

        await Page.Locator(".user-menu-trigger").First.ClickAsync();
        var themeToggle = Page.Locator("#user-menu-dropdown .user-dropdown-action")
            .Filter(new LocatorFilterOptions { HasText = "Giao diện" });
        await themeToggle.ClickAsync();
        await Page.WaitForFunctionAsync(
            darkMode
                ? "() => document.documentElement.classList.contains('rz-theme-dark')"
                : "() => !document.documentElement.classList.contains('rz-theme-dark')");
        var backdrop = Page.Locator(".user-dropdown-backdrop:visible");
        if (await backdrop.CountAsync() > 0)
        {
            await backdrop.ClickAsync();
            await backdrop.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }
        await WaitForRenderSettleAsync();
    }
}
