using System.Globalization;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using gtas_vpp_be.Model.Library;
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
        "Id", "VPPCode", "VPPName", "Description", "UOMId", "UOMCode", "UOMName",
        "VPPCategoryId", "VPPCategoryCode", "VPPCategoryName",
        "DefaultSupplierName", "DefaultPrice", "DefaultVatRate", "SupplierCount", "IsDeleted"
    };

    private static readonly HashSet<string> AllowedFilterTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "VPPCode", "VPPName", "Description", "UOMId", "UOMCode", "UOMName",
        "VPPCategoryId", "VPPCategoryCode", "VPPCategoryName",
        "DefaultSupplierName", "DefaultPrice", "DefaultVatRate", "SupplierCount", "IsDeleted",
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

    public VppCatalogService(IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<L04_VPPResDTO?> GetItemAsync(
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

    public async Task<(IReadOnlyList<L04_VPPResDTO> Items, int TotalCount)> QueryItemsAsync(
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

    public async Task<L04_VPPResDTO> CreateItemAsync(
        L04_VppCreateReqDTO request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var values = NormalizeAndValidate(request.VPPCode, request.VPPName, request.Description);
        await ValidateReferencesAsync(request.UOMId, request.VPPCategoryId, cancellationToken);
        await EnsureCodeAvailableAsync(values.Code, null, cancellationToken);

        var now = _dateTimeProvider.Now;
        var entity = new L04_VPP
        {
            Id = Guid.NewGuid(),
            VPPCode = values.Code,
            VPPName = values.Name,
            Description = values.Description,
            UOMId = request.UOMId,
            VPPCategoryId = request.VPPCategoryId,
            CreateUserId = userId,
            UpdateUserId = userId,
            CreateDate = now,
            UpdateDate = now,
            IsDeleted = false
        };

        _unitOfWork.VPPContext.Set<L04_VPP>().Add(entity);
        await SaveChangesAsync(cancellationToken);
        return await GetItemDtoAsync(entity.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {entity.Id} was not found after creation.");
    }

    public async Task<L04_VPPResDTO> UpdateItemAsync(
        L04_VppUpdateReqDTO request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var values = NormalizeAndValidate(request.VPPCode, request.VPPName, request.Description);
        var entity = await _unitOfWork.VPPContext.Set<L04_VPP>()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {request.Id} was not found.");

        await ValidateReferencesAsync(request.UOMId, request.VPPCategoryId, cancellationToken);
        await EnsureCodeAvailableAsync(values.Code, request.Id, cancellationToken);

        entity.VPPCode = values.Code;
        entity.VPPName = values.Name;
        entity.Description = values.Description;
        entity.UOMId = request.UOMId;
        entity.VPPCategoryId = request.VPPCategoryId;
        entity.UpdateUserId = userId;
        entity.UpdateDate = _dateTimeProvider.Now;

        await SaveChangesAsync(cancellationToken);
        return await GetItemDtoAsync(entity.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {entity.Id} was not found after update.");
    }

    public async Task<L04_VPPResDTO> SetItemStatusAsync(
        Guid id,
        L04_VppStatusReqDTO request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.VPPContext.Set<L04_VPP>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"Catalog item {id} was not found.");

        if (!request.IsDeleted)
        {
            await ValidateReferencesAsync(entity.UOMId, entity.VPPCategoryId, cancellationToken);
            await EnsureCodeAvailableAsync(entity.VPPCode, id, cancellationToken);
        }

        entity.IsDeleted = request.IsDeleted;
        entity.UpdateUserId = userId;
        entity.UpdateDate = _dateTimeProvider.Now;
        await SaveChangesAsync(cancellationToken);
        return await GetItemDtoAsync(entity.Id, cancellationToken, includeDeleted: true)
            ?? throw new KeyNotFoundException($"Catalog item {entity.Id} was not found after status change.");
    }

    private async Task<Guid?> GetDefaultPriceListIdAsync(CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<L07_PriceList>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsDefault)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<CatalogRow> BuildRowQuery(Guid? categoryId, string? search, bool showDeleted, Guid? defaultPriceListId)
    {
        var context = _unitOfWork.VPPContext;
        var baseQuery = context.Set<L04_VPP>().AsNoTracking();
        if (!showDeleted)
        {
            baseQuery = baseQuery.Where(x => !x.IsDeleted
                && x.UOM != null && !x.UOM.IsDeleted
                && (x.VPPCategory == null || !x.VPPCategory.IsDeleted));
        }

        if (categoryId.HasValue)
        {
            baseQuery = baseQuery.Where(x => x.VPPCategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            if (context.Database.IsSqlServer())
            {
                var pattern = $"%{EscapeLikePattern(search)}%";
                baseQuery = baseQuery.Where(x =>
                    (x.VPPCode != null && EF.Functions.Like(EF.Functions.Collate(x.VPPCode, SearchCollation), pattern, "\\"))
                    || (x.VPPName != null && EF.Functions.Like(EF.Functions.Collate(x.VPPName, SearchCollation), pattern, "\\"))
                    || (x.VPPCategory != null && x.VPPCategory.VPPCategoryCode != null && EF.Functions.Like(EF.Functions.Collate(x.VPPCategory.VPPCategoryCode, SearchCollation), pattern, "\\"))
                    || (x.VPPCategory != null && x.VPPCategory.VPPCategoryName != null && EF.Functions.Like(EF.Functions.Collate(x.VPPCategory.VPPCategoryName, SearchCollation), pattern, "\\"))
                    || (x.UOM != null && x.UOM.ClassDetailCode != null && EF.Functions.Like(EF.Functions.Collate(x.UOM.ClassDetailCode, SearchCollation), pattern, "\\"))
                    || (x.UOM != null && x.UOM.ClassDetailValue != null && EF.Functions.Like(EF.Functions.Collate(x.UOM.ClassDetailValue, SearchCollation), pattern, "\\")));
            }
            else
            {
                var searchUpper = search.ToUpper();
                baseQuery = baseQuery.Where(x =>
                    (x.VPPCode != null && x.VPPCode.ToUpper().Contains(searchUpper))
                    || (x.VPPName != null && x.VPPName.ToUpper().Contains(searchUpper))
                    || (x.VPPCategory != null && x.VPPCategory.VPPCategoryCode != null && x.VPPCategory.VPPCategoryCode.ToUpper().Contains(searchUpper))
                    || (x.VPPCategory != null && x.VPPCategory.VPPCategoryName != null && x.VPPCategory.VPPCategoryName.ToUpper().Contains(searchUpper))
                    || (x.UOM != null && x.UOM.ClassDetailCode != null && x.UOM.ClassDetailCode.ToUpper().Contains(searchUpper))
                    || (x.UOM != null && x.UOM.ClassDetailValue != null && x.UOM.ClassDetailValue.ToUpper().Contains(searchUpper)));
            }
        }

        return baseQuery.Select(x => new CatalogRow
        {
            Id = x.Id,
            VPPCode = x.VPPCode,
            VPPName = x.VPPName,
            Description = x.Description,
            UOMId = x.UOMId,
            UOMCode = x.UOM != null ? x.UOM.ClassDetailCode : null,
            UOMName = x.UOM != null ? x.UOM.ClassDetailValue : null,
            VPPCategoryId = x.VPPCategoryId,
            VPPCategoryCode = x.VPPCategory != null ? x.VPPCategory.VPPCategoryCode : null,
            VPPCategoryName = x.VPPCategory != null ? x.VPPCategory.VPPCategoryName : null,
            SupplierCount = x.L06_VPPSupplierMappings!.Count(m => !m.IsDeleted
                && m.L07_PriceListId == defaultPriceListId
                && m.L07_PriceList != null && !m.L07_PriceList.IsDeleted
                && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted)),
            DefaultVatRate = VppPricingDefaults.VatRate,
            DefaultPrice = x.L06_VPPSupplierMappings!
                .Where(m => !m.IsDeleted
                    && m.L07_PriceListId == defaultPriceListId
                    && m.L07_PriceList != null && !m.L07_PriceList.IsDeleted
                    && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted))
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.L05_VPPSupplier != null && m.L05_VPPSupplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                .ThenBy(m => m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null)
                .Select(m => (decimal?)m.Price)
                .FirstOrDefault(),
            DefaultSupplierName = x.L06_VPPSupplierMappings!
                .Where(m => !m.IsDeleted
                    && m.L07_PriceListId == defaultPriceListId
                    && m.L07_PriceList != null && !m.L07_PriceList.IsDeleted
                    && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted))
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.L05_VPPSupplier != null && m.L05_VPPSupplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                .ThenBy(m => m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null)
                .Select(m => m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null)
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
            "VPPCode" => (await query.Select(x => x.VPPCode).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VPPName" => (await query.Select(x => x.VPPName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "Description" => (await query.Select(x => x.Description).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "UOMId" => (await query.Select(x => x.UOMId).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "UOMCode" => (await query.Select(x => x.UOMCode).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "UOMName" => (await query.Select(x => x.UOMName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VPPCategoryId" => (await query.Select(x => x.VPPCategoryId).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VPPCategoryCode" => (await query.Select(x => x.VPPCategoryCode).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
            "VPPCategoryName" => (await query.Select(x => x.VPPCategoryName).Distinct().ToListAsync(cancellationToken)).Cast<object?>().ToList(),
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
            terms.Add(("VPPCode", false));
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

            result.Add((match.Groups["property"].Value, match.Groups["direction"].Value.Equals("desc", StringComparison.OrdinalIgnoreCase)));
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
        if (uomId == Guid.Empty || !await _unitOfWork.VPPContext.Set<L02_ClassDetail>().AnyAsync(x => x.Id == uomId && !x.IsDeleted, cancellationToken))
        {
            throw new BusinessException("The selected unit of measure is inactive or does not exist.");
        }

        if (categoryId == Guid.Empty || !await _unitOfWork.VPPContext.Set<L03_VPPCategory>().AnyAsync(x => x.Id == categoryId && !x.IsDeleted, cancellationToken))
        {
            throw new BusinessException("The selected category is inactive or does not exist.");
        }
    }

    private async Task EnsureCodeAvailableAsync(string? code, Guid? id, CancellationToken cancellationToken)
    {
        var normalized = code?.Trim();
        var normalizedUpper = normalized?.ToUpper();
        var exists = await _unitOfWork.VPPContext.Set<L04_VPP>()
            .AnyAsync(x => x.VPPCode != null
                && x.VPPCode.ToUpper() == normalizedUpper
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

    private async Task<L04_VPPResDTO?> GetItemDtoAsync(Guid id, CancellationToken cancellationToken, bool includeDeleted = false)
    {
        return await GetItemAsync(id, includeDeleted, cancellationToken);
    }

    private static L04_VPPResDTO ToDto(CatalogRow row) => new()
    {
        Id = row.Id,
        VPPCode = row.VPPCode,
        VPPName = row.VPPName,
        Description = row.Description,
        UOMId = row.UOMId,
        UOMCode = row.UOMCode,
        UOMName = row.UOMName,
        VPPCategoryId = row.VPPCategoryId,
        VPPCategoryCode = row.VPPCategoryCode,
        VPPCategoryName = row.VPPCategoryName,
        DefaultSupplierName = row.DefaultSupplierName,
        DefaultPrice = row.DefaultPrice,
        DefaultVatRate = row.DefaultVatRate,
        SupplierCount = row.SupplierCount,
        IsDeleted = row.IsDeleted,
        UOM = row.UOMCode is null && row.UOMName is null ? null : new L02_ClassDetailResDTO
        {
            Id = row.UOMId,
            ClassDetailCode = row.UOMCode,
            ClassDetailValue = row.UOMName
        },
        VPPCategory = row.VPPCategoryCode is null && row.VPPCategoryName is null ? null : new L03_VPPCategoryResDTO
        {
            Id = row.VPPCategoryId,
            VPPCategoryCode = row.VPPCategoryCode,
            VPPCategoryName = row.VPPCategoryName
        }
    };

    private static L04_VPPResDTO CreateDistinctDto(string property, object? value)
    {
        var dto = new L04_VPPResDTO();
        var propertyInfo = typeof(L04_VPPResDTO).GetProperty(property)
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
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public string? Description { get; set; }
        public Guid UOMId { get; set; }
        public string? UOMCode { get; set; }
        public string? UOMName { get; set; }
        public Guid VPPCategoryId { get; set; }
        public string? VPPCategoryCode { get; set; }
        public string? VPPCategoryName { get; set; }
        public int SupplierCount { get; set; }
        public decimal? DefaultPrice { get; set; }
        public decimal DefaultVatRate { get; set; }
        public string? DefaultSupplierName { get; set; }
        public bool IsDeleted { get; set; }
    }
}
