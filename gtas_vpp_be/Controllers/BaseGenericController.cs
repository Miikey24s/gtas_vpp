using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
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

        protected async Task<IActionResult> GetTableDataAsync<T>(
            Guid? id,
            string cleanSearch,
            Expression<Func<T, bool>> matchId,
            Expression<Func<T, bool>>? matchSearch = null) where T : class
        {
            if (id.HasValue)
            {
                var dataById = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_GetTAsync, true, matchId);
                return Ok(dataById ?? new List<T>());
            }

            if (!string.IsNullOrEmpty(cleanSearch) && matchSearch != null)
            {
                var dataBySearch = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_GetTAsync, true, matchSearch);
                return Ok(dataBySearch ?? new List<T>());
            }

            var allData = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_GetTAsync, true);
            return Ok(allData ?? new List<T>());
        }

        protected async Task<IActionResult> GetByIdAsync<T>(Guid id) where T : class
        {
            var data = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
            return Ok(data?.FirstOrDefault());
        }

        protected async Task<IActionResult> CreateAsync<T>(string json) where T : BaseModel
        {
            var obj = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (obj == null) return BadRequest();
            obj.Id = Guid.Empty;
            obj.CreateDate = DateTime.Now;
            obj.UpdateDate = DateTime.Now;
            var rs = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_Create, objs: new List<T> { obj });
            return Ok(rs?.FirstOrDefault());
        }

        protected async Task<IActionResult> UpdateAsync<T>(string json) where T : BaseModel
        {
            var obj = JsonSerializer.Deserialize<T>(json, _jsonOptions);
            if (obj == null) return BadRequest();
            obj.UpdateDate = DateTime.Now;
            var rs = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_Update, objs: new List<T> { obj });
            return Ok(rs?.FirstOrDefault());
        }

        protected async Task<IActionResult> ApplyPatchAsync<T>(Guid id, JsonElement payload) where T : class
        {
            var existingData = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_GetTByIdAsync, true, Param: id);
            var entity = existingData?.FirstOrDefault();

            if (entity == null)
                return NotFound(new { Message = $"Record with ID {id} not found." });

            var type = typeof(T);
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

            var result = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_Update, objs: new List<T> { entity });
            return Ok(result?.FirstOrDefault());
        }

        protected async Task<IActionResult> DeleteAsync<T>(Guid id) where T : class
        {
            var rs = await _bussinessService.BaseService<T>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
            return Ok(new { success = rs != null });
        }
    }
}
