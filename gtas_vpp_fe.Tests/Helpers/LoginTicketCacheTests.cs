using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Auth;
using Xunit;

namespace gtas_vpp_fe.Tests.Helpers;

public sealed class LoginTicketCacheTests
{
    [Fact]
    public void AddThenGet_ReturnsTicketOnceWithoutEnvironmentInput()
    {
        var cache = new LoginTicketCache();
        var loginData = new sp_Authentication_Login
        {
            UserID = 42,
            UserLogin = "test-user"
        };

        var ticketId = cache.Add(loginData, rememberMe: true);

        var ticket = cache.Get(ticketId);
        Assert.NotNull(ticket);
        Assert.Same(loginData, ticket.Value.loginData);
        Assert.True(ticket.Value.rememberMe);
        Assert.Null(cache.Get(ticketId));

        var addParameters = typeof(LoginTicketCache)
            .GetMethod(nameof(LoginTicketCache.Add))!
            .GetParameters();
        Assert.DoesNotContain(
            addParameters,
            parameter => parameter.Name?.Contains("server", StringComparison.OrdinalIgnoreCase) == true
                || parameter.Name?.Contains("environment", StringComparison.OrdinalIgnoreCase) == true);
    }
}
