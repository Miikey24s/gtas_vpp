using FluentAssertions;
using gtas_vpp_fe.UITests.Core;
using gtas_vpp_fe.UITests.Pages.Auth;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_fe.UITests.Tests.Auth
{
    [Collection(ReadOnlyE2ECollection.Name)]
    public class LoginTests : TestBase, IAuthenticatedUiTest
    {
        [Fact]
        public async Task Login_Voi_Tai_Khoan_Hop_Le_Thanh_Cong()
        {
            // Arrange
            var loginPage = new LoginPage(Page);
            await loginPage.GotoAsync(BaseUrl);

            // Act
            await loginPage.LoginAsync(TestUsername, TestPassword);
            await loginPage.WaitForDashboardAsync();

            // Assert
            Page.Url.Should().NotContain("Login");
        }
    }
}
