using System.Security.Claims;
using System.Text.Json;
using gtas_vpp_be.Middleware;
using gtas_vpp_shared.Constants;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace gtas_vpp_be.Tests.MiddlewareTests;

public sealed class PasswordChangeRequiredMiddlewareTests
{
    [Fact]
    public async Task RequiredPasswordChange_BlocksBusinessApiWithMachineReadableReason()
    {
        var nextCalled = false;
        var middleware = new PasswordChangeRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/VPPRequest/orders", mustChangePassword: true);

        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal("password-change-required", context.Response.Headers["X-Auth-Reason"]);
        context.Response.Body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("PASSWORD_CHANGE_REQUIRED", payload.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("/api/account/password/change")]
    [InlineData("/api/Auth/logout")]
    [InlineData("/api/Auth/me")]
    public async Task RequiredPasswordChange_AllowsOnlyLifecycleEndpoints(string path)
    {
        var nextCalled = false;
        var middleware = new PasswordChangeRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext(path, mustChangePassword: true);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task NormalSession_IsNotRestricted()
    {
        var nextCalled = false;
        var middleware = new PasswordChangeRequiredMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("/api/VPPRequest/orders", mustChangePassword: false);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    private static DefaultHttpContext CreateContext(string path, bool mustChangePassword)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "1000000001"),
                new Claim(AppClaimTypes.MustChangePassword, mustChangePassword.ToString())
            ], "Test")),
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Path = path;
        return context;
    }
}
