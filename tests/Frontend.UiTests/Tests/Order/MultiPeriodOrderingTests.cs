using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class MultiPeriodOrderingTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Employee_can_select_an_open_period_and_keep_it_on_create_page()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");
        var picker = Page.Locator(".vpp-orders-period-decision");
        var trigger = picker.Locator(".vpp-decision-select-trigger");
        await Assertions.Expect(trigger).ToBeVisibleAsync();
        await trigger.ClickAsync();
        var options = picker.GetByRole(AriaRole.Option);
        Assert.True(await options.CountAsync() >= 2, "Rolling horizon should expose at least two open periods in the isolated fixture.");
        var selectedPeriodId = await options.Last.GetAttributeAsync("data-value");
        Assert.False(string.IsNullOrWhiteSpace(selectedPeriodId));

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "my-orders-period-selector-open-1920x1080.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

        await options.Last.ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(new Regex($"periodId={selectedPeriodId}", RegexOptions.IgnoreCase));
        await Assertions.Expect(trigger).ToHaveAttributeAsync("data-value", selectedPeriodId!);

        await Page.GotoAsync($"{BaseUrl}dashboard/order-create?periodId={selectedPeriodId}");
        var createPicker = Page.Locator(".vpp-order-create-period-decision .vpp-decision-select-trigger");
        await Assertions.Expect(createPicker).ToBeVisibleAsync();
        await Assertions.Expect(createPicker).ToHaveAttributeAsync("data-value", selectedPeriodId!);
        await Assertions.Expect(Page.GetByText("Mỗi kỳ có thời hạn và đơn riêng.")).ToBeVisibleAsync();
    }
}
