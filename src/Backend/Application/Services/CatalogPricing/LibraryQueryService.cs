using System.Globalization;
using System.Linq.Dynamic.Core;
using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Library;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Đọc danh mục quản trị và bổ sung số liệu liên quan; controller chỉ còn định tuyến và ánh xạ HTTP.
/// </summary>
public sealed class LibraryQueryService : ILibraryQueryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserNameResolver _userNameResolver;

    public LibraryQueryService(IUnitOfWork unitOfWork, IUserNameResolver userNameResolver)
    {
        _unitOfWork = unitOfWork;
        _userNameResolver = userNameResolver;
    }

    public async Task<LibraryQueryResult> QueryAsync(
        string tableCode,
        LibraryQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var cleanSearch = request.SearchText?.Trim() ?? string.Empty;
        var normalizedTableCode = tableCode.Trim().ToLowerInvariant();
        var isLoadDataRequest = !string.IsNullOrEmpty(request.Filter)
            || request.Skip.HasValue
            || request.Top.HasValue
            || !string.IsNullOrEmpty(request.OrderBy)
            || !string.IsNullOrEmpty(request.Distinct)
            || !string.IsNullOrEmpty(request.DistinctFilter);

        if (normalizedTableCode == "lookup-values"
            && request.LookupCategoryId.HasValue
            && !request.Id.HasValue
            && string.IsNullOrEmpty(cleanSearch))
        {
            isLoadDataRequest = true;
        }

        if (isLoadDataRequest)
        {
            return normalizedTableCode switch
            {
                "lookup-categories" => await QueryTableAsync<LookupCategory, LookupCategoryResDTO>(request, null, cleanSearch, cancellationToken),
                "lookup-values" => await QueryTableAsync<LookupValue, LookupValueResDTO>(request, request.LookupCategoryId, cleanSearch, cancellationToken),
                "vpp-categories" => await QueryTableAsync<VppCategory, VppCategoryResDTO>(request, null, cleanSearch, cancellationToken),
                "vpp-items" => await QueryVppItemsAsync(request, cleanSearch, cancellationToken),
                "suppliers" => await QueryTableAsync<Supplier, SupplierResDTO>(request, null, cleanSearch, cancellationToken),
                "supplier-product-mappings" => await QueryTableAsync<SupplierProductMapping, SupplierProductMappingResDTO>(request, null, null, cancellationToken),
                "departments" => await QueryTableAsync<Department, DepartmentResDTO>(request, null, cleanSearch, cancellationToken),
                _ => new LibraryQueryResult(
                    null,
                    ErrorMessage: $"Advanced filtering for Table Code '{tableCode}' is not supported.")
            };
        }

        return normalizedTableCode switch
        {
            "lookup-categories" => await QuerySimpleAsync<LookupCategory, LookupCategoryResDTO>(request, cleanSearch, cancellationToken),
            "lookup-values" => await QuerySimpleAsync<LookupValue, LookupValueResDTO>(request, cleanSearch, cancellationToken),
            "vpp-categories" => await QuerySimpleAsync<VppCategory, VppCategoryResDTO>(request, cleanSearch, cancellationToken),
            "vpp-items" => await QuerySimpleVppItemsAsync(request, cleanSearch, cancellationToken),
            "suppliers" => await QuerySimpleAsync<Supplier, SupplierResDTO>(request, cleanSearch, cancellationToken),
            "supplier-product-mappings" => await QuerySimpleAsync<SupplierProductMapping, SupplierProductMappingResDTO>(request, cleanSearch, cancellationToken),
            "departments" => await QuerySimpleAsync<Department, DepartmentResDTO>(request, cleanSearch, cancellationToken),
            _ => new LibraryQueryResult(null, ErrorMessage: $"Table Code '{tableCode}' is not supported.")
        };
    }

    public async Task<LibraryQueryResult> GetByIdAsync(
        string tableCode,
        Guid id,
        bool showDeleted,
        CancellationToken cancellationToken = default)
    {
        return tableCode.Trim().ToLowerInvariant() switch
        {
            "lookup-categories" => await GetGenericByIdAsync<LookupCategory, LookupCategoryResDTO>(id, cancellationToken),
            "lookup-values" => await GetGenericByIdAsync<LookupValue, LookupValueResDTO>(id, cancellationToken),
            "vpp-categories" => await GetGenericByIdAsync<VppCategory, VppCategoryResDTO>(id, cancellationToken),
            "vpp-items" => await GetVppItemByIdAsync(id, showDeleted, cancellationToken),
            "suppliers" => await GetGenericByIdAsync<Supplier, SupplierResDTO>(id, cancellationToken),
            "supplier-product-mappings" => await GetGenericByIdAsync<SupplierProductMapping, SupplierProductMappingResDTO>(id, cancellationToken),
            "departments" => await GetGenericByIdAsync<Department, DepartmentResDTO>(id, cancellationToken),
            _ => new LibraryQueryResult(null, ErrorMessage: $"GetById for Table Code '{tableCode}' is not supported.")
        };
    }

    private async Task<LibraryQueryResult> QuerySimpleAsync<TModel, TDto>(
        LibraryQueryRequest request,
        string cleanSearch,
        CancellationToken cancellationToken)
        where TModel : BaseModel
        where TDto : class
    {
        IQueryable<TModel> query = _unitOfWork.VPPContext.Set<TModel>().AsNoTracking();
        if (!request.ShowDeleted)
        {
            query = query.Where(row => !row.IsDeleted);
        }

        if (request.Id.HasValue)
        {
            query = query.Where(row => row.Id == request.Id.Value);
        }
        else if (!string.IsNullOrEmpty(cleanSearch))
        {
            query = ApplyLibrarySearch(query, cleanSearch);
        }
        else
        {
            query = query.Take(1000);
        }

        var entities = await query.ToListAsync(cancellationToken);
        var enriched = await _userNameResolver.WithUserNamesAsync(entities, _unitOfWork.VPPContext);
        return new LibraryQueryResult(enriched.Adapt<List<TDto>>(), entities.Count);
    }

    private async Task<LibraryQueryResult> QuerySimpleVppItemsAsync(
        LibraryQueryRequest request,
        string cleanSearch,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .Include(item => item.Uom)
            .Include(item => item.VppCategory)
            .AsQueryable();

        if (!request.ShowDeleted)
        {
            query = query.Where(item => !item.IsDeleted);
        }

        if (request.Id.HasValue)
        {
            query = query.Where(item => item.Id == request.Id.Value);
        }

        query = ApplyVppItemSearch(query, cleanSearch);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(item => item.VppCode).Take(5000).ToListAsync(cancellationToken);
        if (items.Count == 0)
        {
            return new LibraryQueryResult(new List<VppItemResDTO>(), 0);
        }

        var defaultPriceListId = await GetDefaultPriceListIdAsync(cancellationToken);
        var itemIds = items.Select(item => item.Id).ToList();
        var mappings = await _unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .Where(mapping => (request.ShowDeleted || !mapping.IsDeleted)
                && mapping.PriceListId == defaultPriceListId
                && itemIds.Contains(mapping.VppItemId)
                && (mapping.Supplier == null || request.ShowDeleted || !mapping.Supplier.IsDeleted))
            .Select(mapping => new
            {
                mapping.VppItemId,
                mapping.SupplierId,
                mapping.Price,
                mapping.IsDefault,
                SupplierShortName = mapping.Supplier != null ? mapping.Supplier.SupplierShortName : null,
                SupplierName = mapping.Supplier != null ? mapping.Supplier.SupplierName : null
            })
            .ToListAsync(cancellationToken);

        var mappingLookup = mappings
            .GroupBy(mapping => mapping.VppItemId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var bestMapping = group
                        .OrderByDescending(mapping => mapping.IsDefault)
                        .ThenBy(mapping => mapping.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                        .ThenBy(mapping => mapping.SupplierName)
                        .FirstOrDefault();
                    return new
                    {
                        Price = bestMapping?.Price,
                        SupplierName = bestMapping?.SupplierName,
                        SupplierCount = group.Select(mapping => mapping.SupplierId).Distinct().Count()
                    };
                });

        var rows = items.Select(item =>
        {
            mappingLookup.TryGetValue(item.Id, out var priceInfo);
            return ToVppItemDto(item, priceInfo?.Price, priceInfo?.SupplierName, priceInfo?.SupplierCount ?? 0);
        }).ToList();
        return new LibraryQueryResult(rows, totalCount);
    }

    private async Task<LibraryQueryResult> QueryVppItemsAsync(
        LibraryQueryRequest request,
        string cleanSearch,
        CancellationToken cancellationToken)
    {
        try
        {
            var defaultPriceListId = await GetDefaultPriceListIdAsync(cancellationToken);
            var baseQuery = _unitOfWork.VPPContext.Set<VppItem>().AsNoTracking();
            if (!request.ShowDeleted)
            {
                baseQuery = baseQuery.Where(item => !item.IsDeleted);
            }

            baseQuery = ApplyVppItemSearch(baseQuery, cleanSearch);

            var query = baseQuery.Select(item => new VppItemResDTO
            {
                Id = item.Id,
                Description = item.Description,
                CreatedByUserId = item.CreatedByUserId,
                CreatedAtUtc = item.CreatedAtUtc,
                UpdatedByUserId = item.UpdatedByUserId,
                UpdatedAtUtc = item.UpdatedAtUtc,
                IsDeleted = item.IsDeleted,
                VppCode = item.VppCode,
                VppName = item.VppName,
                MaxQuantityPerOrder = item.MaxQuantityPerOrder,
                UomId = item.UomId,
                VppCategoryId = item.VppCategoryId,
                DefaultVatRate = VppPricingDefaults.VatRate,
                DefaultPrice = item.SupplierProductMappings!
                    .Where(mapping => (request.ShowDeleted || !mapping.IsDeleted)
                        && mapping.PriceListId == defaultPriceListId
                        && (mapping.Supplier == null || request.ShowDeleted || !mapping.Supplier.IsDeleted))
                    .OrderByDescending(mapping => mapping.IsDefault)
                    .ThenBy(mapping => mapping.Supplier != null && mapping.Supplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                    .ThenBy(mapping => mapping.Supplier != null ? mapping.Supplier.SupplierName : null)
                    .Select(mapping => (decimal?)mapping.Price)
                    .FirstOrDefault(),
                DefaultSupplierName = item.SupplierProductMappings!
                    .Where(mapping => (request.ShowDeleted || !mapping.IsDeleted)
                        && mapping.PriceListId == defaultPriceListId
                        && (mapping.Supplier == null || request.ShowDeleted || !mapping.Supplier.IsDeleted))
                    .OrderByDescending(mapping => mapping.IsDefault)
                    .ThenBy(mapping => mapping.Supplier != null && mapping.Supplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                    .ThenBy(mapping => mapping.Supplier != null ? mapping.Supplier.SupplierName : null)
                    .Select(mapping => mapping.Supplier != null ? mapping.Supplier.SupplierName : null)
                    .FirstOrDefault(),
                SupplierCount = item.SupplierProductMappings!
                    .Where(mapping => (request.ShowDeleted || !mapping.IsDeleted)
                        && mapping.PriceListId == defaultPriceListId
                        && (mapping.Supplier == null || request.ShowDeleted || !mapping.Supplier.IsDeleted))
                    .Select(mapping => mapping.SupplierId)
                    .Distinct()
                    .Count(),
                Uom = item.Uom == null ? null : new LookupValueResDTO
                {
                    Id = item.Uom.Id,
                    Description = item.Uom.Description,
                    CreatedByUserId = item.Uom.CreatedByUserId,
                    CreatedAtUtc = item.Uom.CreatedAtUtc,
                    UpdatedByUserId = item.Uom.UpdatedByUserId,
                    UpdatedAtUtc = item.Uom.UpdatedAtUtc,
                    IsDeleted = item.Uom.IsDeleted,
                    LookupCategoryId = item.Uom.LookupCategoryId,
                    Code = item.Uom.Code,
                    Value = item.Uom.Value,
                    ExtraField1 = item.Uom.ExtraField1,
                    ExtraField2 = item.Uom.ExtraField2,
                    ExtraField3 = item.Uom.ExtraField3,
                    Sort = item.Uom.Sort
                },
                VppCategory = item.VppCategory == null ? null : new VppCategoryResDTO
                {
                    Id = item.VppCategory.Id,
                    Description = item.VppCategory.Description,
                    CreatedByUserId = item.VppCategory.CreatedByUserId,
                    CreatedAtUtc = item.VppCategory.CreatedAtUtc,
                    UpdatedByUserId = item.VppCategory.UpdatedByUserId,
                    UpdatedAtUtc = item.VppCategory.UpdatedAtUtc,
                    IsDeleted = item.VppCategory.IsDeleted,
                    VppCategoryCode = item.VppCategory.VppCategoryCode,
                    VppCategoryName = item.VppCategory.VppCategoryName
                }
            });

            if (!string.IsNullOrWhiteSpace(request.Filter))
            {
                try { query = query.Where(request.Filter); }
                catch (Exception exception) { Log.Warning(exception, "VPP item filter parse failed"); }
            }

            if (!string.IsNullOrWhiteSpace(request.Distinct))
            {
                var propertyInfo = typeof(VppItemResDTO).GetProperty(request.Distinct);
                if (propertyInfo != null)
                {
                    var distinctValues = await query.Select(request.Distinct).Distinct().ToDynamicListAsync();
                    var normalizedDistinctFilter = VietnameseSearch.Normalize(request.DistinctFilter);
                    var filteredValues = distinctValues
                        .Where(value => value != null)
                        .Where(value => VietnameseSearch.Contains(
                            Convert.ToString(value, CultureInfo.CurrentCulture),
                            normalizedDistinctFilter))
                        .ToList();
                    IEnumerable<object> pageValues = filteredValues.Cast<object>();
                    if (request.Skip is > 0) pageValues = pageValues.Skip(request.Skip.Value);
                    if (request.Top is > 0) pageValues = pageValues.Take(request.Top.Value);
                    var distinctRows = pageValues.Select(value =>
                    {
                        var dto = new VppItemResDTO();
                        propertyInfo.SetValue(dto, value);
                        return dto;
                    }).ToList();
                    return new LibraryQueryResult(distinctRows, filteredValues.Count);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(request.OrderBy))
            {
                try { query = query.OrderBy(request.OrderBy); }
                catch { query = query.OrderBy(item => item.VppCode); }
            }
            else
            {
                query = query.OrderBy(item => item.VppCode);
            }

            if (request.Skip is > 0) query = query.Skip(request.Skip.Value);
            if (request.Top is > 0) query = query.Take(request.Top.Value);
            var rows = await query.ToListAsync(cancellationToken);
            var enriched = await _userNameResolver.WithUserNamesAsync(rows, _unitOfWork.VPPContext);
            return new LibraryQueryResult(enriched, totalCount);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "VPP item query failed");
            return new LibraryQueryResult(null, ErrorMessage: "An error occurred while processing VPP item data.");
        }
    }

    private async Task<LibraryQueryResult> QueryTableAsync<TModel, TDto>(
        LibraryQueryRequest request,
        Guid? lookupCategoryId,
        string? searchText,
        CancellationToken cancellationToken)
        where TModel : BaseModel
        where TDto : class
    {
        try
        {
            IQueryable<TModel> query = _unitOfWork.VPPContext.Set<TModel>().AsNoTracking();
            if (!request.ShowDeleted)
            {
                query = query.Where(row => !row.IsDeleted);
            }

            if (lookupCategoryId.HasValue && typeof(TModel) == typeof(LookupValue))
            {
                query = (IQueryable<TModel>)((IQueryable<LookupValue>)query)
                    .Where(value => value.LookupCategoryId == lookupCategoryId.Value);
            }

            query = ApplyLibrarySearch(query, searchText);
            if (!string.IsNullOrEmpty(request.Filter))
            {
                try { query = query.Where(request.Filter); }
                catch (Exception exception) { Log.Warning(exception, "Library filter parse failed"); }
            }

            if (!string.IsNullOrEmpty(request.Distinct))
            {
                var propertyInfo = typeof(TModel).GetProperty(request.Distinct);
                if (propertyInfo != null)
                {
                    var distinctValues = await query.Select(request.Distinct).Distinct().ToDynamicListAsync();
                    var normalizedDistinctFilter = VietnameseSearch.Normalize(request.DistinctFilter);
                    var filteredValues = distinctValues
                        .Where(value => value != null)
                        .Where(value => VietnameseSearch.Contains(
                            Convert.ToString(value, CultureInfo.CurrentCulture),
                            normalizedDistinctFilter))
                        .ToList();
                    IEnumerable<object> pageValues = filteredValues.Cast<object>();
                    if (request.Skip is > 0) pageValues = pageValues.Skip(request.Skip.Value);
                    if (request.Top is > 0) pageValues = pageValues.Take(request.Top.Value);
                    var distinctRows = pageValues.Select(value =>
                    {
                        var dto = Activator.CreateInstance<TDto>();
                        typeof(TDto).GetProperty(request.Distinct)?.SetValue(dto, value);
                        return dto;
                    }).ToList();
                    return new LibraryQueryResult(distinctRows, filteredValues.Count);
                }
            }

            var totalCount = await query.CountAsync(cancellationToken);
            if (!string.IsNullOrEmpty(request.OrderBy))
            {
                try { query = query.OrderBy(request.OrderBy); }
                catch { query = query.OrderBy(row => row.Id); }
            }
            else
            {
                query = query.OrderBy(row => row.Id);
            }

            if (request.Skip is > 0) query = query.Skip(request.Skip.Value);
            if (request.Top is > 0) query = query.Take(request.Top.Value);
            var entities = await query.ToListAsync(cancellationToken);
            var enriched = await _userNameResolver.WithUserNamesAsync(entities, _unitOfWork.VPPContext);
            var rows = enriched.Adapt<List<TDto>>();
            await EnrichAdministrationDtosAsync(rows, cancellationToken);
            return new LibraryQueryResult(rows, totalCount);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Library query failed");
            return new LibraryQueryResult(null, ErrorMessage: "An error occurred while processing the request.");
        }
    }

    private async Task<LibraryQueryResult> GetGenericByIdAsync<TModel, TDto>(
        Guid id,
        CancellationToken cancellationToken)
        where TModel : class
        where TDto : class
    {
        var entity = await _unitOfWork.VPPContext.Set<TModel>().FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            await _userNameResolver.IncludeUserInfoAsync(entity, _unitOfWork.VPPContext);
        }

        return new LibraryQueryResult(entity?.Adapt<TDto>());
    }

    private async Task<LibraryQueryResult> GetVppItemByIdAsync(
        Guid id,
        bool showDeleted,
        CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.VPPContext.Set<VppItem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == id && (showDeleted || !row.IsDeleted), cancellationToken);
        if (item is null)
        {
            return new LibraryQueryResult(null, NotFound: true);
        }

        var defaultPriceListId = await GetDefaultPriceListIdAsync(cancellationToken);
        var mappings = await _unitOfWork.VPPContext.Set<SupplierProductMapping>()
            .AsNoTracking()
            .Where(mapping => (showDeleted || !mapping.IsDeleted)
                && mapping.PriceListId == defaultPriceListId
                && mapping.VppItemId == id
                && (mapping.Supplier == null || showDeleted || !mapping.Supplier.IsDeleted))
            .Select(mapping => new
            {
                mapping.SupplierId,
                mapping.Price,
                mapping.IsDefault,
                SupplierShortName = mapping.Supplier != null ? mapping.Supplier.SupplierShortName : null,
                SupplierName = mapping.Supplier != null ? mapping.Supplier.SupplierName : null
            })
            .ToListAsync(cancellationToken);
        var bestMapping = mappings
            .OrderByDescending(mapping => mapping.IsDefault)
            .ThenBy(mapping => mapping.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
            .ThenBy(mapping => mapping.SupplierName)
            .FirstOrDefault();
        return new LibraryQueryResult(ToVppItemDto(
            item,
            bestMapping?.Price,
            bestMapping?.SupplierName,
            mappings.Select(mapping => mapping.SupplierId).Distinct().Count()));
    }

    private async Task<Guid> GetDefaultPriceListIdAsync(CancellationToken cancellationToken)
        => await _unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Where(priceList => !priceList.IsDeleted && priceList.IsDefault)
            .Select(priceList => priceList.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task EnrichAdministrationDtosAsync<TDto>(List<TDto> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0) return;

        if (typeof(TDto) == typeof(LookupCategoryResDTO))
        {
            var typedRows = rows.Cast<LookupCategoryResDTO>().ToList();
            var ids = typedRows.Select(row => row.Id).ToArray();
            var counts = await _unitOfWork.VPPContext.Set<LookupValue>()
                .AsNoTracking()
                .Where(value => value.LookupCategoryId.HasValue && ids.Contains(value.LookupCategoryId.Value) && !value.IsDeleted)
                .GroupBy(value => value.LookupCategoryId!.Value)
                .Select(group => new { Id = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.Id, row => row.Count, cancellationToken);
            foreach (var row in typedRows) row.ValueCount = counts.GetValueOrDefault(row.Id);
            return;
        }

        if (typeof(TDto) == typeof(VppCategoryResDTO))
        {
            var typedRows = rows.Cast<VppCategoryResDTO>().ToList();
            var ids = typedRows.Select(row => row.Id).ToArray();
            var counts = await _unitOfWork.VPPContext.Set<VppItem>()
                .AsNoTracking()
                .Where(item => ids.Contains(item.VppCategoryId) && !item.IsDeleted)
                .GroupBy(item => item.VppCategoryId)
                .Select(group => new { Id = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.Id, row => row.Count, cancellationToken);
            foreach (var row in typedRows) row.ItemCount = counts.GetValueOrDefault(row.Id);
            return;
        }

        if (typeof(TDto) == typeof(SupplierResDTO))
        {
            var typedRows = rows.Cast<SupplierResDTO>().ToList();
            var ids = typedRows.Select(row => row.Id).ToArray();
            var itemCounts = await _unitOfWork.VPPContext.Set<SupplierProductMapping>()
                .AsNoTracking()
                .Where(mapping => ids.Contains(mapping.SupplierId)
                    && !mapping.IsDeleted
                    && mapping.VppItem != null
                    && !mapping.VppItem.IsDeleted)
                .GroupBy(mapping => mapping.SupplierId)
                .Select(group => new { Id = group.Key, Count = group.Select(mapping => mapping.VppItemId).Distinct().Count() })
                .ToDictionaryAsync(row => row.Id, row => row.Count, cancellationToken);
            var priceListCounts = await _unitOfWork.VPPContext.Set<PriceList>()
                .AsNoTracking()
                .Where(priceList => priceList.SupplierId.HasValue
                    && ids.Contains(priceList.SupplierId.Value)
                    && !priceList.IsDeleted)
                .GroupBy(priceList => priceList.SupplierId!.Value)
                .Select(group => new { Id = group.Key, Count = group.Count() })
                .ToDictionaryAsync(row => row.Id, row => row.Count, cancellationToken);
            foreach (var row in typedRows)
            {
                row.ItemCount = itemCounts.GetValueOrDefault(row.Id);
                row.PriceListCount = priceListCounts.GetValueOrDefault(row.Id);
            }
            return;
        }

        if (typeof(TDto) == typeof(DepartmentResDTO))
        {
            var typedRows = rows.Cast<DepartmentResDTO>().ToList();
            var ids = typedRows.Select(row => row.Id).ToArray();
            var counts = await (
                from membership in _unitOfWork.VPPContext.Set<UserGroupMembership>().AsNoTracking()
                join user in _unitOfWork.VPPContext.Users.AsNoTracking()
                    on membership.AccountId equals (int?)user.Id
                where ids.Contains(membership.DepartmentId)
                      && !membership.IsDeleted
                      && user.AccountStatus == AppAccountStatus.Active
                group membership by membership.DepartmentId
                into grouped
                select new { Id = grouped.Key, Count = grouped.Select(membership => membership.AccountId).Distinct().Count() })
                .ToDictionaryAsync(row => row.Id, row => row.Count, cancellationToken);
            foreach (var row in typedRows) row.UserCount = counts.GetValueOrDefault(row.Id);
        }
    }

    private IQueryable<TModel> ApplyLibrarySearch<TModel>(IQueryable<TModel> query, string? searchText)
        where TModel : class
    {
        if (string.IsNullOrWhiteSpace(searchText)) return query;
        var term = VietnameseSearch.PrepareTerm(searchText);
        var isSqlServer = _unitOfWork.VPPContext.Database.IsSqlServer();
        var pattern = VietnameseSearch.BuildContainsPattern(term);
        var searchUpper = term.ToUpperInvariant();
        if (typeof(TModel) == typeof(LookupCategory))
        {
            var typedQuery = (IQueryable<LookupCategory>)query;
            return (IQueryable<TModel>)(isSqlServer
                ? typedQuery.Where(row =>
                    (row.Code != null && EF.Functions.Like(EF.Functions.Collate(row.Code.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Name != null && EF.Functions.Like(EF.Functions.Collate(row.Name.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Description != null && EF.Functions.Like(EF.Functions.Collate(row.Description.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")))
                : typedQuery.Where(row =>
                    (row.Code != null && row.Code.ToUpper().Contains(searchUpper))
                    || (row.Name != null && row.Name.ToUpper().Contains(searchUpper))
                    || (row.Description != null && row.Description.ToUpper().Contains(searchUpper))));
        }
        if (typeof(TModel) == typeof(LookupValue))
        {
            var typedQuery = (IQueryable<LookupValue>)query;
            return (IQueryable<TModel>)(isSqlServer
                ? typedQuery.Where(row =>
                    EF.Functions.Like(EF.Functions.Collate(row.Code.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")
                    || EF.Functions.Like(EF.Functions.Collate(row.Value.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")
                    || (row.Description != null && EF.Functions.Like(EF.Functions.Collate(row.Description.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")))
                : typedQuery.Where(row =>
                    row.Code.ToUpper().Contains(searchUpper)
                    || row.Value.ToUpper().Contains(searchUpper)
                    || (row.Description != null && row.Description.ToUpper().Contains(searchUpper))));
        }
        if (typeof(TModel) == typeof(VppCategory))
        {
            var typedQuery = (IQueryable<VppCategory>)query;
            return (IQueryable<TModel>)(isSqlServer
                ? typedQuery.Where(row =>
                    (row.VppCategoryCode != null && EF.Functions.Like(EF.Functions.Collate(row.VppCategoryCode.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.VppCategoryName != null && EF.Functions.Like(EF.Functions.Collate(row.VppCategoryName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")))
                : typedQuery.Where(row =>
                    (row.VppCategoryCode != null && row.VppCategoryCode.ToUpper().Contains(searchUpper))
                    || (row.VppCategoryName != null && row.VppCategoryName.ToUpper().Contains(searchUpper))));
        }
        if (typeof(TModel) == typeof(Supplier))
        {
            var typedQuery = (IQueryable<Supplier>)query;
            return (IQueryable<TModel>)(isSqlServer
                ? typedQuery.Where(row =>
                    (row.SupplierShortName != null && EF.Functions.Like(EF.Functions.Collate(row.SupplierShortName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.SupplierName != null && EF.Functions.Like(EF.Functions.Collate(row.SupplierName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Address1 != null && EF.Functions.Like(EF.Functions.Collate(row.Address1.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Address2 != null && EF.Functions.Like(EF.Functions.Collate(row.Address2.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Address3 != null && EF.Functions.Like(EF.Functions.Collate(row.Address3.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Ward != null && EF.Functions.Like(EF.Functions.Collate(row.Ward.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.City != null && EF.Functions.Like(EF.Functions.Collate(row.City.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")))
                : typedQuery.Where(row =>
                    (row.SupplierShortName != null && row.SupplierShortName.ToUpper().Contains(searchUpper))
                    || (row.SupplierName != null && row.SupplierName.ToUpper().Contains(searchUpper))
                    || (row.Address1 != null && row.Address1.ToUpper().Contains(searchUpper))
                    || (row.Address2 != null && row.Address2.ToUpper().Contains(searchUpper))
                    || (row.Address3 != null && row.Address3.ToUpper().Contains(searchUpper))
                    || (row.Ward != null && row.Ward.ToUpper().Contains(searchUpper))
                    || (row.City != null && row.City.ToUpper().Contains(searchUpper))));
        }
        if (typeof(TModel) == typeof(Department))
        {
            var typedQuery = (IQueryable<Department>)query;
            return (IQueryable<TModel>)(isSqlServer
                ? typedQuery.Where(row =>
                    (row.Code != null && EF.Functions.Like(EF.Functions.Collate(row.Code.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                    || (row.Name != null && EF.Functions.Like(EF.Functions.Collate(row.Name.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")))
                : typedQuery.Where(row =>
                    (row.Code != null && row.Code.ToUpper().Contains(searchUpper))
                    || (row.Name != null && row.Name.ToUpper().Contains(searchUpper))));
        }
        return query;
    }

    private IQueryable<VppItem> ApplyVppItemSearch(IQueryable<VppItem> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        var term = VietnameseSearch.PrepareTerm(searchText);
        if (_unitOfWork.VPPContext.Database.IsSqlServer())
        {
            var pattern = VietnameseSearch.BuildContainsPattern(term);
            return query.Where(item =>
                (item.VppCode != null && EF.Functions.Like(EF.Functions.Collate(item.VppCode.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\"))
                || (item.VppName != null && EF.Functions.Like(EF.Functions.Collate(item.VppName.Replace("đ", "d").Replace("Đ", "D"), VietnameseSearch.SqlServerCollation), pattern, "\\")));
        }

        var searchUpper = term.ToUpperInvariant();
        return query.Where(item =>
            (item.VppCode != null && item.VppCode.ToUpper().Contains(searchUpper))
            || (item.VppName != null && item.VppName.ToUpper().Contains(searchUpper)));
    }

    private static VppItemResDTO ToVppItemDto(
        VppItem item,
        decimal? defaultPrice,
        string? defaultSupplierName,
        int supplierCount)
        => new()
        {
            Id = item.Id,
            Description = item.Description,
            CreatedByUserId = item.CreatedByUserId,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedByUserId = item.UpdatedByUserId,
            UpdatedAtUtc = item.UpdatedAtUtc,
            IsDeleted = item.IsDeleted,
            VppCode = item.VppCode,
            VppName = item.VppName,
            MaxQuantityPerOrder = item.MaxQuantityPerOrder,
            UomId = item.UomId,
            VppCategoryId = item.VppCategoryId,
            DefaultVatRate = VppPricingDefaults.VatRate,
            DefaultPrice = defaultPrice,
            DefaultSupplierName = defaultSupplierName,
            SupplierCount = supplierCount,
            Uom = item.Uom?.Adapt<LookupValueResDTO>(),
            VppCategory = item.VppCategory?.Adapt<VppCategoryResDTO>()
        };
}
