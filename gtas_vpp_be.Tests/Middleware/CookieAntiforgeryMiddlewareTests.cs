using gtas_vpp_be.Authorization;
using gtas_vpp_be.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.Middleware;

public sealed class CookieAntiforgeryMiddlewareTests
{
    [Fact]
    public async Task UnsafeCookieRequestWithoutValidToken_IsRejected()
    {
        var nextCalled = false;
        var antiforgery = new Mock<IAntiforgery>();
        antiforgery
            .Setup(service => service.ValidateRequestAsync(It.IsAny<HttpContext>()))
            .ThrowsAsync(new AntiforgeryValidationException("missing token"));
        var middleware = new CookieAntiforgeryMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedPostContext();

        await middleware.InvokeAsync(context, antiforgery.Object);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task SafeCookieRequest_DoesNotRequireAntiforgeryToken()
    {
        var nextCalled = false;
        var antiforgery = new Mock<IAntiforgery>();
        var middleware = new CookieAntiforgeryMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedPostContext();
        context.Request.Method = HttpMethods.Get;

        await middleware.InvokeAsync(context, antiforgery.Object);

        Assert.True(nextCalled);
        antiforgery.Verify(
            service => service.ValidateRequestAsync(It.IsAny<HttpContext>()),
            Times.Never);
    }

    [Fact]
    public async Task UnsafeCookieRequestWithValidToken_Continues()
    {
        var nextCalled = false;
        var antiforgery = new Mock<IAntiforgery>();
        var middleware = new CookieAntiforgeryMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateAuthenticatedPostContext();

        await middleware.InvokeAsync(context, antiforgery.Object);

        Assert.True(nextCalled);
        antiforgery.Verify(
            service => service.ValidateRequestAsync(It.IsAny<HttpContext>()),
            Times.Once);
    }

    private static DefaultHttpContext CreateAuthenticatedPostContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.Cookie =
            $"{AppAuthenticationSchemes.SessionCookieName}=session";
        context.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity("cookie"));
        return context;
    }
}
