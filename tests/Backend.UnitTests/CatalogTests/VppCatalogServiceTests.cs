using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.CatalogTests;

public sealed class VppCatalogServiceTests
{
    [Fact]
    public async Task QueryItems_UsesServerPagingSearchAndAllowlistedSort()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().AddRange(
            CreateItem("VPP-001", "Alpha pen", uomId, categoryId),
            CreateItem("VPP-002", "Bravo pen", uomId, categoryId),
            CreateItem("VPP-003", "Alpha marker", uomId, categoryId));
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var result = await service.QueryItemsAsync(
            categoryId, "alpha", null, skip: 0, top: 1, orderby: "VppCode desc",
            distinct: null, distinctFilter: null, showDeleted: false);

        Assert.Equal(2, result.TotalCount);
        var item = Assert.Single(result.Items);
        Assert.Equal("VPP-003", item.VppCode);
    }

    [Fact]
    public async Task QueryItems_RejectsUnsupportedFilterAndSortMembers()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().Add(CreateItem("VPP-001", "Alpha", uomId, categoryId));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(() => service.QueryItemsAsync(
            null, null, "GetType().Name == \"VppItem\"", 0, 20, null, null, null, false));
        await Assert.ThrowsAsync<ArgumentException>(() => service.QueryItemsAsync(
            null, null, null, 0, 20, "Description.ToString() desc", null, null, false));
    }

    [Fact]
    public async Task QueryItems_PagesTenThousandSyntheticItemsWithoutReturningTheFullTable()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().AddRange(Enumerable.Range(1, 10_000)
            .Select(index => CreateItem($"VPP-{index:D5}", $"Synthetic {index:D5}", uomId, categoryId)));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.QueryItemsAsync(
            categoryId, null, null, skip: 0, top: 25, orderby: "VppCode desc",
            distinct: null, distinctFilter: null, showDeleted: false);

        Assert.Equal(10_000, result.TotalCount);
        Assert.Equal(25, result.Items.Count);
        Assert.Equal("VPP-10000", result.Items[0].VppCode);
    }

    [Fact]
    public async Task QueryItems_ReturnsPagedDistinctValuesForAllowlistedColumn()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().AddRange(
            CreateItem("VPP-001", "Alpha", uomId, categoryId),
            CreateItem("VPP-002", "Alpha", uomId, categoryId),
            CreateItem("VPP-003", "Bravo", uomId, categoryId));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.QueryItemsAsync(
            null, null, null, 0, 20, null, "VppName", "alp", false);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Alpha", Assert.Single(result.Items).VppName);
    }

    [Fact]
    public async Task QueryItems_AcceptsRadzenCaseInsensitiveStringFilterShape()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().AddRange(
            CreateItem("VPP-001", "Alpha", uomId, categoryId),
            CreateItem("VPP-002", "Bravo", uomId, categoryId));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.QueryItemsAsync(
            null, null, "x => ((x.VppName ?? \"\").ToLower() ?? \"\").Contains(\"alpha\")",
            0, 20, null, null, null, false);

        Assert.Equal("Alpha", Assert.Single(result.Items).VppName);
    }

    [Fact]
    public async Task QueryItems_AcceptsRadzenCheckboxListFilterShape()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().AddRange(
            CreateItem("VPP-001", "Alpha", uomId, categoryId),
            CreateItem("VPP-002", "Bravo", uomId, categoryId));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.QueryItemsAsync(
            null, null, "x => new [] { \"Alpha\" }.Contains(x.VppName)",
            0, 20, null, null, null, false);

        Assert.Equal("Alpha", Assert.Single(result.Items).VppName);
    }

    [Fact]
    public async Task CreateItem_RejectsDuplicateCode_AndStatusUsesSoftDeleteRestore()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        context.Set<VppItem>().Add(CreateItem("VPP-001", "Alpha", uomId, categoryId));
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateItemAsync(
            new VppItemCreateRequest
            {
                VppCode = " vpp-001 ",
                VppName = "Duplicate",
                UomId = uomId,
                VppCategoryId = categoryId
            },
            userId: 7));

        var created = await service.CreateItemAsync(
            new VppItemCreateRequest
            {
                VppCode = "VPP-002",
                VppName = "Bravo",
                UomId = uomId,
                VppCategoryId = categoryId
            },
            userId: 7);

        var deleted = await service.SetItemStatusAsync(
            created.Id,
            new VppItemStatusRequest { IsDeleted = true },
            userId: 7);
        Assert.True(deleted.IsDeleted);
        Assert.True(await context.Set<VppItem>().AnyAsync(x => x.Id == created.Id && x.IsDeleted));

        var restored = await service.SetItemStatusAsync(
            created.Id,
            new VppItemStatusRequest { IsDeleted = false },
            userId: 7);
        Assert.False(restored.IsDeleted);
    }

    [Fact]
    public async Task HardDeleteItem_RequiresDeactivationAndDeletesItem()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var (uomId, categoryId) = await SeedReferencesAsync(context);
        var item = CreateItem("VPP-DELETE", "Delete me", uomId, categoryId);
        context.VppItems.Add(item);
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var activeResult = await service.HardDeleteItemAsync(item.Id);
        Assert.Equal(LibraryHardDeleteStatus.MustDeactivate, activeResult.Status);

        item.IsDeleted = true;
        var deletedResult = await service.HardDeleteItemAsync(item.Id);
        Assert.Equal(LibraryHardDeleteStatus.Deleted, deletedResult.Status);
        Assert.Null(await context.VppItems.FindAsync(item.Id));
    }

    private static VppCatalogService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context)
        => new(
            ServiceTestHelpers.CreateUnitOfWorkMock(context).Object,
            new FakeDateTimeProvider(new DateTime(2026, 7, 16, 10, 0, 0)));

    private static async Task<(Guid UomId, Guid CategoryId)> SeedReferencesAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var uomId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var now = new DateTime(2026, 7, 16, 9, 0, 0);
        context.Set<LookupValue>().Add(new LookupValue
        {
            Id = uomId,
            Code = "PCS",
            Value = "Pieces",
            CreatedByUserId = 1,
            UpdatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        context.Set<VppCategory>().Add(new VppCategory
        {
            Id = categoryId,
            VppCategoryCode = "OFFICE",
            VppCategoryName = "Office",
            CreatedByUserId = 1,
            UpdatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await context.SaveChangesAsync();
        return (uomId, categoryId);
    }

    private static VppItem CreateItem(string code, string name, Guid uomId, Guid categoryId)
        => new()
        {
            Id = Guid.NewGuid(),
            VppCode = code,
            VppName = name,
            UomId = uomId,
            VppCategoryId = categoryId,
            CreatedByUserId = 1,
            UpdatedByUserId = 1,
            CreatedAtUtc = new DateTime(2026, 7, 16, 9, 0, 0),
            UpdatedAtUtc = new DateTime(2026, 7, 16, 9, 0, 0)
        };
}
