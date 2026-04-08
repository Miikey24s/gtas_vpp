using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.Context;
using gtas_vpp_be.Service.Helpers.DTOs;
using gtas_vpp_be.Service.Helpers.DTOs.Res;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using static gtas_vpp_be.Service.Helpers.Config;
using static gtas_vpp_be.Service.Helpers.Config.EnvConfig;

namespace gtas_vpp_be.Service.Services
{
    public interface IBussinessService
    {
        Task<sp_ResDTO> SP(string sp_Name, string sp_Type, object? Param = null, int? timeout = 300);
        Task<sp_ResDTO> Query(string query, int? timeout = 300);
        Task<List<T>?> BaseService<T>(EF_BASEMETHOD eF_BASEMETHOD,
                                                   bool? getFullName = null,
                                                   Expression<Func<T, bool>>? expression = null,
                                                   Func<IQueryable<T>, IQueryable<T>>? include = null,
                                                   List<T>? objs = null,
                                                   object? Param = null,
                                                   Expression<Func<T, object>>[]? properties = null
                                                ) where T : class;
    }
    public class BussinessService : BaseServices, IBussinessService
    {
        public BussinessService(IUnitOfWorkFactory uow, IHttpContextAccessor httpContextAccessor)
                : base(uow, httpContextAccessor) { }
        public async Task<sp_ResDTO> SP(string sp_Name, string sp_Type, object? Param = null, int? timeout = 300)
        {
            sp_ResDTO sp_ResDTO = new sp_ResDTO();
            try
            {
                sp_ResDTO = await SP(nameof(Config.ContextType.VPPContext), sp_Name, sp_Type, Param ?? new { }, timeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            return sp_ResDTO;
        }
        public async Task<sp_ResDTO> Query(string query, int? timeout = 300)
        {
            sp_ResDTO sp_ResDTO = new sp_ResDTO();
            try
            {
                sp_ResDTO = await Query(nameof(Config.ContextType.VPPContext), query, timeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            return sp_ResDTO;
        }
        
        

        
        public async Task<List<T>?> BaseService<T>(EF_BASEMETHOD eF_BASEMETHOD,
                                                   bool? getFullName = null,
                                                   Expression<Func<T, bool>>? expression = null,
                                                   Func<IQueryable<T>, IQueryable<T>>? include = null,
                                                   List<T>? objs = null,
                                                   object? Param = null,
                                                   Expression<Func<T, object>>[]? properties = null
                                                ) where T : class
        {
            List<T>? result = default;
            switch (eF_BASEMETHOD)
            {
                case EF_BASEMETHOD.EF_GetTAsync:
                    result = await ReadAsync<T>(nameof(ContextType.VPPContext), getFullName, expression, include);
                    break;
                //case EF_BASEMETHOD.EF_GetTAsync_Paging:
                //    PagingRequest paging = (PagingRequest)Param!;
                //    PagingResponse<T> pagingResponse = await ReadAsyncWithPaging<T>(nameof(ContextType.VPPContext), paging, getFullName, expression, include);
                //    result = pagingResponse;
                //    break;
                case EF_BASEMETHOD.EF_GetTByIdAsync:
                    result = await GetByIdAsync<T>(nameof(ContextType.VPPContext), Param!, getFullName).ContinueWith(t => new List<T>() { t.Result ?? default! });
                    break;
                case EF_BASEMETHOD.EF_GetTByIdIncludeAsync:
                    result = await GetByIdIncludeAsync<T>(nameof(ContextType.VPPContext), Param!, true, include).ContinueWith(t => new List<T>() { t.Result ?? default! });
                    break;
                case EF_BASEMETHOD.EF_Create:
                    result = await AddAsync(objs![0], nameof(ContextType.VPPContext)).ContinueWith(t => new List<T>() { t.Result });
                    break;
                case EF_BASEMETHOD.EF_Update:
                    if (properties is not null)
                    {
                        result = await UpdateAsync(objs![0], nameof(ContextType.VPPContext), properties).ContinueWith(t => new List<T>() { t.Result });
                    }
                    else
                    {
                        result = await UpdateAsync(objs![0], nameof(ContextType.VPPContext)).ContinueWith(t => new List<T>() { t.Result });
                    }
                    break;
                case EF_BASEMETHOD.EF_UpdateRange:
                    result = await UpdateRangeTAsync(objs!, nameof(ContextType.VPPContext), expression!);
                    break;
                case EF_BASEMETHOD.EF_DeleteAsync:
                    if (await DeleteAsync<T>(Param!, nameof(ContextType.VPPContext)))
                    {
                        result = new List<T>();
                    }
                    else
                    {
                        result = null;
                    }
                    break;
                default:
                    result = new List<T>();
                    break;
            }
            if (Enum.GetName(typeof(EF_BASEMETHOD), eF_BASEMETHOD) == nameof(EF_BASEMETHOD.EF_DeleteAsync))
            {
                return result;
            }
            else
            {
                return result != null ? result : new List<T>();
            }
        }
    }
}
