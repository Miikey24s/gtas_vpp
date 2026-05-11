using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var storedProcedureExecutor = new Mock<IStoredProcedureExecutor>();
        var loginData = new sp_Authentication_Login
        {
            UserID = 5615,
            UserLogin = "tester",
            FullName = "Test User",
            GroupId = Guid.NewGuid(),
            IsAdmin = true,
            DepartmentCode = "IT",
            DepartmentName = "IT Department"
        };
        storedProcedureExecutor.Setup(x => x.ExecuteSPAsync(
                "sp_Authen",
                "sp_Authen_Login",
                It.IsAny<object>(),
                It.IsAny<int?>()))
            .ReturnsAsync(new sp_ResDTO
            {
                IsSuccess = true,
                ResData = JsonConvert.SerializeObject(loginData)
            });
        var controller = CreateController(context, storedProcedureExecutor.Object);

        var result = await controller.Login(new AuthController.LoginRequest("tester", "secret"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<sp_Authentication_Login>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var storedProcedureExecutor = new Mock<IStoredProcedureExecutor>();
        storedProcedureExecutor.Setup(x => x.ExecuteSPAsync(
                "sp_Authen",
                "sp_Authen_Login",
                It.IsAny<object>(),
                It.IsAny<int?>()))
            .ReturnsAsync(new sp_ResDTO
            {
                IsSuccess = false,
                ErrorMess = "Login failed"
            });
        var controller = CreateController(context, storedProcedureExecutor.Object);

        var result = await controller.Login(new AuthController.LoginRequest("tester", "bad-password"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private static AuthController CreateController(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        IStoredProcedureExecutor storedProcedureExecutor)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "TEST_JWT_KEY_FOR_AUTH_CONTROLLER_TESTS_2026",
                ["JwtSettings:Issuer"] = "gtas_vpp_be",
                ["JwtSettings:Audience"] = "gtas_vpp_clients"
            })
            .Build();

        return new AuthController(
            context,
            storedProcedureExecutor,
            Mock.Of<IGenericRepository<P04_UserGroup>>(),
            Mock.Of<IGenericRepository<LEX02_CompanyDepartmentLocation>>(),
            Mock.Of<IUserNameResolver>(),
            unitOfWork.Object,
            configuration,
            Mock.Of<IPasswordEncoder>());
    }
}
