using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using Xunit;

namespace gtas_vpp_be.Tests.BaseServicesTests;

public class CrudOperationsTests
{
    [Fact]
    public async Task AddAsync_ValidEntity_AddsEntityToDatabase()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var service = CreateService(context);
        var entity = CreateCategory("CAT-ADD", "Add category");

        var result = await service.AddAsync(entity, nameof(Config.ContextType.VPPContext));

        Assert.NotNull(result);
        Assert.Single(context.Set<L03_VPPCategory>());
        Assert.Equal("CAT-ADD", context.Set<L03_VPPCategory>().Single().VPPCategoryCode);
    }

    [Fact]
    public async Task ReadAsync_Filter_ReturnsMatchingEntities()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        context.Set<L03_VPPCategory>().AddRange(
            CreateCategory("KEEP", "Keep category"),
            CreateCategory("SKIP", "Skip category"));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.ReadAsync<L03_VPPCategory>(
            nameof(Config.ContextType.VPPContext),
            getFullName: false,
            expression: x => x.VPPCategoryCode == "KEEP");

        Assert.Single(result);
        Assert.Equal("KEEP", result[0].VPPCategoryCode);
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_RemovesEntityFromDatabase()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var entity = CreateCategory("CAT-DELETE", "Delete category");
        context.Set<L03_VPPCategory>().Add(entity);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.DeleteAsync<L03_VPPCategory>(entity.Id, nameof(Config.ContextType.VPPContext));

        Assert.True(result);
        Assert.Empty(context.Set<L03_VPPCategory>());
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesEntityInDatabase()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var entity = CreateCategory("CAT-UPDATE", "Before update");
        context.Set<L03_VPPCategory>().Add(entity);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        entity.VPPCategoryName = "After update";
        await service.UpdateAsync(entity, nameof(Config.ContextType.VPPContext));

        Assert.Equal("After update", context.Set<L03_VPPCategory>().Single().VPPCategoryName);
    }

    private static BaseServices CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        var factory = ServiceTestHelpers.CreateUnitOfWorkFactoryMock(unitOfWork.Object);
        var httpContextAccessor = ServiceTestHelpers.CreateHttpContextAccessor();
        return new BaseServices(factory.Object, httpContextAccessor);
    }

    private static L03_VPPCategory CreateCategory(string code, string name)
        => new()
        {
            Id = Guid.NewGuid(),
            VPPCategoryCode = code,
            VPPCategoryName = name,
            CreateDate = DateTime.UtcNow,
            UpdateDate = DateTime.UtcNow,
            CreateUserId = 1,
            UpdateUserId = 1
        };
}
