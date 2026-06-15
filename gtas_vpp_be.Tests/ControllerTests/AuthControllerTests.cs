using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
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
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Equal("Test", token.Claims.Single(claim => claim.Type == "Server").Value);
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

    [Fact]
    public async Task Login_TestServerInLiveOnlyEnvironment_ReturnsBadRequest()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var storedProcedureExecutor = new Mock<IStoredProcedureExecutor>();
        var controller = CreateController(
            context,
            storedProcedureExecutor.Object,
            defaultEnvironment: "LiveEnv",
            includeTestEnvironment: false,
            includeLiveEnvironment: true);

        var result = await controller.Login(new AuthController.LoginRequest("tester", "secret", "Test"));

        Assert.IsType<BadRequestObjectResult>(result);
        storedProcedureExecutor.VerifyNoOtherCalls();
    }

    private static AuthController CreateController(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        IStoredProcedureExecutor storedProcedureExecutor,
        string defaultEnvironment = "TestEnv",
        bool includeTestEnvironment = true,
        bool includeLiveEnvironment = false)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var configurationValues = new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "TEST_JWT_KEY_FOR_AUTH_CONTROLLER_TESTS_2026",
            ["JwtSettings:Issuer"] = "gtas_vpp_be",
            ["JwtSettings:Audience"] = "gtas_vpp_clients",
            ["DatabaseSettings:DefaultEnvironment"] = defaultEnvironment
        };
        if (includeTestEnvironment)
        {
            configurationValues["ConnectionStrings:TestEnv"] = "Server=test;Database=test;";
        }
        if (includeLiveEnvironment)
        {
            configurationValues["ConnectionStrings:LiveEnv"] = "Server=live;Database=live;";
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var controller = new AuthController(
            context,
            storedProcedureExecutor,
            Mock.Of<IGenericRepository<P04_UserGroup>>(),
            Mock.Of<IGenericRepository<LEX02_CompanyDepartmentLocation>>(),
            Mock.Of<IUserNameResolver>(),
            unitOfWork.Object,
            configuration,
            Mock.Of<IPasswordEncoder>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        return controller;
    }
}
