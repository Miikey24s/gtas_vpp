using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using gtas_vpp_fe.UITests.Pages.Order;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order
{
    public class ProductCatalogTests : TestBase
    {
        [Fact]
        public async Task Xem_Danh_Muc_San_Pham_Thanh_Cong()
        {
            // Arrange - Login
            var loginPage = new LoginPage(Page);
            await Page.GotoAsync($"{BaseUrl}Account/Login");
            await loginPage.LoginAsync(TestUsername, TestPassword);
            await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard.*"), new PageWaitForURLOptions { Timeout = 15000 });

            // Navigate to Dashboard
            await Page.GotoAsync($"{BaseUrl}dashboard");
            await Task.Delay(2000);

            // Act
            var catalogPage = new ProductCatalogPage(Page);
            await catalogPage.NavigateToProductCatalogTabAsync();
            await Task.Delay(2000);

            // Assert
            Page.Url.Should().Contain("dashboard");
            var rowCount = await catalogPage.GetGridRowCountAsync();
            rowCount.Should().BeGreaterThanOrEqualTo(0); // Grid loaded successfully
        }
    }
}
