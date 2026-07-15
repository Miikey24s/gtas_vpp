using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs;
using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Req;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
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

        var result = await controller.Login(new AuthenticationLoginRequest("tester", "secret"));

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<sp_Authentication_Login>(okResult.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.DoesNotContain(token.Claims, claim => claim.Type == "Server");
        Assert.Equal("gtas_vpp_be", token.Issuer);
        Assert.Equal(["gtas_vpp_test_clients"], token.Audiences);
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

        var result = await controller.Login(new AuthenticationLoginRequest("tester", "bad-password"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public void LoginRequest_LegacyServerProperty_IsIgnored()
    {
        const string legacyPayload =
            """{"username":"tester","password":"secret","server":"Live","selected_server":"Test"}""";

        var request = System.Text.Json.JsonSerializer.Deserialize<AuthenticationLoginRequest>(
            legacyPayload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("tester", request.Username);
        Assert.Equal("secret", request.Password);
        Assert.Null(typeof(AuthenticationLoginRequest).GetProperty("Server"));
    }

    private static AuthController CreateController(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        IStoredProcedureExecutor storedProcedureExecutor)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var configurationValues = new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "TEST_JWT_KEY_FOR_AUTH_CONTROLLER_TESTS_2026",
            ["JwtSettings:Issuer"] = "gtas_vpp_be",
            ["JwtSettings:Audience"] = "gtas_vpp_test_clients",
            ["DatabaseSettings:DefaultEnvironment"] = DatabaseBinding.TestEnvironment,
            ["ConnectionStrings:TestEnv"] =
                "Server=localhost;Database=GTAS_AUTH_TEST;Integrated Security=True"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
        var jwtSettings = JwtDeploymentSettings.Create(
            configuration,
            DatabaseBinding.Create(configuration));

        var controller = new AuthController(
            storedProcedureExecutor,
            Mock.Of<IGenericRepository<P04_UserGroup>>(),
            Mock.Of<IGenericRepository<LEX02_CompanyDepartmentLocation>>(),
            Mock.Of<IUserNameResolver>(),
            unitOfWork.Object,
            jwtSettings,
            Mock.Of<IPasswordEncoder>(),
            Mock.Of<IPermissionService>());

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

        return controller;
    }
}
