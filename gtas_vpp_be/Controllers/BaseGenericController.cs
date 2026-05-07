using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Services;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Text.Json;

namespace gtas_vpp_be.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public abstract class BaseGenericController : ControllerBase

    {
        protected readonly IServiceProvider _serviceProvider;
        protected readonly IUserNameResolver _userNameResolver;
        protected readonly IUnitOfWork _unitOfWork;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        protected BaseGenericController(IServiceProvider serviceProvider, IUserNameResolver userNameResolver, IUnitOfWork unitOfWork)
        {
            _serviceProvider = serviceProvider;
            _userNameResolver = userNameResolver;
            _unitOfWork = unitOfWork;
        }

        protected async Task<IActionResult> GetTableDataAsync<TModel, TDto>(
            Guid? id,
            string cleanSearch,
            Expression<Func<TModel, bool>> matchId,
            Expression<Func<TModel, bool>>? matchSearch = null) where TModel : class
        {
            if (id.HasValue)
            {
                var dataById = await ReadEntitiesAsync(true, matchId);
                var dtoList = dataById?.Adapt<List<TDto>>();
                return Ok(dtoList ?? new List<TDto>());
            }

            if (!string.IsNullOrEmpty(cleanSearch) && matchSearch != null)
            {
                var dataBySearch = await ReadEntitiesAsync(true, matchSearch);
                var dtoList = dataBySearch?.Adapt<List<TDto>>();
                return Ok(dtoList ?? new List<TDto>());
            }

            var allData = await ReadEntitiesAsync<TModel>(true, take: 1000);
            var allDtoList = allData?.Adapt<List<TDto>>();
            return Ok(allDtoList ?? new List<TDto>());
        }

        protected async Task<IActionResult> GetByIdAsync<TModel, TDto>(Guid id) where TModel : class where TDto : class
        {
            var data = await GetEntityByIdAsync<TModel>(id, true);
            var dto = data?.Adapt<TDto>();
            return Ok(dto);
        }

        protected async Task<IActionResult> DeleteAsync<TModel>(Guid id) where TModel : class
        {
            var success = await GetRepository<TModel>().DeleteAsync(id);
            return Ok(new { success });
        }

        protected IGenericRepository<TModel> GetRepository<TModel>() where TModel : class
        {
            return _serviceProvider.GetRequiredService<IGenericRepository<TModel>>();
        }

        protected async Task<List<TModel>> ReadEntitiesAsync<TModel>(
            bool getFullName,
            Expression<Func<TModel, bool>>? filter = null,
            Func<IQueryable<TModel>, IQueryable<TModel>>? include = null,
            int? take = null) where TModel : class
        {
            var data = await GetRepository<TModel>().ReadAsync(filter, include, take);
            return getFullName
                ? await _userNameResolver.WithUserNamesAsync(data, _unitOfWork.VPPContext)
                : data;
        }

        protected async Task<TModel?> GetEntityByIdAsync<TModel>(object id, bool getFullName) where TModel : class
        {
            var entity = await GetRepository<TModel>().GetByIdAsync(id);
            if (entity is not null && getFullName)
            {
                await _userNameResolver.IncludeUserInfoAsync(entity, _unitOfWork.VPPContext);
            }

            return entity;
        }
    }
}
