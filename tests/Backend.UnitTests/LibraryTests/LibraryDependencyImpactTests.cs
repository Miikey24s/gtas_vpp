using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace gtas_vpp_be.Tests.LibraryTests;

public sealed class LibraryDependencyImpactTests
{
    [Fact]
    public async Task LookupCategoryImpact_BlocksDeactivateWhenActiveValueExists()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var controller = CreateController(context);
        var categoryId = Guid.NewGuid();

        context.Set<LookupCategory>().Add(new LookupCategory { Id = categoryId, Code = "UOM", Name = "Unit" });
        context.Set<LookupValue>().Add(new LookupValue
        {
            Id = Guid.NewGuid(),
            LookupCategoryId = categoryId,
            Code = "PCS",
            Value = "Piece"
        });
        await context.SaveChangesAsync();

        var result = await controller.GetDependencyImpact("lookup-categories", categoryId);

        var response = Assert.IsType<OkObjectResult>(result);
        var impact = Assert.IsType<LibraryDependencyImpactResDTO>(response.Value);
        Assert.Equal(1, impact.ActiveReferenceCount);
        Assert.False(impact.CanDeactivate);
    }

    private static LibraryController CreateController(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);
        return new LibraryController(
            new Mock<IServiceProvider>().Object,
            new Mock<IUserNameResolver>().Object,
            unitOfWork.Object,
            dateTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }
}
