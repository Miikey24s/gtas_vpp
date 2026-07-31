using gtas_vpp_be.Controllers;
using gtas_vpp_be.Model.Auth;
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

    [Fact]
    public async Task SupplierImpactCountsActiveMappingsAndPriceLists()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var supplierId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        context.Set<Supplier>().Add(new Supplier { Id = supplierId, SupplierName = "Supplier" });
        context.Set<PriceList>().Add(new PriceList { Id = priceListId, SupplierId = supplierId, PriceListName = "Default" });
        context.Set<SupplierProductMapping>().Add(new SupplierProductMapping
        {
            Id = Guid.NewGuid(),
            SupplierId = supplierId,
            PriceListId = priceListId,
            VppItemId = Guid.NewGuid()
        });
        await context.SaveChangesAsync();

        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var service = new LibraryIntegrityService(unitOfWork.Object);

        var impact = await service.GetDependencyImpactAsync("suppliers", supplierId);

        Assert.NotNull(impact);
        Assert.Equal(2, impact!.ActiveReferenceCount);
        Assert.False(impact.CanDeactivate);
    }

    [Fact]
    public async Task DepartmentImpactCountsActiveChildrenAndMemberships()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var departmentId = Guid.NewGuid();
        context.Set<Department>().AddRange(
            new Department { Id = departmentId, Code = "PARENT", Name = "Parent" },
            new Department { Id = Guid.NewGuid(), Code = "CHILD", Name = "Child", ParentDepartmentId = departmentId });
        context.Set<UserGroupMembership>().Add(new UserGroupMembership
        {
            Id = Guid.NewGuid(),
            UserId = 7,
            PermissionGroupId = Guid.NewGuid(),
            DepartmentId = departmentId
        });
        await context.SaveChangesAsync();

        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var service = new LibraryIntegrityService(unitOfWork.Object);

        var impact = await service.GetDependencyImpactAsync("departments", departmentId);

        Assert.NotNull(impact);
        Assert.Equal(2, impact!.ActiveReferenceCount);
        Assert.False(impact.CanDeactivate);
    }

    [Fact]
    public async Task DepartmentParentValidationRejectsDescendantCycle()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        context.Set<Department>().AddRange(
            new Department { Id = rootId, Code = "ROOT", Name = "Root" },
            new Department { Id = childId, Code = "CHILD", Name = "Child", ParentDepartmentId = rootId });
        await context.SaveChangesAsync();

        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var service = new LibraryIntegrityService(unitOfWork.Object);

        var validation = await service.ValidateDepartmentParentAsync(rootId, childId);

        Assert.False(validation.IsValid);
        Assert.Contains("descendants", validation.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LookupHardDelete_RequiresSoftDeactivationFirst()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var categoryId = Guid.NewGuid();
        context.LookupCategories.Add(new LookupCategory
        {
            Id = categoryId,
            Code = "UOM",
            Name = "Unit",
            IsDeleted = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        var result = await controller.GenericDelete("lookup-categories", categoryId);

        var conflict = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
        Assert.NotNull(await context.LookupCategories.FindAsync(categoryId));
    }

    [Fact]
    public async Task LookupCategoryHardDelete_BlocksAnyRemainingValueIncludingInactiveRows()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var categoryId = Guid.NewGuid();
        context.LookupCategories.Add(new LookupCategory
        {
            Id = categoryId,
            Code = "UOM",
            Name = "Unit",
            IsDeleted = true
        });
        context.LookupValues.Add(new LookupValue
        {
            Id = Guid.NewGuid(),
            LookupCategoryId = categoryId,
            Code = "PCS",
            Value = "Piece",
            IsDeleted = true
        });
        await context.SaveChangesAsync();

        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var service = new LibraryIntegrityService(unitOfWork.Object);
        var result = await service.HardDeleteLookupAsync("lookup-categories", categoryId);

        Assert.NotNull(result);
        Assert.Equal(LibraryHardDeleteStatus.HasDependencies, result!.Status);
        Assert.Equal(1, result.ReferenceCount);
        Assert.NotNull(await context.LookupCategories.FindAsync(categoryId));
    }

    [Fact]
    public async Task LookupValueHardDelete_RemovesInactiveRecordAndOwnedTranslations()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var categoryId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        context.LookupCategories.Add(new LookupCategory
        {
            Id = categoryId,
            Code = "UOM",
            Name = "Unit"
        });
        context.LookupValues.Add(new LookupValue
        {
            Id = valueId,
            LookupCategoryId = categoryId,
            Code = "PCS",
            Value = "Piece",
            IsDeleted = true
        });
        context.LookupValueTranslations.Add(new LookupValueTranslation
        {
            Id = Guid.NewGuid(),
            LookupValueId = valueId,
            LanguageCode = "en",
            Name = "Piece"
        });
        await context.SaveChangesAsync();

        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var service = new LibraryIntegrityService(unitOfWork.Object);
        var result = await service.HardDeleteLookupAsync("lookup-values", valueId);

        Assert.NotNull(result);
        Assert.Equal(LibraryHardDeleteStatus.Deleted, result!.Status);
        Assert.Null(await context.LookupValues.FindAsync(valueId));
        Assert.Empty(context.LookupValueTranslations.Where(x => x.LookupValueId == valueId));
    }

    [Fact]
    public async Task VppCategoryHardDelete_BlocksInactiveChildrenAndPreservesCategory()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var categoryId = Guid.NewGuid();
        context.VppCategories.Add(new VppCategory
        {
            Id = categoryId,
            VppCategoryCode = "OFFICE",
            VppCategoryName = "Office",
            IsDeleted = true
        });
        context.VppItems.Add(new VppItem
        {
            Id = Guid.NewGuid(),
            VppCategoryId = categoryId,
            UomId = Guid.NewGuid(),
            VppCode = "VPP-001",
            VppName = "Pen",
            IsDeleted = true
        });
        await context.SaveChangesAsync();

        var service = new LibraryIntegrityService(ServiceTestHelpers.CreateUnitOfWorkMock(context).Object);
        var result = await service.HardDeleteAsync("vpp-categories", categoryId);

        Assert.NotNull(result);
        Assert.Equal(LibraryHardDeleteStatus.HasDependencies, result!.Status);
        Assert.Equal(1, result.ReferenceCount);
        Assert.NotNull(await context.VppCategories.FindAsync(categoryId));
    }

    [Fact]
    public async Task DepartmentHardDelete_RemovesInactiveRecordAndOwnedTranslations()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var departmentId = Guid.NewGuid();
        context.Departments.Add(new Department
        {
            Id = departmentId,
            Code = "ARCHIVE",
            Name = "Archive",
            IsDeleted = true
        });
        context.DepartmentTranslations.Add(new DepartmentTranslation
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            LanguageCode = "en",
            Name = "Archive"
        });
        await context.SaveChangesAsync();

        var service = new LibraryIntegrityService(ServiceTestHelpers.CreateUnitOfWorkMock(context).Object);
        var result = await service.HardDeleteAsync("departments", departmentId);

        Assert.NotNull(result);
        Assert.Equal(LibraryHardDeleteStatus.Deleted, result!.Status);
        Assert.Null(await context.Departments.FindAsync(departmentId));
        Assert.Empty(context.DepartmentTranslations.Where(x => x.DepartmentId == departmentId));
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
