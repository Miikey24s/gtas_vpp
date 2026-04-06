using gtas_vpp_be.Service.Helpers;
using gtas_vpp_be.Service.Helpers.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace gtas_vpp_be.Service.Services
{
    public interface IBussinessService
    {
        Task<sp_ResDTO> SP(string sp_Name, string sp_Type, object? Param = null, int? timeout = 300);
        Task<sp_ResDTO> Query(string query, int? timeout = 300);
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
                sp_ResDTO = await SP(nameof(Config.EnvConfig.ContextType.VPPContext), sp_Name, sp_Type, Param ?? new { }, timeout);
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
                sp_ResDTO = await Query(nameof(Config.EnvConfig.ContextType.VPPContext), query, timeout);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in EFBaseServiceRead: {ex.Message}", ex);
            }
            return sp_ResDTO;
        }
    }
}
