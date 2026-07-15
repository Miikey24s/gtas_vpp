using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Auth
{
    public class LoginTests : TestBase, IAuthenticatedUiTest
    {
        [Fact]
        public async Task Login_Voi_Tai_Khoan_Hop_Le_Thanh_Cong()
        {
            // Arrange
            var loginPage = new LoginPage(Page);
            await Page.GotoAsync($"{BaseUrl}Account/Login");

            // Act
            await loginPage.LoginAsync(TestUsername, TestPassword);

            await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*dashboard.*"), new PageWaitForURLOptions { Timeout = 15000 });

            // Assert
            Page.Url.Should().NotContain("Login");
        }
    }
}
