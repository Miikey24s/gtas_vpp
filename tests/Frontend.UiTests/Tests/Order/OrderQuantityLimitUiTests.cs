using System.Text.RegularExpressions;
using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class OrderQuantityLimitUiTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task QuantityLimit_IsConfigurableAndVisibleAcrossAdminAndOrderFlow()
    {
        await Page.SetViewportSizeAsync(1366, 768);
        await LoginAsDefaultUserAsync();

        await Page.GotoAsync($"{BaseUrl}library?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        var itemSurface = Page.Locator("[data-testid='item-admin-data-surface']");
        await itemSurface.WaitForAsync();
        var columnPickerTrigger = itemSurface.Locator(".vpp-column-picker-trigger");
        await columnPickerTrigger.ClickAsync();
        var columnPicker = Page.Locator(".vpp-column-picker-popover:popover-open");
        await columnPicker.WaitForAsync();
        (await columnPicker.Locator(".vpp-column-picker-label").AllInnerTextsAsync())
            .Should().Contain("Số lượng tối đa");
        await Page.Keyboard.PressAsync("Escape");

        var addItemButton = itemSurface.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Thêm mặt hàng", RegexOptions.IgnoreCase)
        });
        await addItemButton.ClickAsync();

        var dialog = Page.Locator(".rz-dialog:visible").Last;
        await dialog.GetByText("Số lượng tối đa", new() { Exact = true }).WaitForAsync();
        await CaptureIfRequestedAsync("order-quantity-limit-admin.png");
        var limitEditor = dialog.Locator(".rz-numeric input").First;
        await limitEditor.WaitForAsync();
        var minimum = await limitEditor.GetAttributeAsync("min")
            ?? await limitEditor.GetAttributeAsync("aria-valuemin");
        var maximum = await limitEditor.GetAttributeAsync("max")
            ?? await limitEditor.GetAttributeAsync("aria-valuemax");
        minimum.Should().Be("1");
        maximum.Should().Be("1000");
        (await limitEditor.InputValueAsync()).Should().Be("1000");
        await Page.Keyboard.PressAsync("Escape");

        await SwitchUserAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        var orderCard = Page.Locator("[data-testid='current-order-panel']:visible").Last;
        await orderCard.WaitForAsync();
        await orderCard.GetByRole(AriaRole.Button, new()
        {
            NameRegex = new Regex("^Chỉnh sửa đơn ", RegexOptions.IgnoreCase)
        }).Last.ClickAsync();

        await WaitForUrlMatchAsync(
            new Regex(".*/dashboard/order-create.*orderId=.*", RegexOptions.IgnoreCase),
            TimeSpan.FromSeconds(30));
        var catalogSurface = Page.Locator("[data-testid='order-create-catalog-data-surface']");
        await Assertions.Expect(catalogSurface.GetByRole(AriaRole.Columnheader, new()
        {
            Name = "Số lượng tối đa",
            Exact = true
        })).ToBeVisibleAsync();

        var draftItem = Page.Locator(".vpp-order-draft-item").First;
        await draftItem.WaitForAsync();
        var limitText = (await draftItem.Locator(".vpp-order-quantity-limit").InnerTextAsync()).Trim();
        limitText.Should().MatchRegex("^Số lượng tối đa: [0-9]+$");

        var quantityInput = draftItem.Locator(".vpp-order-quantity-input");
        var inputMaximum = await quantityInput.GetAttributeAsync("max");
        inputMaximum.Should().BeNull();
        var configuredLimit = int.Parse(
            limitText.Replace("Số lượng tối đa: ", string.Empty, StringComparison.Ordinal));

        await quantityInput.FillAsync((configuredLimit + 1).ToString());
        await Assertions.Expect(draftItem.Locator(".vpp-order-quantity-warning"))
            .ToContainTextAsync(configuredLimit.ToString());
        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Tiếp tục", Exact = true }))
            .ToBeDisabledAsync();
        await CaptureIfRequestedAsync("order-quantity-limit-validation.png");

        await quantityInput.FillAsync(configuredLimit.ToString());
        await Assertions.Expect(draftItem.Locator(".vpp-order-quantity-warning"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(draftItem.Locator(".vpp-order-quantity-stepper button").Last)
            .ToBeDisabledAsync(new() { Timeout = 5_000 });

        await CaptureIfRequestedAsync("order-quantity-limit-order-selection.png");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Tiếp tục", Exact = true }).ClickAsync();
        await Page.Locator(".vpp-order-review-grid-frame").WaitForAsync();
        await Assertions.Expect(Page.Locator(".vpp-order-review-grid-frame .vpp-order-quantity-limit"))
            .ToHaveCountAsync(0);
        await CaptureIfRequestedAsync("order-quantity-limit-order.png");
    }

    private async Task CaptureIfRequestedAsync(string fileName)
    {
        var directory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(directory, fileName),
            FullPage = false,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
            Scale = ScreenshotScale.Css
        });
    }

    private async Task WaitForUrlMatchAsync(Regex expectedUrl, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            if (expectedUrl.IsMatch(Page.Url))
            {
                return;
            }

            await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        throw new TimeoutException(
            $"Timed out waiting for URL '{expectedUrl}'. Last URL: {Page.Url}");
    }
}
