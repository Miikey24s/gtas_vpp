using gtas_vpp_shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace gtas_vpp_be.Service.Services
{
    public class StoredProcedureExecutor : IStoredProcedureExecutor
    {
        private readonly IUnitOfWork _unitOfWork;

        public StoredProcedureExecutor(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<sp_ResDTO> ExecuteSPAsync(string spName, string spType, object param, int? timeout = 300)
        {
            _unitOfWork.VPPContext.Database.SetCommandTimeout(timeout);
            try
            {
                var spResult = await _unitOfWork.VPPContext.Set<sp_ResDTO>()
                    .FromSqlRaw("exec {0} @SpType={1}, @Param={2}", spName, spType, JsonConvert.SerializeObject(param))
                    .ToListAsync();

                return spResult.FirstOrDefault() ?? new sp_ResDTO
                {
                    IsSuccess = false,
                    ErrorMess = "No data returned from stored procedure."
                };
            }
            catch (Exception ex)
            {
                return new sp_ResDTO
                {
                    IsSuccess = false,
                    ErrorMess = ex.Message
                };
            }
        }

    }
}
