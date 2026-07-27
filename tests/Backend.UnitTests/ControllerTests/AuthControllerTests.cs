using System.Net;
using System.Text.Json;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_shared.DTOs.Req;
using gtas_vpp_shared.DTOs.Res.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_ValidCredentials_ReturnsTypedLoginResponse()
    {
        var expected = new AuthenticationResultDTO
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
            .ReturnsAsync((AuthenticationResultDTO?)null);
        var controller = CreateController(authentication.Object);

        var result = await controller.Login(
            new AuthenticationLoginRequest("tester", "bad-password"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_CookieMode_UsesHttpOnlyAuthenticationServiceAndDoesNotReturnBearerToken()
    {
        var expected = new AuthenticationResultDTO
        {
            UserID = 1_000_000_000,
            UserLogin = "tester",
            AccessToken = "must-not-reach-browser",
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
        var cookieAuthentication = new Mock<IAuthenticationService>();
        var controller = CreateController(authentication.Object, cookieAuthentication.Object);

        var result = await controller.Login(
            new AuthenticationLoginRequest("tester", "Valid-Pass1!"),
            CancellationToken.None,
            useCookies: true);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        Assert.Null(expected.AccessToken);
        cookieAuthentication.Verify(service => service.SignInAsync(
            It.IsAny<HttpContext>(),
            AppAuthenticationSchemes.Cookie,
            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties>()), Times.Once);
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

    private static AuthController CreateController(
        IAppAuthenticationService authentication,
        IAuthenticationService? cookieAuthentication = null)
    {
        var services = new ServiceCollection();
        if (cookieAuthentication is not null)
        {
            services.AddSingleton(cookieAuthentication);
        }
        var controller = new AuthController(authentication, Mock.Of<IPermissionService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    RequestServices = services.BuildServiceProvider()
                }
            }
        };
        controller.HttpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        return controller;
    }
}
