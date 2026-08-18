using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class MultiPeriodOrderingTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task RegularAndSupplementViews_KeepIndependentPeriodSelections()
    {
        await Page.SetViewportSizeAsync(1920, 1080);
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0");

        var periodTrigger = Page.Locator(
            ".vpp-orders-period-decision-card .vpp-decision-select-trigger");
        await Assertions.Expect(periodTrigger).ToBeVisibleAsync();

        var regularPeriodId = await periodTrigger.GetAttributeAsync("data-value");
        var regularPeriodLabel = (await periodTrigger
            .Locator(".vpp-decision-select-label")
            .InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(regularPeriodId));
        regularPeriodLabel.Should().Be("08/2026");

        await Page.Locator(".vpp-orders-view-selector > button").Nth(1).ClickAsync();
        await Assertions.Expect(Page).ToHaveURLAsync(
            new Regex(".*[?&]orderView=supplement(?:&.*)?$", RegexOptions.IgnoreCase));

        var supplementPeriodId = await periodTrigger.GetAttributeAsync("data-value");
        var supplementPeriodLabel = (await periodTrigger
            .Locator(".vpp-decision-select-label")
            .InnerTextAsync()).Trim();
        Assert.False(string.IsNullOrWhiteSpace(supplementPeriodId));
        supplementPeriodId.Should().NotBe(regularPeriodId);
        supplementPeriodLabel.Should().Be("07/2026",
            "đơn bổ sung của kỳ hiện tại 08/2026 phải mặc định về kỳ đã đóng gần nhất 07/2026");
        Page.Url.Contains("regularPeriodId=", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
        Page.Url.Contains("supplementPeriodId=", StringComparison.OrdinalIgnoreCase).Should().BeTrue();

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = Path.Combine(evidenceDirectory, "my-orders-independent-periods-1920x1080.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }

        await Page.Locator(".vpp-orders-view-selector > button").Nth(0).ClickAsync();
        await Assertions.Expect(periodTrigger).ToHaveAttributeAsync("data-value", regularPeriodId!);
        await Assertions.Expect(periodTrigger.Locator(".vpp-decision-select-label"))
            .ToHaveTextAsync(regularPeriodLabel);
    }
}
