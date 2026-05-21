using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace gtas_vpp_be.Tests.LibraryTests;

public class LibraryControllerTests
{
    [Fact]
    public async Task GenericGet_SupplierTable_FiltersOutDeletedSuppliers()
    {
        // Arrange
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var userNameResolver = new Mock<IUserNameResolver>();
        var dateTimeProvider = new FakeDateTimeProvider(DateTime.UtcNow);

        // Real GenericRepository pointing to our UnitOfWork mock (which wraps context)
        var repository = new GenericRepository<L05_VPPSupplier>(unitOfWork.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(x => x.GetService(typeof(IGenericRepository<L05_VPPSupplier>)))
            .Returns(repository);

        // Pass entities as-is since they are mapped inside Controller
        userNameResolver.Setup(x => x.WithUserNamesAsync(It.IsAny<List<L05_VPPSupplier>>(), context))
            .ReturnsAsync((List<L05_VPPSupplier> list, gtas_vpp_be.Service.Helpers.Context.VPPContext ctx) => list);

        // Add 2 active and 2 deleted suppliers
        context.Set<L05_VPPSupplier>().AddRange(
            new L05_VPPSupplier { Id = Guid.NewGuid(), SupplierShortName = "Active 1", IsDeleted = false },
            new L05_VPPSupplier { Id = Guid.NewGuid(), SupplierShortName = "Active 2", IsDeleted = false },
            new L05_VPPSupplier { Id = Guid.NewGuid(), SupplierShortName = "Deleted 1", IsDeleted = true },
            new L05_VPPSupplier { Id = Guid.NewGuid(), SupplierShortName = "Deleted 2", IsDeleted = true }
        );
        await context.SaveChangesAsync();

        var controller = new LibraryController(serviceProvider.Object, userNameResolver.Object, unitOfWork.Object, dateTimeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        // Act: Get "l05" (Suppliers)
        var result = await controller.GenericGet(
            tableCode: "l05",
            id: null,
            searchText: null,
            classId: null,
            filter: null,
            skip: null,
            top: null,
            orderby: null,
            distinct: null,
            distinctFilter: null
        );

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<L05_VPPSupplierResDTO>>(okResult.Value);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, x => x.SupplierShortName == "Active 1");
        Assert.Contains(list, x => x.SupplierShortName == "Active 2");
        Assert.DoesNotContain(list, x => x.SupplierShortName == "Deleted 1");
        Assert.DoesNotContain(list, x => x.SupplierShortName == "Deleted 2");
    }
}
