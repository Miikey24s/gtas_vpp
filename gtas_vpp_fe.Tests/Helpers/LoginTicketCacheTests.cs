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

    [Fact]
    public void ExpiredTicket_IsRemovedAndCannotBeUsed()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-07-16T00:00:00Z"));
        var cache = new LoginTicketCache(clock, TimeSpan.FromMinutes(2), capacity: 4);
        var ticketId = cache.Add(new sp_Authentication_Login { UserID = 1 }, rememberMe: false);

        clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Null(cache.Get(ticketId));
    }

    [Fact]
    public void Cache_EvictsOldestTicketWhenCapacityIsReached()
    {
        var clock = new ManualTimeProvider(DateTimeOffset.Parse("2026-07-16T00:00:00Z"));
        var cache = new LoginTicketCache(clock, TimeSpan.FromMinutes(10), capacity: 2);
        var first = cache.Add(new sp_Authentication_Login { UserID = 1 }, rememberMe: false);
        clock.Advance(TimeSpan.FromSeconds(1));
        var second = cache.Add(new sp_Authentication_Login { UserID = 2 }, rememberMe: false);
        clock.Advance(TimeSpan.FromSeconds(1));
        var third = cache.Add(new sp_Authentication_Login { UserID = 3 }, rememberMe: false);

        Assert.Null(cache.Get(first));
        Assert.Equal(2, cache.Get(second)?.loginData.UserID);
        Assert.Equal(3, cache.Get(third)?.loginData.UserID);
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; private set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;

        public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
    }
}
