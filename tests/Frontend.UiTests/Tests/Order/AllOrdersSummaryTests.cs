using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

[Collection(ReadOnlyE2ECollection.Name)]
public sealed class AllOrdersSummaryTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Procurement_CanSeeCompanyRequestSummary()
    {
        // D2/D8: grid Tổng hợp toàn công ty giờ là chế độ "Theo đơn" của bước Gom nhu cầu.
        await LoginAsAsync(TestAccounts.Procurement);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=5&periodTab=demand");

        await Page.Locator(".vpp-demand-view-toggle").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        await Page.Locator(".vpp-demand-view-toggle button").Nth(1).ClickAsync();

        var grid = Page.Locator(".vpp-data-card.vpp-datagrid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var totalOrders = Page.Locator(".kpi-grid:visible").Last
            .Locator(".kpi-card-value").First;
        await totalOrders.WaitForAsync();
        (await totalOrders.InnerTextAsync()).Trim().Should().Be("3");
    }

    [Fact]
    public async Task LegacyAllOrdersUrl_RedirectsToPeriodDemand()
    {
        await LoginAsAsync(TestAccounts.Procurement);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=3&managementTab=all");

        await Page.WaitForURLAsync(url => url.Contains("periodTab=demand"));
        await Page.Locator(".vpp-demand-view-toggle").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
