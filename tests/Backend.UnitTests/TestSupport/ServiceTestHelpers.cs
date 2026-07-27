using System.Security.Claims;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
        var category = new VppCategory
        {
            Id = Guid.NewGuid(),
            VppCategoryCode = "TEST-CATEGORY",
            VppCategoryName = "Test Category",
            CreatedByUserId = 1,
            CreatedAtUtc = SeedNow,
            UpdatedByUserId = 1,
            UpdatedAtUtc = SeedNow,
            IsDeleted = false
        };

        context.Set<VppCategory>().Add(category);
        context.Set<VppItem>().AddRange(idsToSeed.Select(id => new VppItem
        {
            Id = id,
            VppCode = $"TEST-VPP-{id:N}",
            VppName = "Test VPP",
            UomId = FakeUomId,
            VppCategoryId = category.Id,
            CreatedByUserId = 1,
            CreatedAtUtc = SeedNow,
            UpdatedByUserId = 1,
            UpdatedAtUtc = SeedNow,
            IsDeleted = false
        }));

        await context.SaveChangesAsync();
    }

    public static async Task<Guid> SeedDefaultPriceListAsync(VPPContext context, params (Guid VppId, decimal Price)[] items)
    {
        var priceListId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        foreach (var list in context.Set<PriceList>().Where(x => x.IsDefault && !x.IsDeleted))
        {
            list.IsDefault = false;
            list.UpdatedByUserId = 1;
            list.UpdatedAtUtc = SeedNow;
        }

        context.Set<PriceList>().Add(new PriceList
        {
            Id = priceListId,
            PriceListCode = "DEFAULT",
            PriceListName = "Default Price List",
            IsDefault = true,
            CreatedByUserId = 1,
            CreatedAtUtc = SeedNow,
            UpdatedByUserId = 1,
            UpdatedAtUtc = SeedNow,
            IsDeleted = false
        });

        context.Set<Supplier>().Add(new Supplier
        {
            Id = supplierId,
            SupplierShortName = "TEST",
            SupplierName = "Test Supplier",
            CreatedByUserId = 1,
            CreatedAtUtc = SeedNow,
            UpdatedByUserId = 1,
            UpdatedAtUtc = SeedNow,
            IsDeleted = false
        });

        context.Set<SupplierProductMapping>().AddRange(items.Select(item => new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            VppItemId = item.VppId,
            SupplierId = supplierId,
            PriceListId = priceListId,
            Price = item.Price,
            IsDefault = true,
            CreatedByUserId = 1,
            CreatedAtUtc = SeedNow,
            UpdatedByUserId = 1,
            UpdatedAtUtc = SeedNow,
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
        factory.Setup(x => x.Create()).Returns(unitOfWork);
        return factory;
    }

    public static IEnvironmentResolver CreateEnvironmentResolver(
        string environmentName = DatabaseBinding.TestEnvironment)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseSettings:DefaultEnvironment"] = environmentName,
                [$"ConnectionStrings:{environmentName}"] =
                    $"Server=localhost;Database=GTAS_{environmentName};Integrated Security=True;TrustServerCertificate=True"
            })
            .Build();

        return new EnvironmentResolver(DatabaseBinding.Create(configuration));
    }
}
