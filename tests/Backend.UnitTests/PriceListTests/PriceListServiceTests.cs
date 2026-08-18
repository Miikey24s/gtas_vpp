using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.PriceListTests;

public class PriceListServiceTests
{
    [Fact]
    public async Task Query_Search_MatchesCodeAndName_WithoutExpandingToHiddenFields()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 7, 20, 9, 0, 0);
        var supplierId = await SeedSupplierAsync(context, now);
        context.Set<PriceList>().AddRange(
            new PriceList
            {
                Id = Guid.NewGuid(),
                PriceListCode = "PPJ-2026",
                PriceListName = "Office supplies",
                SupplierId = supplierId,
                ContractCode = "CTR-ALPHA",
                Version = 1,
                CurrencyCode = "VND",
                CreatedByUserId = 1,
                CreatedAtUtc = now,
                UpdatedByUserId = 1,
                UpdatedAtUtc = now,
                IsDeleted = false
            },
            new PriceList
            {
                Id = Guid.NewGuid(),
                PriceListCode = "OTHER",
                PriceListName = "Unrelated",
                Version = 1,
                CurrencyCode = "VND",
                CreatedByUserId = 1,
                CreatedAtUtc = now,
                UpdatedByUserId = 1,
                UpdatedAtUtc = now,
                IsDeleted = false
            });
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var byCode = await service.QueryAsync(search: "PPJ");
        var byName = await service.QueryAsync(search: "Office supplies");
        var byCompactName = await service.QueryAsync(search: "officesupplies");
        var bySupplier = await service.QueryAsync(search: "Supplier");
        var byContract = await service.QueryAsync(search: "ALPHA");

        Assert.Single(byCode.Data);
        Assert.Single(byName.Data);
        Assert.Single(byCompactName.Data);
        Assert.Empty(bySupplier.Data);
        Assert.Empty(byContract.Data);
        Assert.Equal("PPJ-2026", byCode.Data[0].PriceListCode);
    }

    [Fact]
    public async Task Query_ProjectsDataSourceFromLatestCompletedImport()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 8, 14, 9, 0, 0);
        var supplierId = await SeedSupplierAsync(context, now);
        var priceListId = await SeedListAsync(context, "IMPORT", "Imported list", isDefault: false, now);
        context.Set<PriceListImportBatch>().AddRange(
            new PriceListImportBatch
            {
                Id = Guid.NewGuid(),
                PriceListId = priceListId,
                SupplierId = supplierId,
                OriginalFileName = "old.xlsx",
                FileHash = "old",
                FileFormat = "xlsx",
                Status = PriceListImportBatchStatus.Completed,
                CompletedAtUtc = now.AddDays(-2),
                CreatedAtUtc = now.AddDays(-2),
                UpdatedAtUtc = now.AddDays(-2)
            },
            new PriceListImportBatch
            {
                Id = Guid.NewGuid(),
                PriceListId = priceListId,
                SupplierId = supplierId,
                OriginalFileName = "latest.csv",
                FileHash = "latest",
                FileFormat = "csv",
                Status = PriceListImportBatchStatus.Completed,
                CompletedAtUtc = now.AddDays(-1),
                CreatedAtUtc = now.AddDays(-1),
                UpdatedAtUtc = now.AddDays(-1)
            },
            new PriceListImportBatch
            {
                Id = Guid.NewGuid(),
                PriceListId = priceListId,
                SupplierId = supplierId,
                OriginalFileName = "failed.xlsx",
                FileHash = "failed",
                FileFormat = "xlsx",
                Status = PriceListImportBatchStatus.Failed,
                CompletedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        await context.SaveChangesAsync();

        var result = await CreateService(context, now).QueryAsync(search: "IMPORT");

        Assert.Equal(PriceListDataSources.Csv, Assert.Single(result.Data).DataSource);
    }

    [Theory]
    [InlineData("DEFAULT", PriceListDataSources.Default)]
    [InlineData("MANUAL", PriceListDataSources.Manual)]
    public async Task Query_ProjectsDataSourceWithoutCompletedImport(string code, string expectedSource)
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 8, 15, 9, 0, 0);
        await SeedListAsync(context, code, code, isDefault: code == "DEFAULT", now);

        var result = await CreateService(context, now).QueryAsync(search: code);

        Assert.Equal(expectedSource, Assert.Single(result.Data).DataSource);
    }

    [Fact]
    public async Task Create_DefaultTrue_DemotesPrevious()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        await SeedListAsync(context, "OLD", "Old", isDefault: true, now);
        var supplierId = await SeedSupplierAsync(context, now);
        var service = CreateService(context, now);

        var result = await service.CreateAsync(new PriceListCreateReqDTO
        {
            Code = "NEW",
            Name = "New",
            IsDefault = true,
            SupplierId = supplierId
        }, 5615);

        Assert.True(result.IsDefault);
        Assert.Equal("Published", result.Status);
        Assert.Equal(5615, result.PublishedByUserId);
        Assert.Equal(now, result.PublishedAtUtc);
        var lists = await context.Set<PriceList>().Where(x => !x.IsDeleted).ToListAsync();
        Assert.Equal(2, lists.Count);
        Assert.Single(lists, x => x.IsDefault);
        Assert.True(lists.Single(x => x.Id == result.Id).IsDefault);
    }

    [Fact]
    public async Task Create_MissingSupplier_Throws()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var service = CreateService(context, now);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(
            new PriceListCreateReqDTO
            {
                Code = "NO-SUPPLIER",
                Name = "Missing supplier"
            },
            5615));

        Assert.Contains("supplier is required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_CommercialTermsDisabled_RejectsLegacyClientValues()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var supplierId = await SeedSupplierAsync(context, now);
        var service = CreateService(context, now);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.CreateAsync(
            new PriceListCreateReqDTO
            {
                Code = "LEGACY-TERMS",
                Name = "Legacy terms",
                SupplierId = supplierId,
                DiscountRate = 5m
            },
            5615));

        Assert.Contains("đang tạm tắt", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetDefault_FlipsDefaultBetweenLists()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var firstId = await SeedListAsync(context, "A", "A", isDefault: true, now);
        var secondId = await SeedListAsync(context, "B", "B", isDefault: false, now);
        var service = CreateService(context, now);

        await service.SetDefaultAsync(secondId, 5615);

        var lists = await context.Set<PriceList>().ToListAsync();
        Assert.False(lists.Single(x => x.Id == firstId).IsDefault);
        Assert.True(lists.Single(x => x.Id == secondId).IsDefault);
    }

    [Fact]
    public async Task Delete_DefaultList_Throws()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var id = await SeedListAsync(context, "DEFAULT", "Default", isDefault: true, now);
        var service = CreateService(context, now);

        var ex = await Assert.ThrowsAsync<BusinessException>(() => service.DeleteAsync(id, 5615));

        Assert.Contains("Cannot delete the default price list", ex.Message);
        Assert.False((await context.Set<PriceList>().SingleAsync(x => x.Id == id)).IsDeleted);
    }

    [Fact]
    public async Task Delete_ListWithMappings_DeactivatesAndKeepsPrices()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var listId = await SeedListAsync(context, "LIST", "List", isDefault: false, now);
        var vppId = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vppId);
        var supplierId = await SeedSupplierAsync(context, now);
        context.Set<SupplierProductMapping>().Add(PriceRow(listId, vppId, supplierId, 100, isDefault: true, now));
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var result = await service.SetDeletedAsync(listId, true, 5615);

        Assert.True(result.IsDeleted);
        Assert.Single(await context.Set<SupplierProductMapping>().Where(x => x.PriceListId == listId).ToListAsync());
    }

    [Fact]
    public async Task HardDelete_RequiresDeactivationAndRemovesPriceList()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var id = await SeedListAsync(context, "ARCHIVE", "Archive", isDefault: false, now);
        var service = CreateService(context, now);

        var activeException = await Assert.ThrowsAsync<BusinessException>(() => service.HardDeleteAsync(id));
        Assert.Contains("deactivated", activeException.Message, StringComparison.OrdinalIgnoreCase);

        await service.SetDeletedAsync(id, true, 5615);
        await service.HardDeleteAsync(id);

        Assert.Null(await context.PriceLists.FindAsync(id));
    }

    [Fact]
    public async Task Clone_CopiesAllL06Rows_NewIds_NotDefault()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var sourceId = await SeedListAsync(context, "SRC", "Source", isDefault: true, now);
        var source = await context.Set<PriceList>().SingleAsync(item => item.Id == sourceId);
        source.ContractCode = "CTR-OLD";
        source.DiscountRate = 5m;
        source.RebateAmount = 10m;
        source.FeeAmount = 20m;
        source.ShippingAmount = 30m;
        var vpp1Id = Guid.NewGuid();
        var vpp2Id = Guid.NewGuid();
        await ServiceTestHelpers.SeedActiveVPPAsync(context, vpp1Id, vpp2Id);
        var supplierId = await SeedSupplierAsync(context, now);
        var original1 = PriceRow(sourceId, vpp1Id, supplierId, 100, isDefault: true, now);
        var original2 = PriceRow(sourceId, vpp2Id, supplierId, 200, isDefault: false, now);
        context.Set<SupplierProductMapping>().AddRange(original1, original2);
        await context.SaveChangesAsync();
        var service = CreateService(context, now);

        var clone = await service.CloneAsync(new PriceListCloneReqDTO
        {
            SourceId = sourceId,
            Code = "CLONE",
            Name = "Clone"
        }, 5615);

        Assert.False(clone.IsDefault);
        Assert.Equal("Published", clone.Status);
        Assert.Equal(1, clone.Version);
        Assert.Equal(2, clone.ItemCount);
        Assert.Null(clone.ContractCode);
        Assert.Equal(0m, clone.DiscountRate);
        Assert.Equal(0m, clone.RebateAmount);
        Assert.Equal(0m, clone.FeeAmount);
        Assert.Equal(0m, clone.ShippingAmount);
        var clonedRows = await context.Set<SupplierProductMapping>()
            .Where(x => x.PriceListId == clone.Id)
            .OrderBy(x => x.Price)
            .ToListAsync();
        Assert.Equal(2, clonedRows.Count);
        Assert.DoesNotContain(clonedRows, x => x.Id == original1.Id || x.Id == original2.Id);
        Assert.True(clonedRows.Single(x => x.VppItemId == vpp1Id).IsDefault);
        Assert.False(clonedRows.Single(x => x.VppItemId == vpp2Id).IsDefault);
    }

    [Fact]
    public async Task Update_PromoteToDefault_DemotesPrevious()
    {
        using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var now = new DateTime(2026, 5, 21, 9, 0, 0);
        var firstId = await SeedListAsync(context, "A", "A", isDefault: true, now);
        var secondId = await SeedListAsync(context, "B", "B", isDefault: false, now);
        var supplierId = await SeedSupplierAsync(context, now);
        var service = CreateService(context, now);

        await service.UpdateAsync(new PriceListUpdateReqDTO
        {
            Id = secondId,
            Code = "B2",
            Name = "B2",
            IsDefault = true,
            SupplierId = supplierId
        }, 5615);

        var lists = await context.Set<PriceList>().ToListAsync();
        Assert.False(lists.Single(x => x.Id == firstId).IsDefault);
        Assert.True(lists.Single(x => x.Id == secondId).IsDefault);
    }

    private static PriceListService CreateService(gtas_vpp_be.Service.Helpers.Context.VPPContext context, DateTime now)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        return new PriceListService(unitOfWork.Object, new FakeDateTimeProvider(now), new UserNameResolver());
    }

    private static async Task<Guid> SeedListAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        string code,
        string name,
        bool isDefault,
        DateTime now)
    {
        var id = Guid.NewGuid();
        context.Set<PriceList>().Add(new PriceList
        {
            Id = id,
            PriceListCode = code,
            PriceListName = name,
            IsDefault = isDefault,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static async Task<Guid> SeedSupplierAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        DateTime now)
    {
        var id = Guid.NewGuid();
        context.Set<Supplier>().Add(new Supplier
        {
            Id = id,
            SupplierShortName = "SUP",
            SupplierName = "Supplier",
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        });
        await context.SaveChangesAsync();
        return id;
    }

    private static SupplierProductMapping PriceRow(
        Guid listId,
        Guid vppId,
        Guid supplierId,
        decimal price,
        bool isDefault,
        DateTime now)
        => new()
        {
            Id = Guid.NewGuid(),
            PriceListId = listId,
            VppItemId = vppId,
            SupplierId = supplierId,
            Price = price,
            IsDefault = isDefault,
            CreatedByUserId = 1,
            CreatedAtUtc = now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = now,
            IsDeleted = false
        };
}
