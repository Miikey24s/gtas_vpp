using gtas_vpp_be.Service.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Service.Services
{
    public interface IBaseServices
    {
        Task<sp_ResDTO> SP(string typeofdbContext, string spName, string spType, object param, int? timeout = 300, string? env = null);
        Task<sp_ResDTO> Query(string typeofdbContext, string query, int? timeout = 300, string? env = null);
    }
    public class BaseServices : IBaseServices
    {
        [Inject] public IUnitOfWorkFactory _unitOfWork { get; set; } = default!;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public BaseServices(IUnitOfWorkFactory unitOfWork, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gọi SP
        /// </summary>
        /// <param name="typeofdbContext"></param>
        /// <param name="spName"></param>
        /// <param name="spType"></param>
        /// <param name="param"></param>
        /// <param name="env"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<sp_ResDTO> SP(string typeofdbContext, string spName, string spType, object param, int? timeout = 300, string? env = null)
        {
            sp_ResDTO sp_ResDTO = new sp_ResDTO();
            using var uow = _unitOfWork.Create(nameof(Config.EnvConfig.EnvType.TestEnv));
            uow.VPPContext.Database.SetCommandTimeout(timeout);
            try
            {
                switch (typeofdbContext)
                {
                    case nameof(Config.EnvConfig.ContextType.VPPMigrationDbContext):
                        sp_ResDTO = (await uow.VPPContext.Set<sp_ResDTO>()
                                    .FromSqlRaw("exec {0} @SpType={1}, @Param={2}", spName, spType, JsonConvert.SerializeObject(param))
                                    .ToListAsync()).FirstOrDefault() ?? new sp_ResDTO();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                sp_ResDTO.IsSuccess = false;
                sp_ResDTO.ErrorMess = ex.Message;
                //_logger.LogError(ex, "Lỗi khi chạy EFBaseServiceRead " + spName + " - " + spType + " - " + JsonConvert.SerializeObject(param));
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            finally
            {
                uow.Dispose();
            }
            return sp_ResDTO;
        }
        public async Task<sp_ResDTO> Query(string typeofdbContext, string query, int? timeout = 300, string? env = null)
        {
            sp_ResDTO sp_ResDTO = new sp_ResDTO();
            using var uow = _unitOfWork.Create(nameof(Config.EnvConfig.EnvType.TestEnv));
            uow.VPPContext.Database.SetCommandTimeout(timeout);
            try
            {
                switch (typeofdbContext)
                {
                    case nameof(Config.EnvConfig.ContextType.VPPMigrationDbContext):
                        sp_ResDTO = (await uow.VPPContext.Set<sp_ResDTO>()
                                    .FromSqlRaw(query)
                                    .ToListAsync()).FirstOrDefault() ?? new sp_ResDTO();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                sp_ResDTO.IsSuccess = false;
                sp_ResDTO.ErrorMess = ex.Message;
                //_logger.LogError(ex, "Lỗi khi chạy EFBaseServiceRead " + spName + " - " + spType + " - " + JsonConvert.SerializeObject(param));
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            finally
            {
                uow.Dispose();
            }
            return sp_ResDTO;
        }
    }
}
