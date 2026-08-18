using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_be.Tests.LibraryTests;

public class LibraryControllerTests
{
    [Fact]
    public async Task GenericGet_SupplierTable_SearchesCanonicalNameWhenPaged()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var userNameResolver = new Mock<IUserNameResolver>();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierShortName = "VP",
            SupplierName = "Nhà cung cấp văn phòng",
            IsDeleted = false
        };
        context.Set<Supplier>().Add(supplier);
        await context.SaveChangesAsync();

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<Supplier>)))
            .Returns(new GenericRepository<Supplier>(unitOfWork.Object));
        userNameResolver.Setup(x => x.WithUserNamesAsync(It.IsAny<List<Supplier>>(), context))
            .ReturnsAsync((List<Supplier> list, gtas_vpp_be.Service.Helpers.Context.VPPContext ctx) => list);

        var controller = new LibraryController(
            userNameResolver.Object,
            unitOfWork.Object,
            dateTimeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GenericGet(
            tableCode: "suppliers",
            id: null,
            searchText: "văn phòng",
            lookupCategoryId: null,
            filter: null,
            skip: 0,
            top: 20,
            orderby: null,
            distinct: null,
            distinctFilter: null);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<SupplierResDTO>>(okResult.Value);
        Assert.Equal("Nhà cung cấp văn phòng", Assert.Single(list).SupplierName);
    }

    [Fact]
    public async Task GenericGet_SupplierTable_FiltersOutDeletedSuppliers()
    {
        // Arrange
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var userNameResolver = new Mock<IUserNameResolver>();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        // Real GenericRepository pointing to our UnitOfWork mock (which wraps context)
        var repository = new GenericRepository<Supplier>(unitOfWork.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<Supplier>)))
            .Returns(repository);

        // Pass entities as-is since they are mapped inside Controller
        userNameResolver.Setup(x => x.WithUserNamesAsync(It.IsAny<List<Supplier>>(), context))
            .ReturnsAsync((List<Supplier> list, gtas_vpp_be.Service.Helpers.Context.VPPContext ctx) => list);

        // Add 2 active and 2 deleted suppliers
        context.Set<Supplier>().AddRange(
            new Supplier { Id = Guid.NewGuid(), SupplierShortName = "Active 1", IsDeleted = false },
            new Supplier { Id = Guid.NewGuid(), SupplierShortName = "Active 2", IsDeleted = false },
            new Supplier { Id = Guid.NewGuid(), SupplierShortName = "Deleted 1", IsDeleted = true },
            new Supplier { Id = Guid.NewGuid(), SupplierShortName = "Deleted 2", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var controller = new LibraryController(userNameResolver.Object, unitOfWork.Object, dateTimeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act: Get "suppliers" (Suppliers)
        var result = await controller.GenericGet(
            tableCode: "suppliers",
            id: null,
            searchText: null,
            lookupCategoryId: null,
            filter: null,
            skip: null,
            top: null,
            orderby: null,
            distinct: null,
            distinctFilter: null
        );

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<SupplierResDTO>>(okResult.Value);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.SupplierShortName == "Active 1");
        Assert.Contains(list, x => x.SupplierShortName == "Active 2");
        Assert.DoesNotContain(list, x => x.SupplierShortName == "Deleted 1");
        Assert.DoesNotContain(list, x => x.SupplierShortName == "Deleted 2");
    }

    [Fact]
    public async Task GenericGet_SupplierTable_ProjectsActiveItemAndPriceListCounts()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 8, 14, 9, 0, 0);
        var supplierId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var supplier = new Supplier
        {
            Id = supplierId,
            SupplierShortName = "COUNT",
            SupplierName = "Count supplier",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        };
        context.Set<Supplier>().Add(supplier);
        await ServiceTestHelpers.SeedActiveVPPAsync(context, itemId);
        context.Set<PriceList>().Add(new PriceList
        {
            Id = priceListId,
            PriceListCode = "COUNT-2026",
            PriceListName = "Count price list",
            SupplierId = supplierId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            VppItemId = itemId,
            PriceListId = priceListId,
            Price = 1000,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<Supplier>)))
            .Returns(new GenericRepository<Supplier>(unitOfWork.Object));
        var userNameResolver = new Mock<IUserNameResolver>();
        userNameResolver.Setup(x => x.WithUserNamesAsync(It.IsAny<List<Supplier>>(), context))
            .ReturnsAsync((List<Supplier> list, gtas_vpp_be.Service.Helpers.Context.VPPContext _) => list);
        var controller = new LibraryController(
            userNameResolver.Object,
            unitOfWork.Object,
            new FakeDateTimeProvider(now))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GenericGet(
            tableCode: "suppliers",
            id: null,
            searchText: null,
            lookupCategoryId: null,
            filter: null,
            skip: 0,
            top: 20,
            orderby: null,
            distinct: null,
            distinctFilter: null);

        var rows = Assert.IsType<List<SupplierResDTO>>(Assert.IsType<OkObjectResult>(result).Value);
        var row = Assert.Single(rows);
        Assert.Equal(1, row.ItemCount);
        Assert.Equal(1, row.PriceListCount);
    }

    [Fact]
    public async Task GenericGet_VppItems_ProjectsSupplierCountFromDefaultPriceList()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 8, 14, 9, 0, 0);
        var itemId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, itemId);
        var item = await context.Set<VppItem>().FindAsync(itemId);
        context.Set<LookupValue>().Add(new LookupValue
        {
            Id = item!.UomId,
            Code = "EA",
            Value = "Cái",
            Sort = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        await ServiceTestHelpers.SeedDefaultPriceListAsync(context, (itemId, 1000));
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var userNameResolver = new Mock<IUserNameResolver>();
        userNameResolver.Setup(x => x.WithUserNamesAsync(It.IsAny<List<VppItemResDTO>>(), context))
            .ReturnsAsync((List<VppItemResDTO> list, gtas_vpp_be.Service.Helpers.Context.VPPContext _) => list);
        var controller = new LibraryController(
            userNameResolver.Object,
            unitOfWork.Object,
            new FakeDateTimeProvider(now))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.GenericGet(
            tableCode: "vpp-items",
            id: null,
            searchText: null,
            lookupCategoryId: null,
            filter: null,
            skip: 0,
            top: 20,
            orderby: null,
            distinct: null,
            distinctFilter: null);

        var rows = Assert.IsType<List<VppItemResDTO>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(1, Assert.Single(rows).SupplierCount);
    }

    [Fact]
    public async Task GenericGet_SupplierDistinct_ReturnsPagedValuesAndTotalHeader()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        context.Set<Supplier>().AddRange(
            new Supplier { Id = Guid.NewGuid(), SupplierName = "Alpha", SupplierShortName = "A1" },
            new Supplier { Id = Guid.NewGuid(), SupplierName = "Alpha", SupplierShortName = "A2" },
            new Supplier { Id = Guid.NewGuid(), SupplierName = "Beta", SupplierShortName = "B1" });
        await context.SaveChangesAsync();
        var controller = CreateController(context);

        var result = await controller.GenericGet(
            tableCode: "suppliers",
            id: null,
            searchText: null,
            lookupCategoryId: null,
            filter: null,
            skip: 0,
            top: 1,
            orderby: null,
            distinct: nameof(Supplier.SupplierName),
            distinctFilter: null);

        var rows = Assert.IsType<List<SupplierResDTO>>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Single(rows);
        Assert.Equal("2", controller.Response.Headers["X-Total-Count"].ToString());
        Assert.Contains(rows[0].SupplierName, new[] { "Alpha", "Beta" });
    }

    [Fact]
    public async Task GenericGetById_MissingVppItem_ReturnsNotFound()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var controller = CreateController(context);

        var result = await controller.GenericGetById("vpp-items", Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GenericCreate_Supplier_UsesServerAuditFieldsAndStartsActive()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 8, 18, 9, 30, 0, DateTimeKind.Utc);
        var controller = CreateMutationController<Supplier>(context, now, userId: 5615);
        using var payloadDocument = JsonDocument.Parse("""
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "supplierShortName": "NEW",
              "supplierName": "New supplier",
              "createdByUserId": 99,
              "updatedByUserId": 99,
              "isDeleted": true
            }
            """);

        var result = await controller.GenericCreate("suppliers", payloadDocument.RootElement.Clone());

        var response = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<SupplierResDTO>(response.Value);
        var entity = Assert.Single(context.Set<Supplier>());
        Assert.NotEqual(Guid.Empty, entity.Id);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(5615, entity.CreatedByUserId);
        Assert.Equal(5615, entity.UpdatedByUserId);
        Assert.Equal(now, entity.CreatedAtUtc);
        Assert.Equal(now, entity.UpdatedAtUtc);
        Assert.False(entity.IsDeleted);
    }

    [Fact]
    public async Task GenericPatch_Supplier_AllowsStatusButIgnoresAuditFields()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var createdAt = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 8, 18, 9, 30, 0, DateTimeKind.Utc);
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            SupplierShortName = "OLD",
            SupplierName = "Old supplier",
            CreatedByUserId = 7,
            UpdatedByUserId = 7,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt
        };
        context.Add(supplier);
        await context.SaveChangesAsync();
        var controller = CreateMutationController<Supplier>(context, now, userId: 5615);
        using var payloadDocument = JsonDocument.Parse("""
            {
              "supplierName": "Updated supplier",
              "createdByUserId": 99,
              "updatedByUserId": 99,
              "createdAtUtc": "2030-01-01T00:00:00Z",
              "isDeleted": true
            }
            """);

        var result = await controller.GenericPatch("suppliers", supplier.Id, payloadDocument.RootElement.Clone());

        Assert.IsType<OkObjectResult>(result);
        var entity = await context.Set<Supplier>().FindAsync(supplier.Id);
        Assert.NotNull(entity);
        Assert.Equal("Updated supplier", entity.SupplierName);
        Assert.True(entity.IsDeleted);
        Assert.Equal(7, entity.CreatedByUserId);
        Assert.Equal(createdAt, entity.CreatedAtUtc);
        Assert.Equal(5615, entity.UpdatedByUserId);
        Assert.Equal(now, entity.UpdatedAtUtc);
    }

    private static LibraryController CreateController(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var userNameResolver = new Mock<IUserNameResolver>();
        userNameResolver
            .Setup(x => x.WithUserNamesAsync(It.IsAny<List<Supplier>>(), context))
            .ReturnsAsync((List<Supplier> rows, gtas_vpp_be.Service.Helpers.Context.VPPContext _) => rows);
        return new LibraryController(
            userNameResolver.Object,
            unitOfWork.Object,
            new FakeDateTimeProvider(DateTime.UtcNow))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private static LibraryController CreateMutationController<TModel>(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        DateTime now,
        int userId)
        where TModel : class
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(provider => provider.GetService(typeof(IGenericRepository<TModel>)))
            .Returns(new GenericRepository<TModel>(unitOfWork.Object));
        var controller = new LibraryController(
            Mock.Of<IUserNameResolver>(),
            unitOfWork.Object,
            new FakeDateTimeProvider(now))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("UserID", userId.ToString())],
                        "TestAuth"))
                }
            }
        };
        return controller;
    }
}
