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

        var grid = Page.Locator(".vpp-data-card.vpp-datagrid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var totalOrders = Page.Locator(".kpi-grid:visible").Last
            .Locator(".kpi-card-value").First;
        await totalOrders.WaitForAsync();
        (await totalOrders.InnerTextAsync()).Trim().Should().Be("2");
        (await grid.InnerTextAsync()).Should().NotContain("QA-D02", "another department must stay out of manager scope");
    }
}
