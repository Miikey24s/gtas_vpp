using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.Constants;
using gtas_vpp_shared.DTOs.Res.Library;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json;
using System.Linq.Dynamic.Core;
using System.Globalization;
using System.Security.Claims;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LibraryController : BaseGenericController
    {
        private readonly IDateTimeProvider _dateTimeProvider;

        public LibraryController(
            IServiceProvider serviceProvider,
            IUserNameResolver userNameResolver,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
            : base(serviceProvider, userNameResolver, unitOfWork)
        {
            // P5/timezone: use shared provider so timestamps stay in Asia/Ho_Chi_Minh
            // even when the host runs in a different timezone (e.g. SGP cloud, UTC).
            _dateTimeProvider = dateTimeProvider;
        }

        [HttpGet("{tableCode}")]
        [Authorize(Policy = Permissions.LibraryView)]
        public async Task<IActionResult> GenericGet(
            string tableCode, 
            [FromQuery] Guid? id, 
            [FromQuery] string? searchText, 
            [FromQuery] Guid? lookupCategoryId,
            [FromQuery] string? filter,
            [FromQuery] int? skip,
            [FromQuery] int? top,
            [FromQuery] string? orderby,
            [FromQuery] string? distinct,
            [FromQuery] string? distinctFilter,
            [FromQuery] bool? showDeleted = false)
        {
            string cleanSearch = searchText?.Trim() ?? string.Empty;
            bool isShowDeleted = showDeleted ?? false;

            // Check if this is a LoadData request (has any of the advanced parameters)
            bool isLoadDataRequest = !string.IsNullOrEmpty(filter) || skip.HasValue || top.HasValue || 
                                     !string.IsNullOrEmpty(orderby) || !string.IsNullOrEmpty(distinct) ||
                                     !string.IsNullOrEmpty(distinctFilter);

            // Lookup values can be filtered by their parent category.
            if (tableCode.Equals("lookup-values", StringComparison.OrdinalIgnoreCase)
                && lookupCategoryId.HasValue
                && !id.HasValue
                && string.IsNullOrEmpty(cleanSearch))
            {
                isLoadDataRequest = true;
            }

            // If using advanced filtering
            if (isLoadDataRequest)
            {
                return tableCode.ToLower() switch
                {
                    "lookup-categories" => await GetTableDataWithFilteringAsync<LookupCategory, LookupCategoryResDTO>(filter, skip, top, orderby, distinct, distinctFilter, null, isShowDeleted),
                    "lookup-values" => await GetTableDataWithFilteringAsync<LookupValue, LookupValueResDTO>(filter, skip, top, orderby, distinct, distinctFilter, lookupCategoryId, isShowDeleted),
                    "vpp-categories" => await GetTableDataWithFilteringAsync<VppCategory, VppCategoryResDTO>(filter, skip, top, orderby, distinct, distinctFilter, null, isShowDeleted),
                    "vpp-items" => await GetVppItemsWithFilteringAsync(filter, skip, top, orderby, distinct, distinctFilter, isShowDeleted),
                    "suppliers" => await GetTableDataWithFilteringAsync<Supplier, SupplierResDTO>(filter, skip, top, orderby, distinct, distinctFilter, null, isShowDeleted),
                    "supplier-product-mappings" => await GetTableDataWithFilteringAsync<SupplierProductMapping, SupplierProductMappingResDTO>(filter, skip, top, orderby, distinct, distinctFilter, null, isShowDeleted),
                    "departments" => await GetTableDataWithFilteringAsync<Department, DepartmentResDTO>(filter, skip, top, orderby, distinct, distinctFilter, null, isShowDeleted),
                    _ => BadRequest(new { Message = $"Advanced filtering for Table Code '{tableCode}' is not supported." })
                };
            }

            // Original simple filtering
            return tableCode.ToLower() switch
            {
                "lookup-categories" => await GetTableDataAsync<LookupCategory, LookupCategoryResDTO>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => (x.Name != null && x.Name.Contains(cleanSearch))
                                   || (x.Code != null && x.Code.Contains(cleanSearch))
                                   || (x.Description != null && x.Description.Contains(cleanSearch)),
                    showDeleted: isShowDeleted),
                "lookup-values" => await GetTableDataAsync<LookupValue, LookupValueResDTO>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => (x.Code != null && x.Code.Contains(cleanSearch))
                                   || (x.Value != null && x.Value.Contains(cleanSearch))
                                   || (x.Description != null && x.Description.Contains(cleanSearch)),
                    showDeleted: isShowDeleted),
                "vpp-categories" => await GetTableDataAsync<VppCategory, VppCategoryResDTO>(id, cleanSearch, matchId: x => x.Id == id, showDeleted: isShowDeleted),
                "vpp-items" => await GetVppItemsAsync(id, cleanSearch, isShowDeleted),
                "suppliers" => await GetTableDataAsync<Supplier, SupplierResDTO>(id, cleanSearch, matchId: x => x.Id == id, showDeleted: isShowDeleted),
                "supplier-product-mappings" => await GetTableDataAsync<SupplierProductMapping, SupplierProductMappingResDTO>(id, cleanSearch, matchId: x => x.Id == id, showDeleted: isShowDeleted),
                "departments" => await GetTableDataAsync<Department, DepartmentResDTO>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => (x.Code != null && x.Code.Contains(cleanSearch))
                                   || (x.Name != null && x.Name.Contains(cleanSearch)),
                    showDeleted: isShowDeleted),
                _ => BadRequest(new { Message = $"Table Code '{tableCode}' is not supported." })
            };
        }

        private async Task<Guid> GetDefaultPriceListIdAsync()
        {
            return await _unitOfWork.VPPContext.Set<gtas_vpp_be.Model.Library.PriceList>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsDefault)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<IActionResult> GetVppItemsAsync(Guid? id, string cleanSearch, bool showDeleted = false)
        {
            IQueryable<VppItem> vppQuery = _unitOfWork.VPPContext.Set<VppItem>()
                .AsNoTracking()
                .Include(x => x.Uom)
                .Include(x => x.VppCategory);

            if (!showDeleted)
            {
                vppQuery = vppQuery.Where(x => !x.IsDeleted);
            }

            if (id.HasValue)
            {
                vppQuery = vppQuery.Where(x => x.Id == id.Value);
            }

            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                vppQuery = vppQuery.Where(x => (x.VppName != null && x.VppName.Contains(cleanSearch))
                                            || (x.VppCode != null && x.VppCode.Contains(cleanSearch)));
            }

            var totalCount = await vppQuery.CountAsync();
            var vppList = await vppQuery
                .OrderBy(x => x.VppCode)
                .Take(5000)
                .ToListAsync();

            if (!vppList.Any())
            {
                Response.Headers["X-Total-Count"] = "0";
                return Ok(new List<VppItemResDTO>());
            }

            var defaultPriceListId = await GetDefaultPriceListIdAsync();
            var vppIds = vppList.Select(v => v.Id).ToList();

            var mappings = await _unitOfWork.VPPContext.Set<SupplierProductMapping>()
                .AsNoTracking()
                .Where(m => (showDeleted || !m.IsDeleted) 
                    && m.PriceListId == defaultPriceListId
                    && vppIds.Contains(m.VppItemId)
                    && (m.Supplier == null || showDeleted || !m.Supplier.IsDeleted))
                .Select(m => new {
                    m.VppItemId,
                    m.Price,
                    m.IsDefault,
                    SupplierShortName = m.Supplier != null ? m.Supplier.SupplierShortName : null,
                    SupplierName = m.Supplier != null ? m.Supplier.SupplierName : null
                })
                .ToListAsync();

            var mappingLookup = mappings
                .GroupBy(m => m.VppItemId)
                .ToDictionary(
                    g => g.Key,
                    g => {
                        var bestMapping = g
                            .OrderByDescending(m => m.IsDefault)
                            .ThenBy(m => m.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                            .ThenBy(m => m.SupplierName)
                            .FirstOrDefault();
                        return new {
                            Price = bestMapping?.Price,
                            SupplierName = bestMapping?.SupplierName
                        };
                    }
                );

            var dtoList = vppList.Select(x => {
                mappingLookup.TryGetValue(x.Id, out var priceInfo);
                return new VppItemResDTO
                {
                    Id = x.Id,
                    Description = x.Description,
                    CreatedByUserId = x.CreatedByUserId,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedByUserId = x.UpdatedByUserId,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    VppCode = x.VppCode,
                    VppName = x.VppName,
                    UomId = x.UomId,
                    VppCategoryId = x.VppCategoryId,
                    DefaultVatRate = VppPricingDefaults.VatRate,
                    DefaultPrice = priceInfo?.Price,
                    DefaultSupplierName = priceInfo?.SupplierName,
                    Uom = x.Uom == null ? null : new LookupValueResDTO
                    {
                        Id = x.Uom.Id,
                        Description = x.Uom.Description,
                        CreatedByUserId = x.Uom.CreatedByUserId,
                        CreatedAtUtc = x.Uom.CreatedAtUtc,
                        UpdatedByUserId = x.Uom.UpdatedByUserId,
                        UpdatedAtUtc = x.Uom.UpdatedAtUtc,
                        IsDeleted = x.Uom.IsDeleted,
                        LookupCategoryId = x.Uom.LookupCategoryId,
                        Code = x.Uom.Code,
                        Value = x.Uom.Value,
                        ExtraField1 = x.Uom.ExtraField1,
                        ExtraField2 = x.Uom.ExtraField2,
                        ExtraField3 = x.Uom.ExtraField3,
                        Sort = x.Uom.Sort
                    },
                    VppCategory = x.VppCategory == null ? null : new VppCategoryResDTO
                    {
                        Id = x.VppCategory.Id,
                        Description = x.VppCategory.Description,
                        CreatedByUserId = x.VppCategory.CreatedByUserId,
                        CreatedAtUtc = x.VppCategory.CreatedAtUtc,
                        UpdatedByUserId = x.VppCategory.UpdatedByUserId,
                        UpdatedAtUtc = x.VppCategory.UpdatedAtUtc,
                        IsDeleted = x.VppCategory.IsDeleted,
                        VppCategoryCode = x.VppCategory.VppCategoryCode,
                        VppCategoryName = x.VppCategory.VppCategoryName
                    }
                };
            }).ToList();

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            return Ok(dtoList);
        }

        private async Task<IActionResult> GetVppItemsWithFilteringAsync(
            string? filter,
            int? skip,
            int? top,
            string? orderby,
            string? distinct,
            string? distinctFilter,
            bool showDeleted = false)
        {
            try
            {
                var defaultPriceListId = await GetDefaultPriceListIdAsync();
                var baseQuery = _unitOfWork.VPPContext.Set<VppItem>().AsNoTracking();

                if (!showDeleted)
                {
                    baseQuery = baseQuery.Where(x => !x.IsDeleted);
                }

                var query = baseQuery.Select(x => new VppItemResDTO
                {
                    Id = x.Id,
                    Description = x.Description,
                    CreatedByUserId = x.CreatedByUserId,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedByUserId = x.UpdatedByUserId,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                    IsDeleted = x.IsDeleted,
                    VppCode = x.VppCode,
                    VppName = x.VppName,
                    UomId = x.UomId,
                    VppCategoryId = x.VppCategoryId,
                    DefaultVatRate = VppPricingDefaults.VatRate,
                    DefaultPrice = x.SupplierProductMappings!
                        .Where(m => (showDeleted || !m.IsDeleted)
                            && m.PriceListId == defaultPriceListId
                            && (m.Supplier == null || showDeleted || !m.Supplier.IsDeleted))
                        .OrderByDescending(m => m.IsDefault)
                        .ThenBy(m => m.Supplier != null && m.Supplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                        .ThenBy(m => m.Supplier != null ? m.Supplier.SupplierName : null)
                        .Select(m => (decimal?)m.Price)
                        .FirstOrDefault(),
                    DefaultSupplierName = x.SupplierProductMappings!
                        .Where(m => (showDeleted || !m.IsDeleted)
                            && m.PriceListId == defaultPriceListId
                            && (m.Supplier == null || showDeleted || !m.Supplier.IsDeleted))
                        .OrderByDescending(m => m.IsDefault)
                        .ThenBy(m => m.Supplier != null && m.Supplier.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                        .ThenBy(m => m.Supplier != null ? m.Supplier.SupplierName : null)
                        .Select(m => m.Supplier != null ? m.Supplier.SupplierName : null)
                        .FirstOrDefault(),
                    Uom = x.Uom == null ? null : new LookupValueResDTO
                    {
                        Id = x.Uom.Id,
                        Description = x.Uom.Description,
                        CreatedByUserId = x.Uom.CreatedByUserId,
                        CreatedAtUtc = x.Uom.CreatedAtUtc,
                        UpdatedByUserId = x.Uom.UpdatedByUserId,
                        UpdatedAtUtc = x.Uom.UpdatedAtUtc,
                        IsDeleted = x.Uom.IsDeleted,
                        LookupCategoryId = x.Uom.LookupCategoryId,
                        Code = x.Uom.Code,
                        Value = x.Uom.Value,
                        ExtraField1 = x.Uom.ExtraField1,
                        ExtraField2 = x.Uom.ExtraField2,
                        ExtraField3 = x.Uom.ExtraField3,
                        Sort = x.Uom.Sort
                    },
                    VppCategory = x.VppCategory == null ? null : new VppCategoryResDTO
                    {
                        Id = x.VppCategory.Id,
                        Description = x.VppCategory.Description,
                        CreatedByUserId = x.VppCategory.CreatedByUserId,
                        CreatedAtUtc = x.VppCategory.CreatedAtUtc,
                        UpdatedByUserId = x.VppCategory.UpdatedByUserId,
                        UpdatedAtUtc = x.VppCategory.UpdatedAtUtc,
                        IsDeleted = x.VppCategory.IsDeleted,
                        VppCategoryCode = x.VppCategory.VppCategoryCode,
                        VppCategoryName = x.VppCategory.VppCategoryName
                    }
                });

                if (!string.IsNullOrWhiteSpace(filter))
                {
                    try
                    {
                        query = query.Where(filter);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "VPP item filter parse failed");
                    }
                }

                if (!string.IsNullOrWhiteSpace(distinct))
                {
                    var propertyInfo = typeof(VppItemResDTO).GetProperty(distinct);
                    if (propertyInfo != null)
                    {
                        var distinctValues = await query
                            .Select(distinct)
                            .Distinct()
                            .ToDynamicListAsync();

                        var filteredValues = distinctValues
                            .Where(val => val != null)
                            .Where(val => string.IsNullOrWhiteSpace(distinctFilter)
                                || (Convert.ToString(val, CultureInfo.CurrentCulture)?.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToList();

                        Response.Headers.Append("X-Total-Count", filteredValues.Count.ToString());

                        IEnumerable<object> pageValues = filteredValues.Cast<object>();
                        if (skip.HasValue && skip.Value > 0)
                        {
                            pageValues = pageValues.Skip(skip.Value);
                        }

                        if (top.HasValue && top.Value > 0)
                        {
                            pageValues = pageValues.Take(top.Value);
                        }

                        var distinctDtos = pageValues.Select(val =>
                        {
                            var dto = new VppItemResDTO();
                            propertyInfo.SetValue(dto, val);
                            return dto;
                        }).ToList();

                        return Ok(distinctDtos);
                    }
                }

                var totalCount = await query.CountAsync();

                if (!string.IsNullOrWhiteSpace(orderby))
                {
                    try
                    {
                        query = query.OrderBy(orderby);
                    }
                    catch
                    {
                        query = query.OrderBy(x => x.VppCode);
                    }
                }
                else
                {
                    query = query.OrderBy(x => x.VppCode);
                }

                if (skip.HasValue && skip.Value > 0)
                {
                    query = query.Skip(skip.Value);
                }

                if (top.HasValue && top.Value > 0)
                {
                    query = query.Take(top.Value);
                }

                var dtoList = await query.ToListAsync();
                Response.Headers.Append("X-Total-Count", totalCount.ToString());
                return Ok(await _userNameResolver.WithUserNamesAsync(dtoList, _unitOfWork.VPPContext));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "VPP item query failed");
                return BadRequest(new { Message = "An error occurred while processing VPP item data." });
            }
        }

        private async Task<IActionResult> GetVppItemByIdAsync(Guid id, bool showDeleted = false)
        {
            var vpp = await _unitOfWork.VPPContext.Set<VppItem>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && (showDeleted || !x.IsDeleted));

            if (vpp == null)
            {
                return NotFound(new { Message = $"Record with ID {id} not found." });
            }

            var defaultPriceListId = await GetDefaultPriceListIdAsync();

            var mappings = await _unitOfWork.VPPContext.Set<SupplierProductMapping>()
                .AsNoTracking()
                .Where(m => (showDeleted || !m.IsDeleted) 
                    && m.PriceListId == defaultPriceListId
                    && m.VppItemId == id
                    && (m.Supplier == null || showDeleted || !m.Supplier.IsDeleted))
                .Select(m => new {
                    m.Price,
                    m.IsDefault,
                    SupplierShortName = m.Supplier != null ? m.Supplier.SupplierShortName : null,
                    SupplierName = m.Supplier != null ? m.Supplier.SupplierName : null
                })
                .ToListAsync();

            var bestMapping = mappings
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                .ThenBy(m => m.SupplierName)
                .FirstOrDefault();

            var dto = new VppItemResDTO
            {
                Id = vpp.Id,
                Description = vpp.Description,
                CreatedByUserId = vpp.CreatedByUserId,
                CreatedAtUtc = vpp.CreatedAtUtc,
                UpdatedByUserId = vpp.UpdatedByUserId,
                UpdatedAtUtc = vpp.UpdatedAtUtc,
                IsDeleted = vpp.IsDeleted,
                VppCode = vpp.VppCode,
                VppName = vpp.VppName,
                UomId = vpp.UomId,
                VppCategoryId = vpp.VppCategoryId,
                DefaultVatRate = VppPricingDefaults.VatRate,
                DefaultPrice = bestMapping?.Price,
                DefaultSupplierName = bestMapping?.SupplierName
            };

            return Ok(dto);
        }

        private async Task<IActionResult> GetTableDataWithFilteringAsync<TModel, TDto>(
            string? filter,
            int? skip,
            int? top,
            string? orderby,
            string? distinct,
            string? distinctFilter,
            Guid? lookupCategoryId = null,
            bool showDeleted = false) where TModel : class
        {
            // P3.2 (F-12): Build query directly on IQueryable<TModel> so filter/orderby/
            // count/skip/take all translate to SQL. Previously this method materialized
            // up to 1000 rows then filtered in-memory — broken pagination + heap pressure
            // under load (1000 user × 1000 row materialised per request).
            try
            {
                IQueryable<TModel> query = _unitOfWork.VPPContext.Set<TModel>().AsNoTracking();

                if (typeof(gtas_vpp_be.Model.Helpers.BaseModel).IsAssignableFrom(typeof(TModel)))
                {
                    if (!showDeleted)
                    {
                        query = query.Where("IsDeleted == false");
                    }
                }

                // Apply lookupCategoryId filter for lookup values (typed, not Dynamic LINQ)
                if (lookupCategoryId.HasValue && typeof(TModel) == typeof(LookupValue))
                {
                    query = (IQueryable<TModel>)((IQueryable<LookupValue>)query)
                        .Where(x => x.LookupCategoryId == lookupCategoryId.Value);
                }

                // Apply Radzen filter expression via Dynamic LINQ (translates to SQL when
                // the source is an EF IQueryable; falls back gracefully on parse errors).
                if (!string.IsNullOrEmpty(filter))
                {
                    try
                    {
                        query = query.Where(filter);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Warning(ex, "Library filter parse failed");
                    }
                }

                // ── Distinct branch: pull only the column the FE asked for. ───────────
                if (!string.IsNullOrEmpty(distinct))
                {
                    var propertyInfo = typeof(TModel).GetProperty(distinct);
                    if (propertyInfo != null)
                    {
                        // Dynamic LINQ: SELECT DISTINCT property values directly in SQL.
                        var distinctValues = await query
                            .Select(distinct)
                            .Distinct()
                            .ToDynamicListAsync();

                        var filteredValues = distinctValues
                            .Where(val => val != null)
                            .Where(val => string.IsNullOrWhiteSpace(distinctFilter)
                                || (Convert.ToString(val, CultureInfo.CurrentCulture)?.Contains(distinctFilter, StringComparison.OrdinalIgnoreCase) ?? false))
                            .ToList();

                        var totalDistinctCount = filteredValues.Count;

                        IEnumerable<object> pagedValues = filteredValues.Cast<object>();
                        if (skip.HasValue && skip.Value > 0)
                        {
                            pagedValues = pagedValues.Skip(skip.Value);
                        }

                        if (top.HasValue && top.Value > 0)
                        {
                            pagedValues = pagedValues.Take(top.Value);
                        }

                        var distinctDtos = pagedValues
                            .Select(val =>
                            {
                                var dto = Activator.CreateInstance<TDto>();
                                var dtoProp = typeof(TDto).GetProperty(distinct);
                                if (dtoProp != null)
                                {
                                    dtoProp.SetValue(dto, val);
                                }
                                return dto;
                            })
                            .ToList();

                        Response.Headers.Append("X-Total-Count", totalDistinctCount.ToString());
                        return Ok(distinctDtos);
                    }
                }

                // Total count BEFORE paging — translates to COUNT(*) in SQL.
                var totalCount = await query.CountAsync();

                // Apply sorting (default to deterministic order to satisfy SQL Server when
                // Skip/Take is used).
                if (!string.IsNullOrEmpty(orderby))
                {
                    try
                    {
                        query = query.OrderBy(orderby);
                    }
                    catch
                    {
                        // Fall back to default order below if Dynamic LINQ rejects the clause.
                        query = query.OrderBy("Id");
                    }
                }
                else
                {
                    query = query.OrderBy("Id");
                }

                if (skip.HasValue && skip.Value > 0)
                {
                    query = query.Skip(skip.Value);
                }

                if (top.HasValue && top.Value > 0)
                {
                    query = query.Take(top.Value);
                }

                // Materialize the (small) page only, then map.
                var pageEntities = await query.ToListAsync();
                var enriched = await _userNameResolver.WithUserNamesAsync(pageEntities, _unitOfWork.VPPContext);
                var dtoList = enriched.Adapt<List<TDto>>();

                Response.Headers.Append("X-Total-Count", totalCount.ToString());

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Library query failed");
                return BadRequest(new { Message = "An error occurred while processing the request." });
            }
        }

        [HttpGet("{tableCode}/{id:guid}")]
        [Authorize(Policy = Permissions.LibraryView)]
        public async Task<IActionResult> GenericGetById(string tableCode, Guid id, [FromQuery] bool? showDeleted = false)
        {
            bool isShowDeleted = showDeleted ?? false;
            return tableCode.ToLower() switch
            {
                "lookup-categories" => await GetByIdAsync<LookupCategory, LookupCategoryResDTO>(id),
                "lookup-values" => await GetByIdAsync<LookupValue, LookupValueResDTO>(id),
                "vpp-categories" => await GetByIdAsync<VppCategory, VppCategoryResDTO>(id),
                "vpp-items" => await GetVppItemByIdAsync(id, isShowDeleted),
                "suppliers" => await GetByIdAsync<Supplier, SupplierResDTO>(id),
                "supplier-product-mappings" => await GetByIdAsync<SupplierProductMapping, SupplierProductMappingResDTO>(id),
                "departments" => await GetByIdAsync<Department, DepartmentResDTO>(id),
                _ => BadRequest(new { Message = $"GetById for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPost("{tableCode}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> GenericCreate(string tableCode, [FromBody] JsonElement payload)
        {
            if (IsMigratedVppItemTable(tableCode))
            {
                return MigratedVppItemMutationProblem();
            }

            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "lookup-categories" => await CreateAsync<LookupCategory, LookupCategoryResDTO>(json),
                "lookup-values" => await CreateAsync<LookupValue, LookupValueResDTO>(json),
                "vpp-categories" => await CreateAsync<VppCategory, VppCategoryResDTO>(json),
                "vpp-items" => await CreateAsync<VppItem, VppItemResDTO>(json),
                "suppliers" => await CreateAsync<Supplier, SupplierResDTO>(json),
                "supplier-product-mappings" => await CreateAsync<SupplierProductMapping, SupplierProductMappingResDTO>(json),
                "departments" => await CreateAsync<Department, DepartmentResDTO>(json),
                _ => BadRequest(new { Message = $"Create for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPut("{tableCode}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
        {
            if (IsMigratedVppItemTable(tableCode))
            {
                return MigratedVppItemMutationProblem();
            }

            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "lookup-categories" => await UpdateAsync<LookupCategory, LookupCategoryResDTO>(json),
                "lookup-values" => await UpdateAsync<LookupValue, LookupValueResDTO>(json),
                "vpp-categories" => await UpdateAsync<VppCategory, VppCategoryResDTO>(json),
                "vpp-items" => await UpdateAsync<VppItem, VppItemResDTO>(json),
                "suppliers" => await UpdateAsync<Supplier, SupplierResDTO>(json),
                "supplier-product-mappings" => await UpdateAsync<SupplierProductMapping, SupplierProductMappingResDTO>(json),
                "departments" => await UpdateAsync<Department, DepartmentResDTO>(json),
                _ => BadRequest(new { Message = $"Update for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPatch("{tableCode}/{id:guid}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> GenericPatch(string tableCode, Guid id, [FromBody] JsonElement payload)
        {
            if (IsMigratedVppItemTable(tableCode))
            {
                return MigratedVppItemMutationProblem();
            }

            if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            {
                return BadRequest(new { Message = "Update payload must not be empty." });
            }

            return tableCode.ToLower() switch
            {
                "lookup-categories" => await ApplyPatchAsync<LookupCategory, LookupCategoryResDTO>(id, payload),
                "lookup-values" => await ApplyPatchAsync<LookupValue, LookupValueResDTO>(id, payload),
                "vpp-categories" => await ApplyPatchAsync<VppCategory, VppCategoryResDTO>(id, payload),
                "vpp-items" => await ApplyPatchAsync<VppItem, VppItemResDTO>(id, payload),
                "suppliers" => await ApplyPatchAsync<Supplier, SupplierResDTO>(id, payload),
                "supplier-product-mappings" => await ApplyPatchAsync<SupplierProductMapping, SupplierProductMappingResDTO>(id, payload),
                "departments" => await ApplyPatchAsync<Department, DepartmentResDTO>(id, payload),
                _ => BadRequest(new { Message = $"Patch for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpDelete("{tableCode}/{id:guid}")]
        [Authorize(Policy = Permissions.LibraryManage)]
        public async Task<IActionResult> GenericDelete(string tableCode, Guid id)
        {
            if (IsMigratedVppItemTable(tableCode))
            {
                return MigratedVppItemMutationProblem();
            }

            return tableCode.ToLower() switch
            {
                "lookup-categories" => await DeleteAsync<LookupCategory>(id),
                "lookup-values" => await DeleteAsync<LookupValue>(id),
                "vpp-categories" => await DeleteAsync<VppCategory>(id),
                "vpp-items" => await DeleteAsync<VppItem>(id),
                "suppliers" => await DeleteAsync<Supplier>(id),
                "supplier-product-mappings" => await DeleteAsync<SupplierProductMapping>(id),
                "departments" => await DeleteAsync<Department>(id),
                _ => BadRequest(new { Message = $"Delete for Table Code '{tableCode}' is not supported." })
            };
        }

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private static bool IsMigratedVppItemTable(string tableCode)
            => string.Equals(tableCode, "vpp-items", StringComparison.OrdinalIgnoreCase);

        private IActionResult MigratedVppItemMutationProblem()
            => Problem(
                title: "Legacy catalog mutation disabled",
                detail: "VPP items must be changed through /api/catalog/items; hard delete is not supported.",
                statusCode: StatusCodes.Status405MethodNotAllowed,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = "CatalogTypedEndpointRequired",
                    ["safeDetail"] = true
                });

        // Audit fields the client must never write. IsDeleted is intentionally NOT here:
        // Library admin pages (Tab_ClassLibrary, etc.) toggle Enable/Disable via PATCH with
        // `IsDeleted = true|false`. Access to these admin actions is gated FE-side via
        // PermissionState.GetPagePermission(PageLibrary) component flags. Adding IsDeleted
        // back to this list would silently break the toggle (PATCH returns 200 with the
        // unchanged entity → misleading "Success" toast in FE).
        private static readonly HashSet<string> _writeDeniedFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "CreatedByUserId",
            "CreatedAtUtc",
            "UpdatedByUserId",
            "UpdatedAtUtc"
        };

        private async Task<IActionResult> CreateAsync<TModel, TDto>(string json) where TModel : gtas_vpp_be.Model.Helpers.BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            obj.Id = Guid.Empty;
            var now = _dateTimeProvider.Now;
            var uid = int.TryParse(User.FindFirstValue("UserID"), out var x) ? x : 0;
            obj.CreatedAtUtc = now;
            obj.UpdatedAtUtc = now;
            obj.CreatedByUserId = uid;
            obj.UpdatedByUserId = uid;
            obj.IsDeleted = false;
            
            var created = await GetRepository<TModel>().AddAsync(obj);
            var resultDto = created?.Adapt<TDto>();
            return Ok(resultDto);
        }

        private async Task<IActionResult> UpdateAsync<TModel, TDto>(string json) where TModel : gtas_vpp_be.Model.Helpers.BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            var existing = await GetEntityByIdAsync<TModel>(obj.Id, false);
            if (existing == null)
                return NotFound(new { Message = $"Record with ID {obj.Id} not found." });

            var uid = int.TryParse(User.FindFirstValue("UserID"), out var x) ? x : 0;
            obj.CreatedByUserId = existing.CreatedByUserId;
            obj.CreatedAtUtc = existing.CreatedAtUtc;
            obj.IsDeleted = existing.IsDeleted;
            obj.UpdatedAtUtc = _dateTimeProvider.Now;
            obj.UpdatedByUserId = uid;

            _unitOfWork.VPPContext.Entry(existing).State = EntityState.Detached;
            
            var updated = await GetRepository<TModel>().UpdateAsync(obj);
            var resultDto = updated.Adapt<TDto>();
            return Ok(resultDto);
        }

        private async Task<IActionResult> ApplyPatchAsync<TModel, TDto>(Guid id, JsonElement payload) where TModel : class where TDto : class
        {
            var entity = await GetEntityByIdAsync<TModel>(id, true);

            if (entity == null)
                return NotFound(new { Message = $"Record with ID {id} not found." });

            var type = typeof(TModel);
            foreach (var jsonProperty in payload.EnumerateObject())
            {
                if (_writeDeniedFields.Contains(jsonProperty.Name)) continue;

                var prop = type.GetProperty(jsonProperty.Name, System.Reflection.BindingFlags.IgnoreCase
                                                             | System.Reflection.BindingFlags.Public
                                                             | System.Reflection.BindingFlags.Instance);

                if (prop != null && prop.CanWrite)
                {
                    var value = JsonSerializer.Deserialize(jsonProperty.Value.GetRawText(), prop.PropertyType);
                    prop.SetValue(entity, value);
                }
            }

            var updateDateProp = type.GetProperty("UpdatedAtUtc");
            if (updateDateProp != null && updateDateProp.CanWrite)
            {
                updateDateProp.SetValue(entity, _dateTimeProvider.Now);
            }

            var updateUserIdProp = type.GetProperty("UpdatedByUserId");
            if (updateUserIdProp != null && updateUserIdProp.CanWrite)
            {
                var uid = int.TryParse(User.FindFirstValue("UserID"), out var x) ? x : 0;
                updateUserIdProp.SetValue(entity, uid);
            }

            var result = await GetRepository<TModel>().UpdateAsync(entity);
            var resultDto = result.Adapt<TDto>();
            return Ok(resultDto);
        }
    }
}
