using System.Reflection;
using System.Security.Claims;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class AccountControllerTests
{
    [Fact]
    public async Task Register_AcceptedLifecycleResult_ReturnsGeneric202()
    {
        var lifecycle = new Mock<IAccountLifecycleService>();
        lifecycle.Setup(service => service.RegisterAsync(
                It.IsAny<AccountRegistrationReqDTO>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountLifecycleResult.Accepted("Accepted."));
        var controller = CreateController(lifecycle.Object);

        var result = await controller.Register(new AccountRegistrationReqDTO(), CancellationToken.None);

        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status202Accepted, response.StatusCode);
    }

    [Theory]
    [InlineData(nameof(AccountController.Register), "account-register")]
    [InlineData(nameof(AccountController.ConfirmEmail), "account-confirm")]
    [InlineData(nameof(AccountController.RequestPasswordRecovery), "account-recovery")]
    [InlineData(nameof(AccountController.ResetPassword), "account-recovery")]
    [InlineData(nameof(AccountController.ChangePassword), "account-password")]
    [InlineData(nameof(AccountController.AdminResetPassword), "account-password")]
    public void SensitiveEndpoint_HasNamedRateLimitPolicy(string methodName, string expectedPolicy)
    {
        var method = typeof(AccountController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        var attribute = Assert.Single(method!.GetCustomAttributes<EnableRateLimitingAttribute>());
        Assert.Equal(expectedPolicy, attribute.PolicyName);
    }

    [Theory]
    [InlineData(nameof(AccountController.Register))]
    [InlineData(nameof(AccountController.ConfirmEmail))]
    [InlineData(nameof(AccountController.RequestPasswordRecovery))]
    [InlineData(nameof(AccountController.ResetPassword))]
    public void PublicLifecycleEndpoint_IsExplicitlyAnonymous(string methodName)
    {
        var method = typeof(AccountController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(method!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    [Theory]
    [InlineData(nameof(AccountController.Activate))]
    [InlineData(nameof(AccountController.AdminResetPassword))]
    public void AdministrationEndpoint_RequiresPermissionManage(string methodName)
    {
        var method = typeof(AccountController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);

        var authorization = Assert.Single(method!.GetCustomAttributes<AuthorizeAttribute>());
        Assert.Equal(Permissions.PermissionManage, authorization.Policy);
    }

    private static AccountController CreateController(IAccountLifecycleService lifecycleService)
    {
        var controller = new AccountController(lifecycleService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(AppClaimTypes.UserId, "1000000001")
                    ], "Test"))
                }
            }
        };
        return controller;
    }
}
