using System.Security.Claims;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.ControllerTests;

public class VPPRequestControllerTests
{
    [Fact]
    public async Task GetMyOrders_ValidUser_ReturnsOk()
    {
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetMyOrdersAsync(
                5615,
                It.IsAny<IEnumerable<int>?>(),
                It.IsAny<IEnumerable<int>?>(),
                It.IsAny<IEnumerable<int>?>()))
            .ReturnsAsync(new List<VPP01_RequestHeaderResDTO>
            {
                new() { Id = Guid.NewGuid(), Y = 2026, M = 4, Status = 1 }
            });
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.GetMyOrders(null, null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var orders = Assert.IsAssignableFrom<List<VPP01_RequestHeaderResDTO>>(okResult.Value);
        Assert.Single(orders);
    }

    [Fact]
    public async Task GetMyOrders_NullUserIdClaim_ReturnsUnauthorized()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object);

        var result = await controller.GetMyOrders(null, null, null, null, null, null);

        Assert.IsType<UnauthorizedObjectResult>(result);
        service.Verify(x => x.GetMyOrdersAsync(
                It.IsAny<int>(),
                It.IsAny<IEnumerable<int>?>(),
                It.IsAny<IEnumerable<int>?>(),
                It.IsAny<IEnumerable<int>?>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsOk()
    {
        var expected = new VPP01_RequestHeaderResDTO { Id = Guid.NewGuid(), Y = 2026, M = 4, Status = 1 };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.CreateOrderAsync(
                It.IsAny<VPP01_CreateReqDTO>(),
                5615,
                "IT",
                "77500"))
            .ReturnsAsync(expected);
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));
        var request = new VPP01_CreateReqDTO
        {
            Y = 2026,
            M = 4,
            Items = new List<VPP02_ItemReqDTO>
            {
                new() { VPPId = Guid.NewGuid(), Qty = 1 }
            }
        };

        var result = await controller.CreateOrder(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task GetOrderById_NotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync((VPP01_RequestHeaderResDTO?)null);
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.GetOrderById(orderId);

        Assert.IsType<NotFoundResult>(result);
    }

    private static VPPRequestController CreateController(IVPPRequestService service, params Claim[] claims)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var controller = new VPPRequestController(
            new ServiceCollection().BuildServiceProvider(),
            Mock.Of<IUserNameResolver>(),
            unitOfWork.Object,
            service);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };

        return controller;
    }
}
