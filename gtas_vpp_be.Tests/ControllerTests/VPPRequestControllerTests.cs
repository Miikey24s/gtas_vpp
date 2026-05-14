using System.Security.Claims;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers.Context;
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

    [Fact]
    public async Task GetProducts_ReturnsFlattenedProducts_WithActiveSupplierCountOnly()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 4, 8, 0, 0);
        var uomId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        context.Add(new L02_ClassDetail
        {
            Id = uomId,
            ClassDetailCode = "BOX",
            ClassDetailValue = "Box",
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now
        });

        context.Add(new L03_VPPCategory
        {
            Id = categoryId,
            VPPCategoryCode = "CAT",
            VPPCategoryName = "Category",
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now
        });

        context.Add(new L04_VPP
        {
            Id = productId,
            VPPCode = "VPP-001",
            VPPName = "Blue Pen",
            UOMId = uomId,
            VPPCategoryId = categoryId,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now
        });

        context.AddRange(
            new L06_VPPSupplierMapping
            {
                Id = Guid.NewGuid(),
                L04_VPPId = productId,
                L05_VPPSupplierId = Guid.NewGuid(),
                Price = 100,
                CreateUserId = 1,
                CreateDate = now,
                UpdateUserId = 1,
                UpdateDate = now
            },
            new L06_VPPSupplierMapping
            {
                Id = Guid.NewGuid(),
                L04_VPPId = productId,
                L05_VPPSupplierId = Guid.NewGuid(),
                Price = 200,
                IsDeleted = true,
                CreateUserId = 1,
                CreateDate = now,
                UpdateUserId = 1,
                UpdateDate = now
            });

        await context.SaveChangesAsync();

        var controller = CreateController(Mock.Of<IVPPRequestService>(), context, new Claim("UserID", "5615"));

        var result = await controller.GetProducts(categoryId, "  Blue ", null, null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
        var product = Assert.Single(products);

        Assert.Equal(productId, GetPropertyValue<Guid>(product, "Id"));
        Assert.Equal("VPP-001", GetPropertyValue<string>(product, "VPPCode"));
        Assert.Equal("Blue Pen", GetPropertyValue<string>(product, "VPPName"));
        Assert.Equal("CAT", GetPropertyValue<string>(product, "VPPCategoryCode"));
        Assert.Equal("Category", GetPropertyValue<string>(product, "VPPCategoryName"));
        Assert.Equal("BOX", GetPropertyValue<string>(product, "UOMCode"));
        Assert.Equal("Box", GetPropertyValue<string>(product, "UOMName"));
        Assert.Equal("1", controller.Response.Headers["X-Total-Count"].ToString());
    }

    [Fact]
    public async Task GetProducts_LoadDataRequest_AppliesPagingAndSetsTotalCountHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 4, 8, 0, 0);
        var uomId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        context.Add(new L02_ClassDetail
        {
            Id = uomId,
            ClassDetailCode = "PCS",
            ClassDetailValue = "Piece",
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now
        });

        context.Add(new L03_VPPCategory
        {
            Id = categoryId,
            VPPCategoryCode = "CAT",
            VPPCategoryName = "Category",
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now
        });

        context.AddRange(
            CreateProduct(Guid.NewGuid(), "VPP-001", "Alpha", uomId, categoryId, now),
            CreateProduct(Guid.NewGuid(), "VPP-002", "Bravo", uomId, categoryId, now));

        await context.SaveChangesAsync();

        var controller = CreateController(Mock.Of<IVPPRequestService>(), context, new Claim("UserID", "5615"));

        var result = await controller.GetProducts(categoryId, null, null, 0, 1, "VPPCode desc", null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
        var product = Assert.Single(products);

        Assert.Equal("2", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Equal("VPP-002", GetPropertyValue<string>(product, "VPPCode"));
    }

    [Fact]
    public async Task GetOrderFilterValues_ExcludesCurrentColumnSelectionFromPopupScope()
    {
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetAllOrdersAsync(null, null, null, null))
            .ReturnsAsync(new List<VPP01_RequestHeaderResDTO>
            {
                new() { Id = Guid.NewGuid(), DepartmentCode = "IT", Status = 1 },
                new() { Id = Guid.NewGuid(), DepartmentCode = "IT", Status = 7 },
                new() { Id = Guid.NewGuid(), DepartmentCode = "HR", Status = 1 },
                new() { Id = Guid.NewGuid(), DepartmentCode = "HR", Status = 8 }
            });

        var controller = CreateController(service.Object, new Claim("UserID", "5615"));
        const string filtersJson = "[{\"property\":\"DepartmentCode\",\"values\":[\"IT\"]}]";

        var result = await controller.GetOrderFilterValues(
            "StatusText",
            null,
            null,
            null,
            null,
            null,
            "StatusText == \"Submitted\"",
            filtersJson,
            null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var values = Assert.IsAssignableFrom<IEnumerable<Dictionary<string, object?>>>(okResult.Value)
            .Select(item => item["StatusText"]?.ToString())
            .ToList();

        Assert.Equal(new[] { "Submitted", "Approved" }, values);
    }

    private static VPPRequestController CreateController(IVPPRequestService service, params Claim[] claims)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        return CreateController(service, context, claims);
    }

    private static VPPRequestController CreateController(IVPPRequestService service, VPPContext context, params Claim[] claims)
    {
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

    private static T GetPropertyValue<T>(object instance, string propertyName)
        => (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

    private static L04_VPP CreateProduct(Guid id, string code, string name, Guid uomId, Guid categoryId, DateTime now)
        => new()
        {
            Id = id,
            VPPCode = code,
            VPPName = name,
            UOMId = uomId,
            VPPCategoryId = categoryId,
            CreateUserId = 1,
            CreateDate = now,
            UpdateUserId = 1,
            UpdateDate = now
        };
}
