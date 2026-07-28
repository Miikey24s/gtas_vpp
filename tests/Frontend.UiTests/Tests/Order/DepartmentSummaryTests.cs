using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class DepartmentSummaryTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Manager_CanSeeOnlyDepartmentRequestSummary()
    {
        await LoginAsAsync(TestAccounts.Manager);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=3&managementTab=department");

        var grid = Page.Locator("[data-testid='department-summary-data-surface']:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await grid.GetAttributeAsync("data-vpp-data-source-mode")).Should().Be("server-paging");
        (await grid.Locator(".vpp-filter-select").CountAsync()).Should().Be(4);
        (await grid.Locator(".rz-paginator, .rz-pager").CountAsync()).Should().BeGreaterThan(0);
        var totalOrders = Page.Locator(".kpi-grid:visible").Last
            .Locator(".kpi-card-value").First;
        await totalOrders.WaitForAsync();
        (await totalOrders.InnerTextAsync()).Trim().Should().Be("2");
        (await grid.InnerTextAsync()).Should().NotContain("QA-D02", "another department must stay out of manager scope");

        var evidenceDirectory = Environment.GetEnvironmentVariable("UITEST_EVIDENCE_DIR");
        if (!string.IsNullOrWhiteSpace(evidenceDirectory))
        {
            Directory.CreateDirectory(evidenceDirectory);
            await Page.ScreenshotAsync(new()
            {
                Path = Path.Combine(evidenceDirectory, "ds3-department-summary-1366x768.png"),
                FullPage = false,
                Animations = ScreenshotAnimations.Disabled,
                Caret = ScreenshotCaret.Hide
            });
        }
    }
}
