using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using gtas_vpp_shared.DTOs.Res.Library;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Linq.Dynamic.Core;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class LibraryController : BaseGenericController
    {
        public LibraryController(IBussinessService bussinessService) : base(bussinessService) { }

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
            [FromQuery] string? distinct)
        {
            string cleanSearch = searchText?.Trim() ?? string.Empty;

            // Check if this is a LoadData request (has any of the advanced parameters)
            bool isLoadDataRequest = !string.IsNullOrEmpty(filter) || skip.HasValue || top.HasValue || 
                                     !string.IsNullOrEmpty(orderby) || !string.IsNullOrEmpty(distinct);

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
                    "l01" => await GetTableDataWithFilteringAsync<L01_Class, L01_ClassResDTO>(filter, skip, top, orderby, distinct),
                    "l02" => await GetTableDataWithFilteringAsync<L02_ClassDetail, L02_ClassDetailResDTO>(filter, skip, top, orderby, distinct, classId),
                    "lex02" => await GetTableDataWithFilteringAsync<LEX02_CompanyDepartmentLocation, LEX02_CompanyDepartmentLocationResDTO>(filter, skip, top, orderby, distinct),
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
                "l04" => await GetTableDataAsync<L04_VPP, L04_VPPResDTO>(id, cleanSearch, matchId: x => x.Id == id),
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

        private async Task<IActionResult> GetTableDataWithFilteringAsync<TModel, TDto>(
            string? filter,
            int? skip,
            int? top,
            string? orderby,
            string? distinct,
            Guid? classId = null) where TModel : class
        {
            try
            {
                // Get all data
                var allData = await _bussinessService.BaseService<TModel>(Config.EF_BASEMETHOD.EF_GetTAsync, true);
                
                if (allData == null || !allData.Any())
                {
                    Response.Headers.Append("X-Total-Count", "0");
                    return Ok(new List<TDto>());
                }

                var query = allData.AsQueryable();

                // Apply classId filter for L02
                if (classId.HasValue && typeof(TModel) == typeof(L02_ClassDetail))
                {
                    query = query.Where(x => ((L02_ClassDetail)(object)x).ClassId == classId.Value);
                }

                // Apply filter (Radzen filter format)
                if (!string.IsNullOrEmpty(filter))
                {
                    try
                    {
                        // Parse Radzen filter format and apply
                        query = query.Where(filter);
                    }
                    catch
                    {
                        // If filter parsing fails, ignore it
                    }
                }

                // Get total count before paging
                var totalCount = query.Count();

                // Handle distinct request for filter dropdowns
                if (!string.IsNullOrEmpty(distinct))
                {
                    var propertyInfo = typeof(TModel).GetProperty(distinct);
                    if (propertyInfo != null)
                    {
                        var distinctValues = query
                            .Select(x => propertyInfo.GetValue(x))
                            .Where(x => x != null)
                            .Distinct()
                            .ToList();

                        var distinctDtos = distinctValues.Select(val =>
                        {
                            var dto = Activator.CreateInstance<TDto>();
                            var dtoProp = typeof(TDto).GetProperty(distinct);
                            if (dtoProp != null)
                            {
                                dtoProp.SetValue(dto, val);
                            }
                            return dto;
                        }).ToList();

                        Response.Headers.Append("X-Total-Count", distinctDtos.Count.ToString());
                        return Ok(distinctDtos);
                    }
                }

                // Apply sorting
                if (!string.IsNullOrEmpty(orderby))
                {
                    try
                    {
                        query = query.OrderBy(orderby);
                    }
                    catch
                    {
                        // If sorting fails, use default ordering
                    }
                }

                // Apply paging
                if (skip.HasValue && skip.Value > 0)
                {
                    query = query.Skip(skip.Value);
                }

                if (top.HasValue && top.Value > 0)
                {
                    query = query.Take(top.Value);
                }

                var result = query.ToList();
                var dtoList = result.Adapt<List<TDto>>();

                // Add total count to response header
                Response.Headers.Append("X-Total-Count", totalCount.ToString());

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error processing request: {ex.Message}", StackTrace = ex.StackTrace });
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
                "l04" => await GetByIdAsync<L04_VPP, L04_VPPResDTO>(id),
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

        private async Task<IActionResult> CreateAsync<TModel, TDto>(string json) where TModel : gtas_vpp_be.Model.Helpers.BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            obj.Id = Guid.Empty;
            obj.CreateDate = DateTime.Now;
            obj.UpdateDate = DateTime.Now;
            
            var rs = await _bussinessService.BaseService<TModel>(Config.EF_BASEMETHOD.EF_Create, objs: new List<TModel> { obj });
            var resultDto = rs?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(resultDto);
        }

        private async Task<IActionResult> UpdateAsync<TModel, TDto>(string json) where TModel : gtas_vpp_be.Model.Helpers.BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            obj.UpdateDate = DateTime.Now;
            
            var rs = await _bussinessService.BaseService<TModel>(Config.EF_BASEMETHOD.EF_Update, objs: new List<TModel> { obj });
            var resultDto = rs?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(resultDto);
        }

        private async Task<IActionResult> ApplyPatchAsync<TModel, TDto>(Guid id, JsonElement payload) where TModel : class where TDto : class
        {
            var existingData = await _bussinessService.BaseService<TModel>(Config.EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
            var entity = existingData?.FirstOrDefault();

            if (entity == null)
                return NotFound(new { Message = $"Record with ID {id} not found." });

            var type = typeof(TModel);
            foreach (var jsonProperty in payload.EnumerateObject())
            {
                if (jsonProperty.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;

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
                updateDateProp.SetValue(entity, DateTime.Now);
            }

            var result = await _bussinessService.BaseService<TModel>(Config.EF_BASEMETHOD.EF_Update, objs: new List<TModel> { entity });
            var resultDto = result?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(resultDto);
        }
    }
}
