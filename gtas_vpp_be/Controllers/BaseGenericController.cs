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
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
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

        protected async Task<IActionResult> DeleteAsync<TModel>(Guid id) where TModel : class
        {
            var rs = await _bussinessService.BaseService<TModel>(EF_BASEMETHOD.EF_DeleteAsync, Param: id);
            return Ok(new { success = rs != null });
        }
    }
}
