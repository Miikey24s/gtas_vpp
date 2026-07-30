using System.Security.Claims;
using gtas_vpp_be.Authorization;
using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Notifications;
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
    public async Task GetOrderById_OtherCompany_ReturnsForbid()
    {
        var service = new Mock<IVPPRequestService>();
        var orderId = Guid.NewGuid();
        service.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync(new VppRequestResDTO
            {
                Id = orderId,
                CreatedByUserId = 99,
                DepartmentCode = "HR",
                MemberCompanyCode = "88000"
            });
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.GetOrderById(orderId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMyOrders_ValidUser_ReturnsOk()
    {
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetMyOrdersAsync(
                5615,
                It.IsAny<IEnumerable<int>?>(),
                It.IsAny<IEnumerable<int>?>(),
                It.IsAny<IEnumerable<int>?>()))
            .ReturnsAsync(new List<VppRequestResDTO>
            {
                new() { Id = Guid.NewGuid(), Year = 2026, Month = 4, Status = 1 }
            });
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.GetMyOrders(null, null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var orders = Assert.IsAssignableFrom<List<VppRequestResDTO>>(okResult.Value);
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
    public async Task GetMyOrderHistorySummary_ValidRange_ReturnsTypedSummary()
    {
        var expected = new VppOrderHistorySummaryResDTO
        {
            PeriodCount = 2,
            TotalOrders = 3,
            TotalLines = 12,
            TotalQuantity = 30
        };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetMyOrderHistorySummaryAsync(5615, 202601, 202612))
            .ReturnsAsync(expected);
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.GetMyOrderHistorySummary(202601, 202612);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetMyOrderHistory_InvalidPeriod_ReturnsBadRequest()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.GetMyOrderHistory(
            202613, null, null, null, null, null, 0, 6);

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(x => x.GetMyOrderHistoryPageAsync(
            It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(),
            It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task GetMyOrderHistory_SetsTotalCountHeader()
    {
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetMyOrderHistoryPageAsync(
                5615, null, null, null, null, null, null, 0, 4))
            .ReturnsAsync((new List<VppRequestResDTO>
            {
                new() { Id = Guid.NewGuid(), Year = 2026, Month = 7 }
            }, 9));
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.GetMyOrderHistory(null, null, null, null, null, null, 0, 4);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("9", controller.Response.Headers["X-Total-Count"].ToString());
    }

    [Fact]
    public async Task GetDepartmentOrderHistorySummary_UsesClaimScope()
    {
        var expected = new VppOrderHistorySummaryResDTO { TotalOrders = 2, TotalQuantity = 18 };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetDepartmentOrderHistorySummaryAsync("IT", "77500", 202601, 202612))
            .ReturnsAsync(expected);
        var controller = CreateController(
            service.Object,
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.GetDepartmentOrderHistorySummary(202601, 202612);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
    }

    [Fact]
    public async Task GetDepartmentOrderHistory_SetsTotalCountAndIgnoresClientDepartment()
    {
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetDepartmentOrderHistoryPageAsync(
                "IT", "77500", null, null, null, null, null, null, 0, 6))
            .ReturnsAsync((new List<VppRequestResDTO> { new() { DepartmentCode = "IT" } }, 7));
        var controller = CreateController(
            service.Object,
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.GetDepartmentOrderHistory(
            null, null, null, null, null, null, 0, 6);

        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("7", controller.Response.Headers["X-Total-Count"].ToString());
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsOk()
    {
        var expected = new VppRequestResDTO { Id = Guid.NewGuid(), Year = 2026, Month = 4, Status = 1 };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.CreateOrderAsync(
                It.IsAny<VppRequestCreateReqDTO>(),
                5615,
                "IT",
                "77500"))
            .ReturnsAsync(expected);
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));
        var request = new VppRequestCreateReqDTO
        {
            Year = 2026,
            Month = 4,
            Items = new List<VppRequestDetailItemReqDTO>
            {
                new() { VppId = Guid.NewGuid(), Qty = 1 }
            }
        };

        var result = await controller.CreateOrder(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, okResult.Value);
    }

    [Fact]
    public async Task GetOrderHistory_OtherCompany_ReturnsForbid()
    {
        var orderId = Guid.NewGuid();
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetOrderHistoryAsync(orderId))
            .ReturnsAsync(new VppRequestHistoryResDTO
            {
                CurrentRequestId = orderId,
                Revisions =
                [
                    new VppRequestResDTO
                    {
                        Id = orderId,
                        CreatedByUserId = 99,
                        DepartmentCode = "HR",
                        MemberCompanyCode = "88000"
                    }
                ]
            });
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.GetOrderHistory(orderId);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task UpdateOrder_MissingRowVersion_ReturnsBadRequest()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.UpdateOrder(Guid.NewGuid(), new VppRequestUpdateReqDTO());

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(x => x.UpdateOrderAsync(It.IsAny<VppRequestUpdateReqDTO>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_MissingRowVersion_ReturnsBadRequest()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.CancelOrder(Guid.NewGuid(), new VppRequestCancelReqDTO());

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(x => x.CancelOrderAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<VppRequestCancelReqDTO>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrder_Owner_ForwardsConcurrencyEnvelope()
    {
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };
        var request = new VppRequestCancelReqDTO
        {
            RowVersion = rowVersion,
            Reason = "No longer required",
            IdempotencyKey = "cancel-controller-1"
        };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync(new VppRequestResDTO
            {
                Id = orderId,
                CreatedByUserId = 5615,
                DepartmentCode = "IT",
                MemberCompanyCode = "77500"
            });
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.CancelOrder(orderId, request);

        Assert.IsType<OkResult>(result);
        service.Verify(x => x.CancelOrderAsync(orderId, 5615, request), Times.Once);
    }

    [Fact]
    public async Task RestoreCancelledOrder_MissingRowVersion_ReturnsBadRequest()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object, new Claim("UserID", "5615"));

        var result = await controller.RestoreCancelledOrder(
            Guid.NewGuid(),
            new VppRequestRestoreReqDTO());

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(x => x.RestoreCancelledOrderAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<VppRequestRestoreReqDTO>()), Times.Never);
    }

    [Fact]
    public async Task RestoreCancelledOrder_Owner_ForwardsRecoveryCommand()
    {
        var orderId = Guid.NewGuid();
        var request = new VppRequestRestoreReqDTO
        {
            RowVersion = new byte[] { 1, 2, 3 },
            IdempotencyKey = "restore-controller-1"
        };
        var expected = new VppRequestResDTO { Id = Guid.NewGuid() };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync(new VppRequestResDTO
            {
                Id = orderId,
                CreatedByUserId = 5615,
                DepartmentCode = "IT",
                MemberCompanyCode = "77500"
            });
        service.Setup(x => x.RestoreCancelledOrderAsync(orderId, 5615, request))
            .ReturnsAsync(expected);
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.RestoreCancelledOrder(orderId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        service.Verify(x => x.RestoreCancelledOrderAsync(orderId, 5615, request), Times.Once);
    }

    [Fact]
    public async Task RecreateCancelledOrder_Owner_ForwardsNewContents()
    {
        var orderId = Guid.NewGuid();
        var request = new VppRequestRecreateReqDTO
        {
            RowVersion = new byte[] { 1, 2, 3 },
            IdempotencyKey = "recreate-controller-1",
            Items = [new VppRequestDetailItemReqDTO { VppId = Guid.NewGuid(), Qty = 2 }]
        };
        var expected = new VppRequestResDTO { Id = Guid.NewGuid() };
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync(new VppRequestResDTO
            {
                Id = orderId,
                CreatedByUserId = 5615,
                DepartmentCode = "IT",
                MemberCompanyCode = "77500"
            });
        service.Setup(x => x.RecreateCancelledOrderAsync(orderId, 5615, request))
            .ReturnsAsync(expected);
        var controller = CreateController(
            service.Object,
            new Claim("UserID", "5615"),
            new Claim("DepartmentCode", "IT"),
            new Claim("MemberCompanyCode", "77500"));

        var result = await controller.RecreateCancelledOrder(orderId, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        service.Verify(x => x.RecreateCancelledOrderAsync(orderId, 5615, request), Times.Once);
    }

    [Fact]
    public async Task ApproveAdditionalOrder_MissingRowVersion_ReturnsBadRequest()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object, new Claim("UserID", "9001"));

        var result = await controller.ApproveAdditionalOrder(Guid.NewGuid(), new ApproveOrderReqDTO());

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(x => x.ApproveAdditionalOrderAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<string?>(),
            It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task RejectAdditionalOrder_MissingRowVersion_ReturnsBadRequest()
    {
        var service = new Mock<IVPPRequestService>();
        var controller = CreateController(service.Object, new Claim("UserID", "9001"));

        var result = await controller.RejectAdditionalOrder(Guid.NewGuid(), new RejectOrderReqDTO
        {
            Reason = "Budget exceeded"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        service.Verify(x => x.RejectAdditionalOrderAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<byte[]>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GetOrderById_NotFound_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetOrderByIdAsync(orderId))
            .ReturnsAsync((VppRequestResDTO?)null);
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
        var supplierId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();

        context.Add(new LookupValue
        {
            Id = uomId,
            Code = "BOX",
            Value = "Box",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.Add(new VppCategory
        {
            Id = categoryId,
            VppCategoryCode = "CAT",
            VppCategoryName = "Category",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.Add(new VppItem
        {
            Id = productId,
            VppCode = "VPP-001",
            VppName = "Blue Pen",
            UomId = uomId,
            VppCategoryId = categoryId,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.Add(new Supplier
        {
            Id = supplierId,
            SupplierShortName = "VPP_HCM",
            SupplierName = "HCM Supplier",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.Add(new PriceList
        {
            Id = priceListId,
            PriceListCode = "DEFAULT",
            PriceListName = "Default Price List",
            IsDefault = true,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.AddRange(
            new SupplierProductMapping
            {
                Id = Guid.NewGuid(),
                VppItemId = productId,
                SupplierId = supplierId,
                PriceListId = priceListId,
                Price = 100,
                IsDefault = true,
                CreatedByUserId = 1,
                CreatedAtUtc = now,
                UpdatedByUserId = 1,
                UpdatedAtUtc = now
            },
            new SupplierProductMapping
            {
                Id = Guid.NewGuid(),
                VppItemId = productId,
                SupplierId = Guid.NewGuid(),
                PriceListId = priceListId,
                Price = 200,
                IsDeleted = true,
                CreatedByUserId = 1,
                CreatedAtUtc = now,
                UpdatedByUserId = 1,
                UpdatedAtUtc = now
            });

        await context.SaveChangesAsync();

        var controller = CreateController(Mock.Of<IVPPRequestService>(), context, new Claim("UserID", "5615"));

        var result = await controller.GetProducts(categoryId, "  Blue ", null, null, null, null, null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
        var product = Assert.Single(products);

        Assert.Equal(productId, GetPropertyValue<Guid>(product, "Id"));
        Assert.Equal("VPP-001", GetPropertyValue<string>(product, "VppCode"));
        Assert.Equal("Blue Pen", GetPropertyValue<string>(product, "VppName"));
        Assert.Equal("CAT", GetPropertyValue<string>(product, "VppCategoryCode"));
        Assert.Equal("Category", GetPropertyValue<string>(product, "VppCategoryName"));
        Assert.Equal("BOX", GetPropertyValue<string>(product, "UomCode"));
        Assert.Equal("Box", GetPropertyValue<string>(product, "UomName"));
        Assert.Equal(1, GetPropertyValue<int>(product, "SupplierCount"));
        Assert.Equal(100m, GetPropertyValue<decimal?>(product, "DefaultPrice"));
        Assert.Equal("HCM Supplier", GetPropertyValue<string>(product, "DefaultSupplierName"));
        Assert.Equal("1", controller.Response.Headers["X-Total-Count"].ToString());
    }

    [Fact]
    public async Task GetProducts_LoadDataRequest_AppliesPagingAndSetsTotalCountHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 4, 8, 0, 0);
        var uomId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        context.Add(new LookupValue
        {
            Id = uomId,
            Code = "PCS",
            Value = "Piece",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.Add(new VppCategory
        {
            Id = categoryId,
            VppCategoryCode = "CAT",
            VppCategoryName = "Category",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        });

        context.AddRange(
            CreateProduct(Guid.NewGuid(), "VPP-001", "Alpha", uomId, categoryId, now),
            CreateProduct(Guid.NewGuid(), "VPP-002", "Bravo", uomId, categoryId, now));

        await context.SaveChangesAsync();

        var controller = CreateController(Mock.Of<IVPPRequestService>(), context, new Claim("UserID", "5615"));

        var result = await controller.GetProducts(categoryId, null, null, 0, 1, "VppCode desc", null, null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var products = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value).ToList();
        var product = Assert.Single(products);

        Assert.Equal("2", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Equal("VPP-002", GetPropertyValue<string>(product, "VppCode"));
    }

    [Fact]
    public async Task GetOrderFilterValues_ExcludesCurrentColumnSelectionFromPopupScope()
    {
        var service = new Mock<IVPPRequestService>();
        service.Setup(x => x.GetAllOrdersAsync(null, null, null, null, It.IsAny<string?>()))
            .ReturnsAsync(new List<VppRequestResDTO>
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
            null, // years
            null, // months
            null, // statuses
            null, // Code
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
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(service => service.HasPermissionAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var controller = new VPPRequestController(
            new ServiceCollection().BuildServiceProvider(),
            Mock.Of<IUserNameResolver>(),
            unitOfWork.Object,
            service,
            permissionService.Object,
            Mock.Of<IAppNotificationService>(),
            new VppCatalogService(unitOfWork.Object, new FakeDateTimeProvider(DateTime.UtcNow)));

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

    private static VppItem CreateProduct(Guid id, string code, string name, Guid uomId, Guid categoryId, DateTime now)
        => new()
        {
            Id = id,
            VppCode = code,
            VppName = name,
            UomId = uomId,
            VppCategoryId = categoryId,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now
        };
}
