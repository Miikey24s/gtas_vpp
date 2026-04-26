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
        var repository = CreateRepository(context);
        var entity = CreateCategory("CAT-ADD", "Add category");

        var result = await repository.AddAsync(entity);

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
        var repository = CreateRepository(context);

        var result = await repository.ReadAsync(x => x.VPPCategoryCode == "KEEP");

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
        var repository = CreateRepository(context);

        var result = await repository.DeleteAsync(entity.Id);

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
        var repository = CreateRepository(context);

        entity.VPPCategoryName = "After update";
        await repository.UpdateAsync(entity);

        Assert.Equal("After update", context.Set<L03_VPPCategory>().Single().VPPCategoryName);
    }

    private static GenericRepository<L03_VPPCategory> CreateRepository(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        return new GenericRepository<L03_VPPCategory>(unitOfWork.Object);
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
