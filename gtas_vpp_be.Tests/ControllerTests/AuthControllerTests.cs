using System.Net;
using System.Text.Json;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsTypedLoginResponse()
    {
        var expected = new sp_Authentication_Login
        {
            UserID = 1_000_000_000,
            UserLogin = "tester",
            FullName = "Test User",
            AccessToken = "synthetic-token",
            AccessTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(30),
            SessionVersion = 1,
            AccountStatus = "Active"
        };
        var authentication = new Mock<IAppAuthenticationService>();
        authentication.Setup(x => x.AuthenticateAsync(
                "tester",
                "Valid-Pass1!",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(authentication.Object);

        var result = await controller.Login(
            new AuthenticationLoginRequest("tester", "Valid-Pass1!"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsGenericUnauthorized()
    {
        var authentication = new Mock<IAppAuthenticationService>();
        authentication.Setup(x => x.AuthenticateAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((sp_Authentication_Login?)null);
        var controller = CreateController(authentication.Object);

        var result = await controller.Login(
            new AuthenticationLoginRequest("tester", "bad-password"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_MissingInput_ReturnsBadRequestWithoutCallingAuthenticator()
    {
        var authentication = new Mock<IAppAuthenticationService>();
        var controller = CreateController(authentication.Object);

        var result = await controller.Login(
            new AuthenticationLoginRequest("", ""),
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        authentication.VerifyNoOtherCalls();
    }

    [Fact]
    public void LoginRequest_LegacyServerProperty_IsIgnored()
    {
        const string legacyPayload =
            """{"username":"tester","password":"secret","server":"Live","selected_server":"Test"}""";

        var request = JsonSerializer.Deserialize<AuthenticationLoginRequest>(
            legacyPayload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(request);
        Assert.Equal("tester", request.Username);
        Assert.Equal("secret", request.Password);
        Assert.Null(typeof(AuthenticationLoginRequest).GetProperty("Server"));
    }

    private static AuthController CreateController(IAppAuthenticationService authentication)
    {
        var controller = new AuthController(authentication, Mock.Of<IPermissionService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        return controller;
    }
}
