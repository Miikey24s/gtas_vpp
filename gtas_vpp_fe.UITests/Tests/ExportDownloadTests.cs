using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;

namespace gtas_vpp_fe.UITests.Tests;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class ExportDownloadTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task ReportExports_DownloadRealFilesFromTheIsolatedFixture()
    {
        await LoginAsDefaultUserAsync();
        await Page.GotoAsync($"{BaseUrl}report", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator(".vpp-report-page").WaitForAsync();

        await AssertDownloadAsync("Xuất CSV", ".csv");
        await AssertDownloadAsync("Xuất Excel", ".xlsx");
    }

    [Fact]
    public async Task OrderExports_DownloadRealFilesFromTheIsolatedFixture()
    {
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=0", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await Page.Locator(".vpp-orders-workspace").WaitForAsync();

        await AssertDownloadAsync("Xuất PDF", ".pdf");
        await AssertDownloadAsync("Xuất Excel", ".xlsx");
    }

    private async Task AssertDownloadAsync(string buttonName, string expectedExtension)
    {
        var button = Page.Locator("#main-content button")
            .Filter(new LocatorFilterOptions { HasText = buttonName })
            .Last;
        try
        {
            await button.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 30_000
            });
        }
        catch (PlaywrightException exception)
        {
            var visibleButtons = await Page.Locator("#main-content button:visible").AllInnerTextsAsync();
            throw new InvalidOperationException(
                $"Export action '{buttonName}' was unavailable at {Page.Url}. " +
                $"Visible buttons: {string.Join(" | ", visibleButtons.Select(text => text.Trim()))}",
                exception);
        }

        var download = await Page.RunAndWaitForDownloadAsync(() => button.ClickAsync());

        download.SuggestedFilename.Should().EndWith(
            expectedExtension,
            $"the {buttonName} action should preserve its server-provided file type");
        (await download.FailureAsync()).Should().BeNull(
            $"the {buttonName} action should finish without a browser download failure");

        var path = await download.PathAsync();
        path.Should().NotBeNullOrWhiteSpace();
        new FileInfo(path!).Length.Should().BeGreaterThan(
            32,
            $"the {buttonName} action should return a real file instead of an empty response");
    }
}
