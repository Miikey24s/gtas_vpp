using System.Text.Json;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.CatalogTests;

public sealed class CatalogControllerTests
{
    [Fact]
    public async Task GetItems_ReturnsTypedPageAndTotalCountHeader()
    {
        var service = new Mock<IVppCatalogService>();
        service.Setup(x => x.QueryItemsAsync(
                null, "alpha", null, 0, 20, null, null, null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<VppItemResDTO>)new List<VppItemResDTO>
            {
                new() { Id = Guid.NewGuid(), VppCode = "VPP-001", VppName = "Alpha" }
            }, 1));

        var controller = new VppCatalogController(service.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GetItems(null, "alpha", null, null, null, null, null, null, true);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<VppItemResDTO>>(ok.Value));
        Assert.Equal("1", controller.Response.Headers["X-Total-Count"].ToString());
    }

    [Fact]
    public async Task LegacyLibraryL04Mutations_ReturnMethodNotAllowedProblem()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var controller = new LibraryController(
            new Mock<IServiceProvider>().Object,
            Mock.Of<IUserNameResolver>(),
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object,
            new FakeDateTimeProvider(DateTime.UtcNow));

        var result = await controller.GenericDelete("vpp-items", Guid.NewGuid());

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status405MethodNotAllowed, objectResult.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal("CatalogTypedEndpointRequired", problem.Extensions["errorCode"]);
    }
}
