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
            CreateDate = DateTime.UtcNow,
            UpdateUserId = 1,
            UpdateDate = DateTime.UtcNow,
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
            CreateDate = DateTime.UtcNow,
            UpdateUserId = 1,
            UpdateDate = DateTime.UtcNow,
            IsDeleted = false
        }));

        await context.SaveChangesAsync();
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
