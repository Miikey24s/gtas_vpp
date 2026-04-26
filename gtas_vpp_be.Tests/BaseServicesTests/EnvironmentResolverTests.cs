using System.Security.Claims;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using Xunit;

namespace gtas_vpp_be.Tests.BaseServicesTests;

public class EnvironmentResolverTests
{
    [Fact]
    public void GetEnvironment_ServerClaimTest_ReturnsTestEnv()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var httpContextAccessor = ServiceTestHelpers.CreateHttpContextAccessor(new Claim("Server", "Test"));
        var service = new BaseServices(factory.Object, httpContextAccessor);

        var result = service.GetEnvironment();

        Assert.Equal("TestEnv", result);
    }

    [Fact]
    public void GetEnvironment_ServerClaimLive_ReturnsLiveEnv()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var httpContextAccessor = ServiceTestHelpers.CreateHttpContextAccessor(new Claim("Server", "Live"));
        var service = new BaseServices(factory.Object, httpContextAccessor);

        var result = service.GetEnvironment();

        Assert.Equal("LiveEnv", result);
    }

    [Fact]
    public void GetEnvironment_NoClaims_ReturnsTestEnv()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var httpContextAccessor = ServiceTestHelpers.CreateHttpContextAccessor();
        var service = new BaseServices(factory.Object, httpContextAccessor);

        var result = service.GetEnvironment();

        Assert.Equal("TestEnv", result);
    }
}
