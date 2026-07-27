using System.Linq.Expressions;
using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public sealed record BusinessDataOriginalValue(
    Guid Id,
    string OriginalLanguageCode,
    string Name,
    string? Description);

public sealed record BusinessDataResolvedText(
    string DisplayName,
    string? DisplayDescription,
    string ResolvedLanguageCode,
    bool IsFallback);

public interface IBusinessDataLocalizationService
{
    Task<BusinessDataTranslationBundleResDTO?> GetBundleAsync(
        string entityType,
        Guid entityId,
        string? requestedLanguage = null,
        CancellationToken cancellationToken = default);

    Task<BusinessDataTranslationBundleResDTO> UpsertTranslationAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        BusinessDataTranslationUpsertReqDTO request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<BusinessDataTranslationBundleResDTO> UpdateOriginalLanguageAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        int userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTranslationAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, BusinessDataResolvedText>> ResolveAsync(
        string entityType,
        IReadOnlyCollection<BusinessDataOriginalValue> originals,
        string? requestedLanguage = null,
        CancellationToken cancellationToken = default);
}

public sealed class BusinessDataLocalizationService : IBusinessDataLocalizationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRequestLanguageProvider _requestLanguageProvider;

    public BusinessDataLocalizationService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IRequestLanguageProvider requestLanguageProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _requestLanguageProvider = requestLanguageProvider;
    }

    public Task<BusinessDataTranslationBundleResDTO?> GetBundleAsync(
        string entityType,
        Guid entityId,
        string? requestedLanguage = null,
        CancellationToken cancellationToken = default)
    {
        entityType = NormalizeEntityType(entityType);
        var languageCode = NormalizeLanguage(requestedLanguage);
        var context = _unitOfWork.VPPContext;

        return entityType switch
        {
            BusinessDataEntityTypes.LookupCategory => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<LookupCategory>(),
                x => x.Id == entityId,
                x => x.Name ?? string.Empty,
                x => x.Description,
                context.Set<LookupCategoryTranslation>().Where(x => x.LookupCategoryId == entityId),
                cancellationToken),
            BusinessDataEntityTypes.LookupValue => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<LookupValue>(),
                x => x.Id == entityId,
                x => x.Value,
                x => x.Description,
                context.Set<LookupValueTranslation>().Where(x => x.LookupValueId == entityId),
                cancellationToken),
            BusinessDataEntityTypes.VppCategory => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<VppCategory>(),
                x => x.Id == entityId,
                x => x.VppCategoryName ?? string.Empty,
                x => x.Description,
                context.Set<VppCategoryTranslation>().Where(x => x.VppCategoryId == entityId),
                cancellationToken),
            BusinessDataEntityTypes.VppItem => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<VppItem>(),
                x => x.Id == entityId,
                x => x.VppName ?? string.Empty,
                x => x.Description,
                context.Set<VppItemTranslation>().Where(x => x.VppItemId == entityId),
                cancellationToken),
            BusinessDataEntityTypes.Supplier => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<Supplier>(),
                x => x.Id == entityId,
                x => x.SupplierName ?? string.Empty,
                x => x.Description,
                context.Set<SupplierTranslation>().Where(x => x.SupplierId == entityId),
                cancellationToken),
            BusinessDataEntityTypes.PriceList => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<PriceList>(),
                x => x.Id == entityId,
                x => x.PriceListName ?? string.Empty,
                x => x.Description,
                context.Set<PriceListTranslation>().Where(x => x.PriceListId == entityId),
                cancellationToken),
            BusinessDataEntityTypes.Department => ReadBundleAsync(
                entityType,
                entityId,
                languageCode,
                context.Set<Department>(),
                x => x.Id == entityId,
                x => x.Name ?? string.Empty,
                x => x.Description,
                context.Set<DepartmentTranslation>().Where(x => x.DepartmentId == entityId),
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(entityType))
        };
    }

    public async Task<BusinessDataTranslationBundleResDTO> UpsertTranslationAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        BusinessDataTranslationUpsertReqDTO request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        entityType = NormalizeEntityType(entityType);
        languageCode = NormalizeLanguage(languageCode, rejectUnsupported: true);
        var status = ParseStatus(request.Status);
        var source = ParseSource(request.Source);
        var name = NormalizeRequired(request.Name, nameof(request.Name), 250);
        var description = NormalizeOptional(request.Description, 500);
        var context = _unitOfWork.VPPContext;

        switch (entityType)
        {
            case BusinessDataEntityTypes.LookupCategory:
                await UpsertAsync(context.Set<LookupCategory>(), entityId, context.Set<LookupCategoryTranslation>(),
                    x => x.LookupCategoryId == entityId && x.LanguageCode == languageCode,
                    () => new LookupCategoryTranslation { LookupCategoryId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.LookupValue:
                await UpsertAsync(context.Set<LookupValue>(), entityId, context.Set<LookupValueTranslation>(),
                    x => x.LookupValueId == entityId && x.LanguageCode == languageCode,
                    () => new LookupValueTranslation { LookupValueId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.VppCategory:
                await UpsertAsync(context.Set<VppCategory>(), entityId, context.Set<VppCategoryTranslation>(),
                    x => x.VppCategoryId == entityId && x.LanguageCode == languageCode,
                    () => new VppCategoryTranslation { VppCategoryId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.VppItem:
                await UpsertAsync(context.Set<VppItem>(), entityId, context.Set<VppItemTranslation>(),
                    x => x.VppItemId == entityId && x.LanguageCode == languageCode,
                    () => new VppItemTranslation { VppItemId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.Supplier:
                await UpsertAsync(context.Set<Supplier>(), entityId, context.Set<SupplierTranslation>(),
                    x => x.SupplierId == entityId && x.LanguageCode == languageCode,
                    () => new SupplierTranslation { SupplierId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.PriceList:
                await UpsertAsync(context.Set<PriceList>(), entityId, context.Set<PriceListTranslation>(),
                    x => x.PriceListId == entityId && x.LanguageCode == languageCode,
                    () => new PriceListTranslation { PriceListId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.Department:
                await UpsertAsync(context.Set<Department>(), entityId, context.Set<DepartmentTranslation>(),
                    x => x.DepartmentId == entityId && x.LanguageCode == languageCode,
                    () => new DepartmentTranslation { DepartmentId = entityId }, languageCode, name,
                    description, status, source, userId, cancellationToken);
                break;
        }

        return await GetBundleAsync(entityType, entityId, languageCode, cancellationToken)
            ?? throw new KeyNotFoundException($"{entityType} {entityId} was not found after translation update.");
    }

    public async Task<BusinessDataTranslationBundleResDTO> UpdateOriginalLanguageAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        int userId,
        CancellationToken cancellationToken = default)
    {
        entityType = NormalizeEntityType(entityType);
        languageCode = NormalizeLanguage(languageCode, rejectUnsupported: true);
        var context = _unitOfWork.VPPContext;

        switch (entityType)
        {
            case BusinessDataEntityTypes.LookupCategory:
                await UpdateOriginalLanguageAsync(context.Set<LookupCategory>(), entityId, languageCode, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.LookupValue:
                await UpdateOriginalLanguageAsync(context.Set<LookupValue>(), entityId, languageCode, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.VppCategory:
                await UpdateOriginalLanguageAsync(context.Set<VppCategory>(), entityId, languageCode, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.VppItem:
                await UpdateOriginalLanguageAsync(context.Set<VppItem>(), entityId, languageCode, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.Supplier:
                await UpdateOriginalLanguageAsync(context.Set<Supplier>(), entityId, languageCode, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.PriceList:
                await UpdateOriginalLanguageAsync(context.Set<PriceList>(), entityId, languageCode, userId, cancellationToken);
                break;
            case BusinessDataEntityTypes.Department:
                await UpdateOriginalLanguageAsync(context.Set<Department>(), entityId, languageCode, userId, cancellationToken);
                break;
        }

        return await GetBundleAsync(entityType, entityId, languageCode, cancellationToken)
            ?? throw new KeyNotFoundException($"{entityType} {entityId} was not found after language update.");
    }

    public async Task<bool> DeleteTranslationAsync(
        string entityType,
        Guid entityId,
        string languageCode,
        int userId,
        CancellationToken cancellationToken = default)
    {
        entityType = NormalizeEntityType(entityType);
        languageCode = NormalizeLanguage(languageCode, rejectUnsupported: true);
        var context = _unitOfWork.VPPContext;

        BusinessDataTranslationBase? translation = entityType switch
        {
            BusinessDataEntityTypes.LookupCategory => await context.Set<LookupCategoryTranslation>()
                .FirstOrDefaultAsync(x => x.LookupCategoryId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            BusinessDataEntityTypes.LookupValue => await context.Set<LookupValueTranslation>()
                .FirstOrDefaultAsync(x => x.LookupValueId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            BusinessDataEntityTypes.VppCategory => await context.Set<VppCategoryTranslation>()
                .FirstOrDefaultAsync(x => x.VppCategoryId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            BusinessDataEntityTypes.VppItem => await context.Set<VppItemTranslation>()
                .FirstOrDefaultAsync(x => x.VppItemId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            BusinessDataEntityTypes.Supplier => await context.Set<SupplierTranslation>()
                .FirstOrDefaultAsync(x => x.SupplierId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            BusinessDataEntityTypes.PriceList => await context.Set<PriceListTranslation>()
                .FirstOrDefaultAsync(x => x.PriceListId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            BusinessDataEntityTypes.Department => await context.Set<DepartmentTranslation>()
                .FirstOrDefaultAsync(x => x.DepartmentId == entityId && x.LanguageCode == languageCode && !x.IsDeleted, cancellationToken),
            _ => null
        };

        if (translation is null) return false;

        translation.IsDeleted = true;
        translation.UpdatedByUserId = userId;
        translation.UpdatedAtUtc = _dateTimeProvider.Now;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyDictionary<Guid, BusinessDataResolvedText>> ResolveAsync(
        string entityType,
        IReadOnlyCollection<BusinessDataOriginalValue> originals,
        string? requestedLanguage = null,
        CancellationToken cancellationToken = default)
    {
        if (originals.Count == 0) return new Dictionary<Guid, BusinessDataResolvedText>();

        entityType = NormalizeEntityType(entityType);
        var languageCode = NormalizeLanguage(requestedLanguage);
        var ids = originals.Select(x => x.Id).Distinct().ToList();
        var context = _unitOfWork.VPPContext;

        var translations = entityType switch
        {
            BusinessDataEntityTypes.LookupCategory => await ReadApprovedAsync(
                context.Set<LookupCategoryTranslation>().Where(x => ids.Contains(x.LookupCategoryId)),
                x => x.LookupCategoryId, languageCode, cancellationToken),
            BusinessDataEntityTypes.LookupValue => await ReadApprovedAsync(
                context.Set<LookupValueTranslation>().Where(x => ids.Contains(x.LookupValueId)),
                x => x.LookupValueId, languageCode, cancellationToken),
            BusinessDataEntityTypes.VppCategory => await ReadApprovedAsync(
                context.Set<VppCategoryTranslation>().Where(x => ids.Contains(x.VppCategoryId)),
                x => x.VppCategoryId, languageCode, cancellationToken),
            BusinessDataEntityTypes.VppItem => await ReadApprovedAsync(
                context.Set<VppItemTranslation>().Where(x => ids.Contains(x.VppItemId)),
                x => x.VppItemId, languageCode, cancellationToken),
            BusinessDataEntityTypes.Supplier => await ReadApprovedAsync(
                context.Set<SupplierTranslation>().Where(x => ids.Contains(x.SupplierId)),
                x => x.SupplierId, languageCode, cancellationToken),
            BusinessDataEntityTypes.PriceList => await ReadApprovedAsync(
                context.Set<PriceListTranslation>().Where(x => ids.Contains(x.PriceListId)),
                x => x.PriceListId, languageCode, cancellationToken),
            BusinessDataEntityTypes.Department => await ReadApprovedAsync(
                context.Set<DepartmentTranslation>().Where(x => ids.Contains(x.DepartmentId)),
                x => x.DepartmentId, languageCode, cancellationToken),
            _ => []
        };

        var translationMap = translations.ToDictionary(x => x.EntityId);
        return originals.ToDictionary(
            original => original.Id,
            original => Resolve(original, languageCode, translationMap.GetValueOrDefault(original.Id)));
    }

    private async Task<BusinessDataTranslationBundleResDTO?> ReadBundleAsync<TEntity, TTranslation>(
        string entityType,
        Guid entityId,
        string requestedLanguage,
        DbSet<TEntity> entities,
        Expression<Func<TEntity, bool>> predicate,
        Func<TEntity, string> getName,
        Func<TEntity, string?> getDescription,
        IQueryable<TTranslation> translations,
        CancellationToken cancellationToken)
        where TEntity : BaseModel, ITranslatableBusinessEntity
        where TTranslation : BusinessDataTranslationBase
    {
        var entity = await entities.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
        if (entity is null) return null;

        var translationRows = await translations.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.LanguageCode)
            .ToListAsync(cancellationToken);
        var original = new BusinessDataOriginalValue(
            entityId,
            BusinessLanguages.Normalize(entity.OriginalLanguageCode),
            getName(entity),
            getDescription(entity));
        var approved = translationRows.FirstOrDefault(x =>
            x.Status == BusinessTranslationStatus.Approved
            && string.Equals(x.LanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase));
        var resolved = Resolve(original, requestedLanguage, approved is null
            ? null
            : new TranslationRow(entityId, approved.Name, approved.Description));

        return new BusinessDataTranslationBundleResDTO
        {
            EntityType = entityType,
            EntityId = entityId,
            OriginalLanguageCode = original.OriginalLanguageCode,
            OriginalName = original.Name,
            OriginalDescription = original.Description,
            DisplayName = resolved.DisplayName,
            DisplayDescription = resolved.DisplayDescription,
            ResolvedLanguageCode = resolved.ResolvedLanguageCode,
            IsFallback = resolved.IsFallback,
            Translations = translationRows.Select(ToDto).ToList()
        };
    }

    private async Task UpsertAsync<TEntity, TTranslation>(
        DbSet<TEntity> entities,
        Guid entityId,
        DbSet<TTranslation> translations,
        Expression<Func<TTranslation, bool>> predicate,
        Func<TTranslation> create,
        string languageCode,
        string name,
        string? description,
        BusinessTranslationStatus status,
        BusinessTranslationSource source,
        int userId,
        CancellationToken cancellationToken)
        where TEntity : BaseModel, ITranslatableBusinessEntity
        where TTranslation : BusinessDataTranslationBase
    {
        if (!await entities.AnyAsync(x => x.Id == entityId, cancellationToken))
        {
            throw new KeyNotFoundException($"Business data entity {entityId} was not found.");
        }

        var now = _dateTimeProvider.Now;
        var translation = await translations.FirstOrDefaultAsync(predicate, cancellationToken);
        if (translation is null)
        {
            translation = create();
            translation.Id = Guid.NewGuid();
            translation.LanguageCode = languageCode;
            translation.CreatedByUserId = userId;
            translation.CreatedAtUtc = now;
            translations.Add(translation);
        }

        translation.Name = name;
        translation.Description = description;
        translation.Status = status;
        translation.Source = source;
        translation.UpdatedByUserId = userId;
        translation.UpdatedAtUtc = now;
        translation.IsDeleted = false;
        await _unitOfWork.VPPContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateOriginalLanguageAsync<TEntity>(
        DbSet<TEntity> entities,
        Guid entityId,
        string languageCode,
        int userId,
        CancellationToken cancellationToken)
        where TEntity : BaseModel, ITranslatableBusinessEntity
    {
        var entity = await entities.FirstOrDefaultAsync(x => x.Id == entityId, cancellationToken)
            ?? throw new KeyNotFoundException($"Business data entity {entityId} was not found.");
        entity.OriginalLanguageCode = languageCode;
        entity.UpdatedByUserId = userId;
        entity.UpdatedAtUtc = _dateTimeProvider.Now;
        await _unitOfWork.VPPContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<TranslationRow>> ReadApprovedAsync<TTranslation>(
        IQueryable<TTranslation> query,
        Expression<Func<TTranslation, Guid>> entityId,
        string languageCode,
        CancellationToken cancellationToken)
        where TTranslation : BusinessDataTranslationBase
    {
        var rows = await query.AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.Status == BusinessTranslationStatus.Approved
                && x.LanguageCode == languageCode)
            .ToListAsync(cancellationToken);
        var getEntityId = entityId.Compile();
        return rows.Select(x => new TranslationRow(getEntityId(x), x.Name, x.Description)).ToList();
    }

    private static BusinessDataResolvedText Resolve(
        BusinessDataOriginalValue original,
        string requestedLanguage,
        TranslationRow? translation)
    {
        if (string.Equals(original.OriginalLanguageCode, requestedLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return new BusinessDataResolvedText(
                original.Name,
                original.Description,
                original.OriginalLanguageCode,
                IsFallback: false);
        }

        return translation is null
            ? new BusinessDataResolvedText(
                original.Name,
                original.Description,
                original.OriginalLanguageCode,
                IsFallback: true)
            : new BusinessDataResolvedText(
                translation.Name,
                translation.Description,
                requestedLanguage,
                IsFallback: false);
    }

    private static BusinessDataTranslationResDTO ToDto(BusinessDataTranslationBase row) => new()
    {
        Id = row.Id,
        LanguageCode = row.LanguageCode,
        Name = row.Name,
        Description = row.Description,
        Status = row.Status.ToString(),
        Source = row.Source.ToString(),
        UpdatedAtUtc = row.UpdatedAtUtc,
        UpdatedByUserId = row.UpdatedByUserId
    };

    private string NormalizeLanguage(string? languageCode, bool rejectUnsupported = false)
    {
        if (string.IsNullOrWhiteSpace(languageCode)) return _requestLanguageProvider.LanguageCode;

        var normalized = languageCode.Trim().ToLowerInvariant();
        var separator = normalized.IndexOfAny(['-', '_']);
        if (separator > 0) normalized = normalized[..separator];
        if (BusinessLanguages.Supported.Contains(normalized)) return normalized;

        if (rejectUnsupported)
        {
            throw new ArgumentException("Supported language codes are vi and en.", nameof(languageCode));
        }

        return BusinessLanguages.Vietnamese;
    }

    private static string NormalizeEntityType(string entityType)
    {
        var normalized = entityType.Trim().ToLowerInvariant() switch
        {
            "lookup-categories" => BusinessDataEntityTypes.LookupCategory,
            "lookup-values" => BusinessDataEntityTypes.LookupValue,
            "vpp-categories" => BusinessDataEntityTypes.VppCategory,
            "vpp-items" => BusinessDataEntityTypes.VppItem,
            "suppliers" => BusinessDataEntityTypes.Supplier,
            "price-lists" => BusinessDataEntityTypes.PriceList,
            "departments" => BusinessDataEntityTypes.Department,
            var value => value
        };

        return BusinessDataEntityTypes.Supported.Contains(normalized)
            ? normalized
            : throw new ArgumentException($"Unsupported business data entity type '{entityType}'.", nameof(entityType));
    }

    private static BusinessTranslationStatus ParseStatus(string value)
        => Enum.TryParse<BusinessTranslationStatus>(value, ignoreCase: true, out var status)
            ? status
            : throw new ArgumentException("Translation status must be Draft or Approved.", nameof(value));

    private static BusinessTranslationSource ParseSource(string value)
        => Enum.TryParse<BusinessTranslationSource>(value, ignoreCase: true, out var source)
            ? source
            : throw new ArgumentException("Translation source must be Manual, Import or AiDraft.", nameof(value));

    private static string NormalizeRequired(string? value, string name, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) throw new ArgumentException("A translated name is required.", name);
        if (normalized.Length > maxLength) throw new ArgumentException($"Translated name exceeds {maxLength} characters.", name);
        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized)) return null;
        if (normalized.Length > maxLength) throw new ArgumentException($"Translated description exceeds {maxLength} characters.", nameof(value));
        return normalized;
    }

    private sealed record TranslationRow(Guid EntityId, string Name, string? Description);
}
