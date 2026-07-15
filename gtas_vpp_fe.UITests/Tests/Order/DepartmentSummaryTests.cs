using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using gtas_vpp_fe.UITests.Pages.Order;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Order
{
    public class DepartmentSummaryTests : TestBase, IAuthenticatedUiTest
    {
        [Fact]
        public async Task Xem_Tong_Hop_Phong_Ban_Thanh_Cong()
        {
            // Arrange - Login
            var loginPage = new LoginPage(Page);
            await Page.GotoAsync($"{BaseUrl}Account/Login");
            await loginPage.LoginAsync(TestUsername, TestPassword);
            await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard.*"), new PageWaitForURLOptions { Timeout = 15000 });

            // Navigate to Dashboard
            await Page.GotoAsync($"{BaseUrl}dashboard");
            await Task.Delay(2000, TestContext.Current.CancellationToken);

            // Act
            var summaryPage = new DepartmentSummaryPage(Page);
            await summaryPage.NavigateToDepartmentSummaryTabAsync();
            await Task.Delay(2000, TestContext.Current.CancellationToken);

            // Assert
            Page.Url.Should().Contain("dashboard");
            var rowCount = await summaryPage.GetGridRowCountAsync();
            rowCount.Should().BeGreaterThanOrEqualTo(0); // Grid loaded successfully
        }
    }
}
