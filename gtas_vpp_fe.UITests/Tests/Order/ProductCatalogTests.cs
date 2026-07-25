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
        await Page.GotoAsync($"{BaseUrl}dashboard?tab=2", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        await Page.Locator(".vpp-catalog-workspace").WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 60_000
        });

        var grid = Page.Locator(".vpp-catalog-grid:visible").Last;
        await grid.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        (await grid.Locator("tbody tr").CountAsync()).Should().BeGreaterThan(0);
    }
}
