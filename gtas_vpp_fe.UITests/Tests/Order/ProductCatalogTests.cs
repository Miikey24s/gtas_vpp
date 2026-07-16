using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using Microsoft.Playwright;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order;

public sealed class ProductCatalogTests : TestBase, IAuthenticatedUiTest
{
    [Fact]
    public async Task Employee_CanBrowseRequestProductCatalog()
    {
        await LoginAsAsync(TestAccounts.Employee);
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2");

        var grid = Page.Locator(".vpp-data-card.vpp-datagrid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await grid.Locator("tbody tr").CountAsync()).Should().BeGreaterThan(0);
    }
}
