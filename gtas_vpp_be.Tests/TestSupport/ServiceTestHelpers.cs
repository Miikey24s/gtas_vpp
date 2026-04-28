using System.Security.Claims;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace gtas_vpp_be.Tests.TestSupport;

internal static class ServiceTestHelpers
{
    public static VPPContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<VPPContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new VPPContext(options);
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
