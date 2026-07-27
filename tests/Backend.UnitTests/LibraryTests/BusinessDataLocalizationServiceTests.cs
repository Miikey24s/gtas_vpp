using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Services;
using gtas_vpp_be.Tests.TestSupport;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace gtas_vpp_be.Tests.LibraryTests;

public sealed class BusinessDataLocalizationServiceTests
{
    private static readonly DateTime Now = new(2026, 7, 21, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ApprovedTranslation_IsResolvedForRequestedLanguage()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var department = await SeedDepartmentAsync(context);
        var service = CreateService(context, BusinessLanguages.English);

        var bundle = await service.UpsertTranslationAsync(
            BusinessDataEntityTypes.Department,
            department.Id,
            BusinessLanguages.English,
            new BusinessDataTranslationUpsertReqDTO
            {
                Name = "Information Technology",
                Description = "Technology operations",
                Status = "Approved",
                Source = "Manual"
            },
            userId: 7);

        Assert.Equal("Information Technology", bundle.DisplayName);
        Assert.Equal(BusinessLanguages.English, bundle.ResolvedLanguageCode);
        Assert.False(bundle.IsFallback);
        Assert.Single(bundle.Translations);
    }

    [Fact]
    public async Task DraftTranslation_DoesNotLeakIntoOperationalResolution()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var department = await SeedDepartmentAsync(context);
        var service = CreateService(context, BusinessLanguages.English);

        await service.UpsertTranslationAsync(
            BusinessDataEntityTypes.Department,
            department.Id,
            BusinessLanguages.English,
            new BusinessDataTranslationUpsertReqDTO
            {
                Name = "Unreviewed title",
                Status = "Draft",
                Source = "AiDraft"
            },
            userId: 7);

        var resolved = await service.ResolveAsync(
            BusinessDataEntityTypes.Department,
            [new BusinessDataOriginalValue(department.Id, "vi", department.Name!, department.Description)]);

        Assert.Equal(department.Name, resolved[department.Id].DisplayName);
        Assert.Equal(BusinessLanguages.Vietnamese, resolved[department.Id].ResolvedLanguageCode);
        Assert.True(resolved[department.Id].IsFallback);
    }

    [Fact]
    public async Task UpsertAndDelete_PreserveOneAuditableTranslationRow()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var department = await SeedDepartmentAsync(context);
        var service = CreateService(context, BusinessLanguages.English);

        await service.UpsertTranslationAsync(
            BusinessDataEntityTypes.Department,
            department.Id,
            BusinessLanguages.English,
            new BusinessDataTranslationUpsertReqDTO
            {
                Name = "IT Department",
                Status = "Approved",
                Source = "Manual"
            },
            userId: 7);
        await service.UpsertTranslationAsync(
            BusinessDataEntityTypes.Department,
            department.Id,
            BusinessLanguages.English,
            new BusinessDataTranslationUpsertReqDTO
            {
                Name = "Information Technology Department",
                Status = "Approved",
                Source = "Manual"
            },
            userId: 8);

        var row = Assert.Single(await context.Set<DepartmentTranslation>().ToListAsync());
        Assert.Equal("Information Technology Department", row.Name);
        Assert.Equal(8, row.UpdatedByUserId);

        Assert.True(await service.DeleteTranslationAsync(
            BusinessDataEntityTypes.Department,
            department.Id,
            BusinessLanguages.English,
            userId: 9));
        Assert.True((await context.Set<DepartmentTranslation>().SingleAsync()).IsDeleted);
    }

    [Fact]
    public async Task OriginalLanguage_CanBeCorrectedWithoutChangingCanonicalText()
    {
        await using var context = ServiceTestHelpers.CreateInMemoryContext(Guid.NewGuid().ToString());
        var department = await SeedDepartmentAsync(context);
        var service = CreateService(context, BusinessLanguages.English);

        var bundle = await service.UpdateOriginalLanguageAsync(
            BusinessDataEntityTypes.Department,
            department.Id,
            BusinessLanguages.English,
            userId: 10);

        Assert.Equal(BusinessLanguages.English, bundle.OriginalLanguageCode);
        Assert.Equal("Công nghệ thông tin", bundle.OriginalName);
        Assert.False(bundle.IsFallback);
    }

    private static BusinessDataLocalizationService CreateService(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context,
        string requestedLanguage)
    {
        var unitOfWork = ServiceTestHelpers.CreateUnitOfWorkMock(context);
        return new BusinessDataLocalizationService(
            unitOfWork.Object,
            new FakeDateTimeProvider(Now),
            new FakeRequestLanguageProvider(requestedLanguage));
    }

    private static async Task<Department> SeedDepartmentAsync(
        gtas_vpp_be.Service.Helpers.Context.VPPContext context)
    {
        var department = new Department
        {
            Id = Guid.NewGuid(),
            Code = "IT",
            Name = "Công nghệ thông tin",
            Description = "Vận hành công nghệ",
            OriginalLanguageCode = BusinessLanguages.Vietnamese,
            CreatedByUserId = 1,
            CreatedAtUtc = Now,
            UpdatedByUserId = 1,
            UpdatedAtUtc = Now,
            IsDeleted = false
        };
        context.Add(department);
        await context.SaveChangesAsync();
        return department;
    }

    private sealed class FakeRequestLanguageProvider(string languageCode) : IRequestLanguageProvider
    {
        public string LanguageCode { get; } = languageCode;
    }
}
