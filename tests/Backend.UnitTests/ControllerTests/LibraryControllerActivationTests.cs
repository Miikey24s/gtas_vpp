using gtas_vpp_be.Controllers;
using gtas_vpp_be.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public sealed class LibraryControllerActivationTests
{
    [Fact]
    public void PriceListController_UsesTheWorkflowAwareConstructor()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(Mock.Of<IPriceListService>())
            .AddSingleton(Mock.Of<IPriceBookWorkflowService>())
            .AddSingleton(Mock.Of<IPriceListImportService>())
            .BuildServiceProvider();

        var controller = ActivatorUtilities.CreateInstance<VPPPriceListController>(provider);

        Assert.NotNull(controller);
    }

    [Fact]
    public void PriceController_UsesTheResolverAwareConstructor()
    {
        using var provider = new ServiceCollection()
            .AddSingleton(Mock.Of<IVPPPriceService>())
            .AddSingleton(Mock.Of<IPriceAsOfResolver>())
            .BuildServiceProvider();

        var controller = ActivatorUtilities.CreateInstance<VPPPriceController>(provider);

        Assert.NotNull(controller);
    }
}
