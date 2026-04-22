using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Text.Json;
using static gtas_vpp_be.Service.Helpers.Config;

namespace gtas_vpp_be.Controllers
{
    public abstract class BaseGenericController : ControllerBase

    {
        protected readonly IBussinessService _bussinessService;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        protected BaseGenericController(IBussinessService bussinessService)
        {
            _bussinessService = bussinessService;
        }

        protected async Task<IActionResult> GetTableDataAsync<TModel, TDto>(
            Guid? id,
            string cleanSearch,
            Expression<Func<TModel, bool>> matchId,
            Expression<Func<TModel, bool>>? matchSearch = null) where TModel : class
        {
            if (id.HasValue)
            {
                var dataById = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_GetTAsync, true, matchId);
                var dtoList = dataById?.Adapt<List<TDto>>();
                return Ok(dtoList ?? new List<TDto>());
            }

            if (!string.IsNullOrEmpty(cleanSearch) && matchSearch != null)
            {
                var dataBySearch = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_GetTAsync, true, matchSearch);
                var dtoList = dataBySearch?.Adapt<List<TDto>>();
                return Ok(dtoList ?? new List<TDto>());
            }

            var allData = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_GetTAsync, true);
            var allDtoList = allData?.Adapt<List<TDto>>();
            return Ok(allDtoList ?? new List<TDto>());
        }

        protected async Task<IActionResult> GetByIdAsync<TModel, TDto>(Guid id) where TModel : class where TDto : class
        {
            var data = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
            var dto = data?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(dto);
        }

        protected async Task<IActionResult> CreateAsync<TModel, TDto>(string json) where TModel : BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            obj.Id = Guid.Empty;
            obj.CreateDate = DateTime.Now;
            obj.UpdateDate = DateTime.Now;
            
            var rs = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_Create, objs: new List<TModel> { obj });
            var resultDto = rs?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(resultDto);
        }

        protected async Task<IActionResult> UpdateAsync<TModel, TDto>(string json) where TModel : BaseModel where TDto : class
        {
            var dto = JsonSerializer.Deserialize<TDto>(json, _jsonOptions);
            if (dto == null) return BadRequest();
            
            var obj = dto.Adapt<TModel>();
            obj.UpdateDate = DateTime.Now;
            
            var rs = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_Update, objs: new List<TModel> { obj });
            var resultDto = rs?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(resultDto);
        }

        protected async Task<IActionResult> ApplyPatchAsync<TModel, TDto>(Guid id, JsonElement payload) where TModel : class where TDto : class
        {
            var existingData = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
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

            var result = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_Update, objs: new List<TModel> { entity });
            var resultDto = result?.FirstOrDefault()?.Adapt<TDto>();
            return Ok(resultDto);
        }

        protected async Task<IActionResult> DeleteAsync<TModel>(Guid id) where TModel : class
        {
            var rs = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
            return Ok(new { success = rs != null });
        }
    }
}
