using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.Constants;
using Microsoft.EntityFrameworkCore;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Service.Services;

public sealed class VppCatalogService : IVppCatalogService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;
    private const string SearchCollation = "Vietnamese_100_CI_AI";

    private static readonly HashSet<string> AllowedColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "VppCode", "VppName", "Description", "UomId", "UomCode", "UomName",
        "VppCategoryId", "VppCategoryCode", "VppCategoryName",
        "MaxQuantityPerOrder", "DefaultSupplierName", "DefaultPrice", "DefaultVatRate", "SupplierCount", "IsDeleted"
    };

    private static readonly HashSet<string> AllowedFilterTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "VppCode", "VppName", "Description", "UomId", "UomCode", "UomName",
        "VppCategoryId", "VppCategoryCode", "VppCategoryName",
        "MaxQuantityPerOrder", "DefaultSupplierName", "DefaultPrice", "DefaultVatRate", "SupplierCount", "IsDeleted",
        "x", "it", "new", "Contains", "StartsWith", "EndsWith", "Equals", "ToLower", "ToUpper",
        "and", "or", "not", "true", "false", "null"
    };

    private static readonly Regex OrderTermRegex = new(
        @"^(?<property>[A-Za-z_][A-Za-z0-9_]*)(?:\s+(?<direction>asc|desc))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex FilterTokenRegex = new(
        @"\b[A-Za-z_][A-Za-z0-9_]*\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public VppCatalogService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<VppCategoryResDTO>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.VPPContext.Set<VppCategory>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Select(x => new VppCategoryResDTO
            {
                Id = x.Id,
                VppCategoryCode = x.VppCategoryCode,
                VppCategoryName = x.VppCategoryName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<VppItemResDTO?> GetItemAsync(
        Guid id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var defaultPriceListId = await GetDefaultPriceListIdAsync(cancellationToken);
        var row = await BuildRowQuery(null, null, includeDeleted, defaultPriceListId)
            .Where(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
        return row is null ? null : ToDto(row);
    }

    public async Task<(IReadOnlyList<VppItemResDTO> Items, int TotalCount)> QueryItemsAsync(
        Guid? categoryId,
        string? search,
        string? filter,
        int skip,
        int top,
        string? orderby,
        string? distinct,
        string? distinctFilter,
        bool showDeleted,
        CancellationToken cancellationToken = default)
    {
        skip = Math.Max(0, skip);
        top = top <= 0 ? DefaultPageSize : Math.Min(top, MaxPageSize);
        search = NormalizeOptional(search);

        var defaultPriceListId = await GetDefaultPriceListIdAsync(cancellationToken);
        var query = BuildRowQuery(categoryId, search, showDeleted, defaultPriceListId);

        if (!string.IsNullOrWhiteSpace(filter))
        {
            EnsureSafeFilter(filter);
            query = query.Where(filter);
        }

        if (!string.IsNullOrWhiteSpace(distinct))
        {
            var distinctValues = await ReadDistinctValuesAsync(query, distinct, distinctFilter, cancellationToken);
            var result = distinctValues
                .Skip(skip)
                .Take(top)
                .Select(value => CreateDistinctDto(distinct, value))
                .ToList();
            return (result, distinctValues.Count);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        query = ApplyOrdering(query, orderby);
        var rows = await query.Skip(skip).Take(top).ToListAsync(cancellationToken);
        return (rows.Select(ToDto).ToList(), totalCount);
    }

    public async Task<VppItemResDTO> CreateItemAsync(
        VppItemCreateRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var values = NormalizeAndValidate(request.VppCode, request.VppName, request.Description);
        ValidateMaxQuantityPerOrder(request.MaxQuantityPerOrder);
        await ValidateReferencesAsync(request.UomId, request.VppCategoryId, cancellationToken);
        await EnsureCodeAvailableAsync(values.Code, null, cancellationToken);

        var now = _dateTimeProvider.Now;
        var entity = new VppItem
        {
            Id = Guid.NewGuid(),
            VppCode = values.Code,
            VppName = values.Name,
            MaxQuantityPerOrder = request.MaxQuantityPerOrder,
            Description = values.Description,
            UomId = request.UomId,
            VppCategoryId = request.VppCategoryId,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        };

        _unitOfWork.VPPContext.Set<VppItem>().Add(entity);
        await SaveChangesAsync(cancellationToken);
        return await GetItemDtoAsync(entity.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {entity.Id} was not found after creation.");
    }

    public async Task<VppItemResDTO> UpdateItemAsync(
        VppItemUpdateRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var values = NormalizeAndValidate(request.VppCode, request.VppName, request.Description);
        ValidateMaxQuantityPerOrder(request.MaxQuantityPerOrder);
        var entity = await _unitOfWork.VPPContext.Set<VppItem>()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {request.Id} was not found.");

        await ValidateReferencesAsync(request.UomId, request.VppCategoryId, cancellationToken);
        await EnsureCodeAvailableAsync(values.Code, request.Id, cancellationToken);

        entity.VppCode = values.Code;
        entity.VppName = values.Name;
        entity.MaxQuantityPerOrder = request.MaxQuantityPerOrder;
        entity.Description = values.Description;
        entity.UomId = request.UomId;
        entity.VppCategoryId = request.VppCategoryId;
        entity.UpdatedByUserId = userId;
        entity.UpdatedAtUtc = _dateTimeProvider.Now;

        await SaveChangesAsync(cancellationToken);
        return await GetItemDtoAsync(entity.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {entity.Id} was not found after update.");
    }

    public async Task<VppItemResDTO> SetItemStatusAsync(
        Guid id,
        VppItemStatusRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.VPPContext.Set<VppItem>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {id} was not found.");

        if (!request.IsDeleted)
        {
            await ValidateReferencesAsync(entity.UomId, entity.VppCategoryId, cancellationToken);
            await EnsureCodeAvailableAsync(entity.VppCode, id, cancellationToken);
        }

        entity.IsDeleted = request.IsDeleted;
        entity.UpdatedByUserId = userId;
        entity.UpdatedAtUtc = _dateTimeProvider.Now;
        await SaveChangesAsync(cancellationToken);
        return await GetItemDtoAsync(entity.Id, cancellationToken, includeDeleted: true)
            ?? throw new KeyNotFoundException($"Catalog item {entity.Id} was not found after status change.");
    }

    public async Task<LibraryHardDeleteResult> HardDeleteItemAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var context = _unitOfWork.VPPContext;
            var entity = await context.VppItems.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity is null)
            {
                await _unitOfWork.RollbackAsync();
                return new LibraryHardDeleteResult(LibraryHardDeleteStatus.NotFound, 0);
            }

            if (!entity.IsDeleted)
            {
                await _unitOfWork.RollbackAsync();
                return new LibraryHardDeleteResult(LibraryHardDeleteStatus.MustDeactivate, 0);
            }

            var referenceCount = await context.SupplierProductMappings.CountAsync(x => x.VppItemId == id, cancellationToken)
                + await context.RequestDetails.CountAsync(x => x.VppId == id, cancellationToken)
                + await context.SettlementItems.CountAsync(x => x.VppId == id, cancellationToken);
            if (referenceCount > 0)
            {
                await _unitOfWork.RollbackAsync();
                return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, referenceCount);
            }

            context.VppItems.Remove(entity);
            await _unitOfWork.CommitAsync();
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.Deleted, 0);
        }
        catch (DbUpdateException)
        {
            await _unitOfWork.RollbackAsync();
            return new LibraryHardDeleteResult(LibraryHardDeleteStatus.HasDependencies, 1);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    private async Task<Guid?> GetDefaultPriceListIdAsync(CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsDefault)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<CatalogRow> BuildRowQuery(Guid? categoryId, string? search, bool showDeleted, Guid? defaultPriceListId)
    {
        var context = _unitOfWork.VPPContext;
        var baseQuery = context.Set<VppItem>().AsNoTracking();
        if (!showDeleted)
        {
            baseQuery = baseQuery.Where(x => !x.IsDeleted
                && x.Uom != null && !x.Uom.IsDeleted
                && (x.VppCategory == null || !x.VppCategory.IsDeleted));
        }

        if (categoryId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.VppCategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (context.Database.IsSqlServer())
            {
                var pattern = $"%{EscapeLikePattern(search)}%";
                baseQuery = baseQuery.Where(x =>
                    (x.VppCode != null && EF.Functions.Like(EF.Functions.Collate(x.VppCode, SearchCollation), pattern, "\\"))
                    || (x.VppName != null && EF.Functions.Like(EF.Functions.Collate(x.VppName, SearchCollation), pattern, "\\"))
                    || (x.VppCategory != null && x.VppCategory.VppCategoryCode != null && EF.Functions.Like(EF.Functions.Collate(x.VppCategory.VppCategoryCode, SearchCollation), pattern, "\\"))
                    || (x.VppCategory != null && x.VppCategory.VppCategoryName != null && EF.Functions.Like(EF.Functions.Collate(x.VppCategory.VppCategoryName, SearchCollation), pattern, "\\"))
                    || (x.Uom != null && x.Uom.Code != null && EF.Functions.Like(EF.Functions.Collate(x.Uom.Code, SearchCollation), pattern, "\\"))
                    || (x.Uom != null && x.Uom.Value != null && EF.Functions.Like(EF.Functions.Collate(x.Uom.Value, SearchCollation), pattern, "\\")));
            }
            else
            {
                var searchUpper = search.ToUpper();
                baseQuery = baseQuery.Where(x =>
                    (x.VppCode != null && x.VppCode.ToUpper().Contains(searchUpper))
                    || (x.VppName != null && x.VppName.ToUpper().Contains(searchUpper))
                    || (x.VppCategory != null && x.VppCategory.VppCategoryCode != null && x.VppCategory.VppCategoryCode.ToUpper().Contains(searchUpper))
                    || (x.VppCategory != null && x.VppCategory.VppCategoryName != null && x.VppCategory.VppCategoryName.ToUpper().Contains(searchUpper))
                    || (x.Uom != null && x.Uom.Code != null && x.Uom.Code.ToUpper().Contains(searchUpper))
                    || (x.Uom != null && x.Uom.Value != null && x.Uom.Value.ToUpper().Contains(searchUpper)));
            }
        }

        return baseQuery.Select(x => new CatalogRow
        {
            Id = x.Id,
            VppCode = x.VppCode,
            VppName = x.VppName,
            MaxQuantityPerOrder = x.MaxQuantityPerOrder,
            Description = x.Description,
            UomId = x.UomId,
            UomCode = x.Uom != null ? x.Uom.Code : null,
            UomName = x.Uom != null ? x.Uom.Value : null,
            VppCategoryId = x.VppCategoryId,
            VppCategoryCode = x.VppCategory != null ? x.VppCategory.VppCategoryCode : null,
            VppCategoryName = x.VppCategory != null ? x.VppCategory.VppCategoryName : null,
            SupplierCount = x.SupplierProductMappings!.Count(m => !m.IsDeleted
                && m.PriceListId == defaultPriceListId
                && m.PriceList != null && !m.PriceList.IsDeleted
                && (m.Supplier == null || !m.Supplier.IsDeleted)),
            DefaultVatRate = VppPricingDefaults.VatRate,
            DefaultPrice = x.SupplierProductMappings!
                .Where(m => !m.IsDeleted
                    && m.PriceListId == defaultPriceListId
                    && m.PriceList != null && !m.PriceList.IsDeleted
                    && (m.Supplier == null || !m.Supplier.IsDeleted))
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.Supplier != null && m.Supplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                .ThenBy(m => m.Supplier != null ? m.Supplier.SupplierName : null)
                .Select(m => (decimal?)m.Price)
                .FirstOrDefault(),
            DefaultSupplierName = x.SupplierProductMappings!
                .Where(m => !m.IsDeleted
                    && m.PriceListId == defaultPriceListId
                    && m.PriceList != null && !m.PriceList.IsDeleted
                    && (m.Supplier == null || !m.Supplier.IsDeleted))
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.Supplier != null && m.Supplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                .ThenBy(m => m.Supplier != null ? m.Supplier.SupplierName : null)
                .Select(m => m.Supplier == null ? null : m.Supplier.SupplierName)
                .FirstOrDefault(),
            IsDeleted = x.IsDeleted
        });
    }

    private async Task<List<object?>> ReadDistinctValuesAsync(
        IQueryable<CatalogRow> query,
        string property,
        string? distinctFilter,
        CancellationToken cancellationToken)
    {
        EnsureAllowedColumn(property);
        var values = property switch
        {
            "Id" => (await query.Select(x => x.Id).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VppCode" => (await query.Select(x => x.VppCode).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VppName" => (await query.Select(x => x.VppName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "Description" => (await query.Select(x => x.Description).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "UomId" => (await query.Select(x => x.UomId).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "UomCode" => (await query.Select(x => x.UomCode).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "UomName" => (await query.Select(x => x.UomName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VppCategoryId" => (await query.Select(x => x.VppCategoryId).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VppCategoryCode" => (await query.Select(x => x.VppCategoryCode).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VppCategoryName" => (await query.Select(x => x.VppCategoryName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "MaxQuantityPerOrder" => (await query.Select(x => x.MaxQuantityPerOrder).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "DefaultSupplierName" => (await query.Select(x => x.DefaultSupplierName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "DefaultPrice" => (await query.Select(x => x.DefaultPrice).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "DefaultVatRate" => (await query.Select(x => x.DefaultVatRate).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "SupplierCount" => (await query.Select(x => x.SupplierCount).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "IsDeleted" => (await query.Select(x => x.IsDeleted).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            _ => throw new ArgumentException($"Distinct column '{property}' is not supported.")
        };

        return values
            .Where(value => string.IsNullOrWhiteSpace(distinctFilter)
                || (value?.ToString()?.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderBy(value => value?.ToString(), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IQueryable<CatalogRow> ApplyOrdering(IQueryable<CatalogRow> query, string? orderby)
    {
        var terms = ParseOrderBy(orderby);
        if (terms.Count == 0)
        {
            terms.Add(("VppCode", false));
            terms.Add(("Id", false));
        }

        var expression = string.Join(", ", terms.Select(x => $"{x.Property} {(x.Descending ? "desc" : "asc")}"));
        return query.OrderBy(expression);
    }

    private static List<(string Property, bool Descending)> ParseOrderBy(string? orderby)
    {
        var result = new List<(string Property, bool Descending)>();
        if (string.IsNullOrWhiteSpace(orderby)) return result;

        foreach (var rawTerm in orderby.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var match = OrderTermRegex.Match(rawTerm);
            if (!match.Success || !AllowedColumns.Contains(match.Groups["property"].Value))
            {
                throw new ArgumentException("The requested sort column is not supported.");
            }

            var property = match.Groups["property"].Value;
            property = property switch
            {
                "VppName" => "VppName",
                "VppCategoryName" => "VppCategoryName",
                "UomName" => "UomName",
                _ => property
            };
            result.Add((property, match.Groups["direction"].Value.Equals("desc", StringComparison.OrdinalIgnoreCase)));
        }

        return result;
    }

    private static void EnsureSafeFilter(string filter)
    {
        if (filter.Length > 1_000)
        {
            throw new ArgumentException("The filter is too long.");
        }

        var withoutLiterals = Regex.Replace(filter, @"""(?:\\.|[^""\\])*""|'(?:''|[^'])*'", " ");
        foreach (Match token in FilterTokenRegex.Matches(withoutLiterals))
        {
            if (!AllowedFilterTokens.Contains(token.Value))
            {
                throw new ArgumentException($"Filter member '{token.Value}' is not supported.");
            }
        }
    }

    private static void EnsureAllowedColumn(string property)
    {
        if (!AllowedColumns.Contains(property))
        {
            throw new ArgumentException($"Column '{property}' is not supported.");
        }
    }

    private async Task ValidateReferencesAsync(Guid uomId, Guid categoryId, CancellationToken cancellationToken)
    {
        if (uomId == Guid.Empty || !await _unitOfWork.VPPContext.Set<LookupValue>().AnyAsync(x => x.Id == uomId && !x.IsDeleted, cancellationToken))
        {
            throw new BusinessException("The selected unit of measure is inactive or does not exist.");
        }

        if (categoryId == Guid.Empty || !await _unitOfWork.VPPContext.Set<VppCategory>().AnyAsync(x => x.Id == categoryId && !x.IsDeleted, cancellationToken))
        {
            throw new BusinessException("The selected category is inactive or does not exist.");
        }
    }

    private async Task EnsureCodeAvailableAsync(string? code, Guid? id, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim();
        var normalizedUpper = normalized?.ToUpper();
        var exists = await _unitOfWork.VPPContext.Set<VppItem>()
            .AnyAsync(x => x.VppCode != null
                && x.VppCode.ToUpper() == normalizedUpper
                && (!id.HasValue || x.Id != id.Value), cancellationToken);

        if (exists)
        {
            throw new ConflictException($"VPP code '{normalized}' already exists.");
        }
    }

    private static (string Code, string Name, string? Description) NormalizeAndValidate(string? code, string? name, string? description)
    {
        var normalizedCode = code?.Trim();
        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode)) throw new BusinessException("VPP code is required.");
        if (string.IsNullOrWhiteSpace(normalizedName)) throw new BusinessException("VPP name is required.");
        if (normalizedCode.Length > 64) throw new BusinessException("VPP code must be 64 characters or fewer.");
        if (normalizedName.Length > 250) throw new BusinessException("VPP name must be 250 characters or fewer.");
        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (normalizedDescription?.Length > 500) throw new BusinessException("Description must be 500 characters or fewer.");
        return (normalizedCode, normalizedName, normalizedDescription);
    }

    private static void ValidateMaxQuantityPerOrder(int value)
    {
        if (value is < VppOrderQuantityLimits.Minimum or > VppOrderQuantityLimits.Maximum)
        {
            throw new BusinessException(
                $"Số lượng tối đa mỗi đơn phải từ {VppOrderQuantityLimits.Minimum} đến {VppOrderQuantityLimits.Maximum}.");
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.VPPContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new ConflictException("The catalog item conflicts with an existing record.", ex);
        }
    }

    private async Task<VppItemResDTO?> GetItemDtoAsync(Guid id, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        return await GetItemAsync(id, includeDeleted, cancellationToken);
    }

    private static VppItemResDTO ToDto(CatalogRow row) => new()
    {
        Id = row.Id,
        VppCode = row.VppCode,
        VppName = row.VppName,
        MaxQuantityPerOrder = row.MaxQuantityPerOrder,
        Description = row.Description,
        UomId = row.UomId,
        UomCode = row.UomCode,
        UomName = row.UomName,
        VppCategoryId = row.VppCategoryId,
        VppCategoryCode = row.VppCategoryCode,
        VppCategoryName = row.VppCategoryName,
        DefaultSupplierName = row.DefaultSupplierName,
        DefaultPrice = row.DefaultPrice,
        DefaultVatRate = row.DefaultVatRate,
        SupplierCount = row.SupplierCount,
        IsDeleted = row.IsDeleted,
        Uom = row.UomCode is null && row.UomName is null ? null : new LookupValueResDTO
        {
            Id = row.UomId,
            Code = row.UomCode,
            Value = row.UomName
        },
        VppCategory = row.VppCategoryCode is null && row.VppCategoryName is null ? null : new VppCategoryResDTO
        {
            Id = row.VppCategoryId,
            VppCategoryCode = row.VppCategoryCode,
            VppCategoryName = row.VppCategoryName
        }
    };

    private static VppItemResDTO CreateDistinctDto(string property, object? value)
    {
        var dto = new VppItemResDTO();
        var propertyInfo = typeof(VppItemResDTO).GetProperty(property)
            ?? throw new ArgumentException($"Column '{property}' is not a supported response property.");
        if (value is not null)
        {
            var targetType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            propertyInfo.SetValue(dto, targetType == value.GetType() ? value : Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture));
        }
        return dto;
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string EscapeLikePattern(string value)
        => value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal)
            .Replace("[", "\\[", StringComparison.Ordinal);

    private sealed class CatalogRow
    {
        public Guid Id { get; set; }
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public int MaxQuantityPerOrder { get; set; }
        public string? Description { get; set; }
        public Guid UomId { get; set; }
        public string? UomCode { get; set; }
        public string? UomName { get; set; }
        public Guid VppCategoryId { get; set; }
        public string? VppCategoryCode { get; set; }
        public string? VppCategoryName { get; set; }
        public int SupplierCount { get; set; }
        public decimal? DefaultPrice { get; set; }
        public decimal DefaultVatRate { get; set; }
        public string? DefaultSupplierName { get; set; }
        public bool IsDeleted { get; set; }
    }
}
