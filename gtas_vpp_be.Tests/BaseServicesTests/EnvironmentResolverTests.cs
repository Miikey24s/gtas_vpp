using System.Security.Claims;
using gtas_vpp_be.Service.Helpers;
using Xunit;

namespace gtas_vpp_be.Tests.BaseServicesTests;

public class EnvironmentResolverTests
{
    [Fact]
    public void GetEnvironment_ServerClaimTest_ReturnsTestEnv()
    {
        var resolver = new EnvironmentResolver();

        var result = resolver.Resolve(new[] { new Claim("Server", "Test") });

        Assert.Equal("TestEnv", result);
    }

    [Fact]
    public void GetEnvironment_ServerClaimLive_ReturnsLiveEnv()
    {
        var resolver = new EnvironmentResolver();

        var result = resolver.Resolve(new[] { new Claim("Server", "Live") });

        Assert.Equal("LiveEnv", result);
    }

    [Fact]
    public void GetEnvironment_NoClaims_ReturnsTestEnv()
    {
        var resolver = new EnvironmentResolver();

        var result = resolver.Resolve(null);

        Assert.Equal("TestEnv", result);
    }
}
