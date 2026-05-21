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
        public async Task<IActionResult> GenericGet(
            string tableCode, 
            [FromQuery] Guid? id, 
            [FromQuery] string? searchText, 
            [FromQuery] Guid? classId,
            [FromQuery] string? filter,
            [FromQuery] int? skip,
            [FromQuery] int? top,
            [FromQuery] string? orderby,
            [FromQuery] string? distinct,
            [FromQuery] string? distinctFilter)
        {
            string cleanSearch = searchText?.Trim() ?? string.Empty;

            // Check if this is a LoadData request (has any of the advanced parameters)
            bool isLoadDataRequest = !string.IsNullOrEmpty(filter) || skip.HasValue || top.HasValue || 
                                     !string.IsNullOrEmpty(orderby) || !string.IsNullOrEmpty(distinct) ||
                                     !string.IsNullOrEmpty(distinctFilter);

            // For L02, if classId is provided without id or searchText, treat as LoadData request
            if (tableCode.ToLower() == "l02" && classId.HasValue && !id.HasValue && string.IsNullOrEmpty(cleanSearch))
            {
                isLoadDataRequest = true;
            }

            // If using advanced filtering
            if (isLoadDataRequest)
            {
                return tableCode.ToLower() switch
                {
                    "l01" => await GetTableDataWithFilteringAsync<L01_Class, L01_ClassResDTO>(filter, skip, top, orderby, distinct, distinctFilter),
                    "l02" => await GetTableDataWithFilteringAsync<L02_ClassDetail, L02_ClassDetailResDTO>(filter, skip, top, orderby, distinct, distinctFilter, classId),
                    "lex02" => await GetTableDataWithFilteringAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(filter, skip, top, orderby, distinct, distinctFilter),
                    _ => BadRequest(new { Message = $"Advanced filtering for Table Code '{tableCode}' is not supported." })
                };
            }

            // Original simple filtering
            return tableCode.ToLower() switch
            {
                "l01" => await GetTableDataAsync<L01_Class, L01_ClassResDTO>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => (x.ClassName != null && x.ClassName.Contains(cleanSearch))
                                   || (x.ClassCode != null && x.ClassCode.Contains(cleanSearch))
                                   || (x.Description != null && x.Description.Contains(cleanSearch))),
                "l02" => await GetTableDataAsync<L02_ClassDetail, L02_ClassDetailResDTO>(id, cleanSearch, 
                    matchId: x => x.Id == id,
                    matchSearch: x => (x.ClassDetailCode != null && x.ClassDetailCode.Contains(cleanSearch))
                                   || (x.ClassDetailValue != null && x.ClassDetailValue.Contains(cleanSearch))
                                   || (x.Description != null && x.Description.Contains(cleanSearch))),
                "l03" => await GetTableDataAsync<L03_VPPCategory, L03_VPPCategoryResDTO>(id, cleanSearch, matchId: x => x.Id == id),
                "l04" => await GetVppItemsAsync(id, cleanSearch),
                "l05" => await GetTableDataAsync<L05_VPPSupplier, L05_VPPSupplierResDTO>(id, cleanSearch, matchId: x => x.Id == id),
                "l06" => await GetTableDataAsync<L06_VPPSupplierMapping, L06_VPPSupplierMappingResDTO>(id, cleanSearch, matchId: x => x.Id == id),
                "lex02" => await GetTableDataAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(id, cleanSearch,
                    matchId: x => x.Id == id,
                    matchSearch: x => (x.LEX02Code != null && x.LEX02Code.Contains(cleanSearch))
                                   || (x.LEX02Name != null && x.LEX02Name.Contains(cleanSearch))
                                   || x.LEX02Type.Contains(cleanSearch)),
                _ => BadRequest(new { Message = $"Table Code '{tableCode}' is not supported." })
            };
        }

        private async Task<Guid> GetDefaultPriceListIdAsync()
        {
            return await _unitOfWork.VPPContext.Set<gtas_vpp_be.Model.Library.L07_PriceList>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsDefault)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<IActionResult> GetVppItemsAsync(Guid? id, string cleanSearch)
        {
            var vppQuery = _unitOfWork.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted);

            if (id.HasValue)
            {
                vppQuery = vppQuery.Where(x => x.Id == id.Value);
            }

            if (!string.IsNullOrWhiteSpace(cleanSearch))
            {
                vppQuery = vppQuery.Where(x => (x.VPPName != null && x.VPPName.Contains(cleanSearch))
                                            || (x.VPPCode != null && x.VPPCode.Contains(cleanSearch)));
            }

            var vppList = await vppQuery
                .OrderBy(x => x.VPPCode)
                .Take(5000)
                .ToListAsync();

            if (!vppList.Any())
            {
                return Ok(new List<L04_VPPResDTO>());
            }

            var defaultPriceListId = await GetDefaultPriceListIdAsync();
            var vppIds = vppList.Select(v => v.Id).ToList();

            var mappings = await _unitOfWork.VPPContext.Set<L06_VPPSupplierMapping>()
                .AsNoTracking()
                .Where(m => !m.IsDeleted 
                    && m.L07_PriceListId == defaultPriceListId
                    && vppIds.Contains(m.L04_VPPId)
                    && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted))
                .Select(m => new {
                    m.L04_VPPId,
                    m.Price,
                    m.IsDefault,
                    SupplierShortName = m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierShortName : null,
                    SupplierName = m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null
                })
                .ToListAsync();

            var mappingLookup = mappings
                .GroupBy(m => m.L04_VPPId)
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
                return new L04_VPPResDTO
                {
                    Id = x.Id,
                    Description = x.Description,
                    CreateUserId = x.CreateUserId,
                    CreateDate = x.CreateDate,
                    UpdateUserId = x.UpdateUserId,
                    UpdateDate = x.UpdateDate,
                    IsDeleted = x.IsDeleted,
                    VPPCode = x.VPPCode,
                    VPPName = x.VPPName,
                    UOMId = x.UOMId,
                    VPPCategoryId = x.VPPCategoryId,
                    DefaultVatRate = VppPricingDefaults.VatRate,
                    DefaultPrice = priceInfo?.Price,
                    DefaultSupplierName = priceInfo?.SupplierName
                };
            }).ToList();

            return Ok(dtoList);
        }

        private async Task<IActionResult> GetVppItemByIdAsync(Guid id)
        {
            var vpp = await _unitOfWork.VPPContext.Set<L04_VPP>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (vpp == null)
            {
                return NotFound(new { Message = $"Record with ID {id} not found." });
            }

            var defaultPriceListId = await GetDefaultPriceListIdAsync();

            var mappings = await _unitOfWork.VPPContext.Set<L06_VPPSupplierMapping>()
                .AsNoTracking()
                .Where(m => !m.IsDeleted 
                    && m.L07_PriceListId == defaultPriceListId
                    && m.L04_VPPId == id
                    && (m.L05_VPPSupplier == null || !m.L05_VPPSupplier.IsDeleted))
                .Select(m => new {
                    m.Price,
                    m.IsDefault,
                    SupplierShortName = m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierShortName : null,
                    SupplierName = m.L05_VPPSupplier != null ? m.L05_VPPSupplier.SupplierName : null
                })
                .ToListAsync();

            var bestMapping = mappings
                .OrderByDescending(m => m.IsDefault)
                .ThenBy(m => m.SupplierShortName == VppPricingDefaults.DefaultSupplierShortName ? 0 : 1)
                .ThenBy(m => m.SupplierName)
                .FirstOrDefault();

            var dto = new L04_VPPResDTO
            {
                Id = vpp.Id,
                Description = vpp.Description,
                CreateUserId = vpp.CreateUserId,
                CreateDate = vpp.CreateDate,
                UpdateUserId = vpp.UpdateUserId,
                UpdateDate = vpp.UpdateDate,
                IsDeleted = vpp.IsDeleted,
                VPPCode = vpp.VPPCode,
                VPPName = vpp.VPPName,
                UOMId = vpp.UOMId,
                VPPCategoryId = vpp.VPPCategoryId,
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
            Guid? classId = null) where TModel : class
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
                    query = query.Where("IsDeleted == false");
                }

                // Apply classId filter for L02 (typed, not Dynamic LINQ)
                if (classId.HasValue && typeof(TModel) == typeof(L02_ClassDetail))
                {
                    query = (IQueryable<TModel>)((IQueryable<L02_ClassDetail>)query)
                        .Where(x => x.ClassId == classId.Value);
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
        public async Task<IActionResult> GenericGetById(string tableCode, Guid id)
        {
            return tableCode.ToLower() switch
            {
                "l01" => await GetByIdAsync<L01_Class, L01_ClassResDTO>(id),
                "l02" => await GetByIdAsync<L02_ClassDetail, L02_ClassDetailResDTO>(id),
                "l03" => await GetByIdAsync<L03_VPPCategory, L03_VPPCategoryResDTO>(id),
                "l04" => await GetVppItemByIdAsync(id),
                "l05" => await GetByIdAsync<L05_VPPSupplier, L05_VPPSupplierResDTO>(id),
                "l06" => await GetByIdAsync<L06_VPPSupplierMapping, L06_VPPSupplierMappingResDTO>(id),
                "lex02" => await GetByIdAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(id),
                _ => BadRequest(new { Message = $"GetById for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPost("{tableCode}")]
        public async Task<IActionResult> GenericCreate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "l01" => await CreateAsync<L01_Class, L01_ClassResDTO>(json),
                "l02" => await CreateAsync<L02_ClassDetail, L02_ClassDetailResDTO>(json),
                "l03" => await CreateAsync<L03_VPPCategory, L03_VPPCategoryResDTO>(json),
                "l04" => await CreateAsync<L04_VPP, L04_VPPResDTO>(json),
                "l05" => await CreateAsync<L05_VPPSupplier, L05_VPPSupplierResDTO>(json),
                "l06" => await CreateAsync<L06_VPPSupplierMapping, L06_VPPSupplierMappingResDTO>(json),
                "lex02" => await CreateAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(json),
                _ => BadRequest(new { Message = $"Create for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPut("{tableCode}")]
        public async Task<IActionResult> GenericUpdate(string tableCode, [FromBody] JsonElement payload)
        {
            var json = payload.GetRawText();
            return tableCode.ToLower() switch
            {
                "l01" => await UpdateAsync<L01_Class, L01_ClassResDTO>(json),
                "l02" => await UpdateAsync<L02_ClassDetail, L02_ClassDetailResDTO>(json),
                "l03" => await UpdateAsync<L03_VPPCategory, L03_VPPCategoryResDTO>(json),
                "l04" => await UpdateAsync<L04_VPP, L04_VPPResDTO>(json),
                "l05" => await UpdateAsync<L05_VPPSupplier, L05_VPPSupplierResDTO>(json),
                "l06" => await UpdateAsync<L06_VPPSupplierMapping, L06_VPPSupplierMappingResDTO>(json),
                "lex02" => await UpdateAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(json),
                _ => BadRequest(new { Message = $"Update for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpPatch("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericPatch(string tableCode, Guid id, [FromBody] JsonElement payload)
        {
            if (payload.ValueKind == JsonValueKind.Undefined || payload.ValueKind == JsonValueKind.Null)
            {
                return BadRequest(new { Message = "Update payload must not be empty." });
            }

            return tableCode.ToLower() switch
            {
                "l01" => await ApplyPatchAsync<L01_Class, L01_ClassResDTO>(id, payload),
                "l02" => await ApplyPatchAsync<L02_ClassDetail, L02_ClassDetailResDTO>(id, payload),
                "l03" => await ApplyPatchAsync<L03_VPPCategory, L03_VPPCategoryResDTO>(id, payload),
                "l04" => await ApplyPatchAsync<L04_VPP, L04_VPPResDTO>(id, payload),
                "l05" => await ApplyPatchAsync<L05_VPPSupplier, L05_VPPSupplierResDTO>(id, payload),
                "l06" => await ApplyPatchAsync<L06_VPPSupplierMapping, L06_VPPSupplierMappingResDTO>(id, payload),
                "lex02" => await ApplyPatchAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(id, payload),
                _ => BadRequest(new { Message = $"Patch for Table Code '{tableCode}' is not supported." })
            };
        }

        [HttpDelete("{tableCode}/{id:guid}")]
        public async Task<IActionResult> GenericDelete(string tableCode, Guid id)
        {
            return tableCode.ToLower() switch
            {
                "l01" => await DeleteAsync<L01_Class>(id),
                "l02" => await DeleteAsync<L02_ClassDetail>(id),
                "l03" => await DeleteAsync<L03_VPPCategory>(id),
                "l04" => await DeleteAsync<L04_VPP>(id),
                "l05" => await DeleteAsync<L05_VPPSupplier>(id),
                "l06" => await DeleteAsync<L06_VPPSupplierMapping>(id),
                "lex02" => await DeleteAsync<LEX02_CompanyDepartmentLocation>(id),
                _ => BadRequest(new { Message = $"Delete for Table Code '{tableCode}' is not supported." })
            };
        }

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
        // Audit fields the client must never write. IsDeleted is intentionally NOT here:
        // Library admin pages (Tab_ClassLibrary, etc.) toggle Enable/Disable via PATCH with
        // `IsDeleted = true|false`. Access to these admin actions is gated FE-side via
        // PermissionState.GetPagePermission(PageLibrary) component flags. Adding IsDeleted
        // back to this list would silently break the toggle (PATCH returns 200 with the
        // unchanged entity → misleading "Success" toast in FE).
        private static readonly HashSet<string> _writeDeniedFields = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id",
            "CreateUserId",
            "CreateDate",
            "UpdateUserId",
            "UpdateDate"
        };

        private async Task<IActionResult> CreateAsync<TModel, TDto>(string json) where TModel : gtas_vpp_be.Model.Helpers.BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            obj.Id = Guid.Empty;
            var now = _dateTimeProvider.Now;
            var uid = int.TryParse(User.FindFirstValue("UserID"), out var x) ? x : 0;
            obj.CreateDate = now;
            obj.UpdateDate = now;
            obj.CreateUserId = uid;
            obj.UpdateUserId = uid;
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
            obj.CreateUserId = existing.CreateUserId;
            obj.CreateDate = existing.CreateDate;
            obj.IsDeleted = existing.IsDeleted;
            obj.UpdateDate = _dateTimeProvider.Now;
            obj.UpdateUserId = uid;

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

            var updateDateProp = type.GetProperty("UpdateDate");
            if (updateDateProp != null && updateDateProp.CanWrite)
            {
                updateDateProp.SetValue(entity, _dateTimeProvider.Now);
            }

            var updateUserIdProp = type.GetProperty("UpdateUserId");
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
