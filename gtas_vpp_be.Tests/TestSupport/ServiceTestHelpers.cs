using System.Security.Claims;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace gtas_vpp_be.Tests.TestSupport;

internal static class ServiceTestHelpers
{
    private static readonly Guid FakeUomId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTime SeedNow = new(2026, 1, 1, 0, 0, 0);

    public static VPPContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<VPPContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new VPPContext(options);
    }

    public static async Task SeedActiveVPPAsync(VPPContext context, params Guid[] vppIds)
    {
        var idsToSeed = vppIds.Distinct().ToArray();
        var category = new L03_VPPCategory
        {
            Id = Guid.NewGuid(),
            VPPCategoryCode = "TEST-CATEGORY",
            VPPCategoryName = "Test Category",
            CreateUserId = 1,
            CreateDate = SeedNow,
            UpdateUserId = 1,
            UpdateDate = SeedNow,
            IsDeleted = false
        };

        context.Set<L03_VPPCategory>().Add(category);
        context.Set<L04_VPP>().AddRange(idsToSeed.Select(id => new L04_VPP
        {
            Id = id,
            VPPCode = $"TEST-VPP-{id:N}",
            VPPName = "Test VPP",
            UOMId = FakeUomId,
            VPPCategoryId = category.Id,
            CreateUserId = 1,
            CreateDate = SeedNow,
            UpdateUserId = 1,
            UpdateDate = SeedNow,
            IsDeleted = false
        }));

        await context.SaveChangesAsync();
    }

    public static async Task<Guid> SeedDefaultPriceListAsync(VPPContext context, params (Guid VPPId, decimal Price)[] items)
    {
        var priceListId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        foreach (var list in context.Set<L07_PriceList>().Where(x => x.IsDefault && !x.IsDeleted))
        {
            list.IsDefault = false;
            list.UpdateUserId = 1;
            list.UpdateDate = SeedNow;
        }

        context.Set<L07_PriceList>().Add(new L07_PriceList
        {
            Id = priceListId,
            PriceListCode = "DEFAULT",
            PriceListName = "Default Price List",
            IsDefault = true,
            CreateUserId = 1,
            CreateDate = SeedNow,
            UpdateUserId = 1,
            UpdateDate = SeedNow,
            IsDeleted = false
        });

        context.Set<L05_VPPSupplier>().Add(new L05_VPPSupplier
        {
            Id = supplierId,
            SupplierShortName = "TEST",
            SupplierName = "Test Supplier",
            CreateUserId = 1,
            CreateDate = SeedNow,
            UpdateUserId = 1,
            UpdateDate = SeedNow,
            IsDeleted = false
        });

        context.Set<L06_VPPSupplierMapping>().AddRange(items.Select(item => new L06_VPPSupplierMapping
        {
            Id = Guid.NewGuid(),
            L04_VPPId = item.VPPId,
            L05_VPPSupplierId = supplierId,
            L07_PriceListId = priceListId,
            Price = item.Price,
            IsDefault = true,
            CreateUserId = 1,
            CreateDate = SeedNow,
            UpdateUserId = 1,
            UpdateDate = SeedNow,
            IsDeleted = false
        }));

        await context.SaveChangesAsync();
        return priceListId;
    }

    public static IHttpContextAccessor CreateHttpContextAccessor(params Claim[] claims)
    {
        var accessor = new HttpContextAccessor();

        if (claims.Length > 0)
        {
            accessor.HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            };
        }

        return accessor;
    }

    public static Mock<IUnitOfWork> CreateUnitOfWorkMock(VPPContext context)
    {
        var unitOfWork = new Mock<IUnitOfWork>();

        unitOfWork.SetupGet(x => x.VPPContext).Returns(context);
        unitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.CommitAsync()).Returns(() => context.SaveChangesAsync());
        unitOfWork.Setup(x => x.Rollback());
        unitOfWork.Setup(x => x.RollbackAsync()).Returns(Task.CompletedTask);
        unitOfWork.Setup(x => x.SaveChangesAsync()).Returns(() => context.SaveChangesAsync());
        unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken));

        return unitOfWork;
    }

    public static Mock<IUnitOfWorkFactory> CreateUnitOfWorkFactoryMock(IUnitOfWork unitOfWork)
    {
        var factory = new Mock<IUnitOfWorkFactory>();
        factory.Setup(x => x.Create(It.IsAny<string>())).Returns(unitOfWork);
        return factory;
    }
}
