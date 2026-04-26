using gtas_vpp_shared.DTOs;

namespace gtas_vpp_be.Service.Services
{
    public interface IStoredProcedureExecutor
    {
        Task<sp_ResDTO> ExecuteSPAsync(string spName, string spType, object param, int? timeout = 300);
        Task<sp_ResDTO> ExecuteQueryAsync(string query, int? timeout = 300);
    }
}
