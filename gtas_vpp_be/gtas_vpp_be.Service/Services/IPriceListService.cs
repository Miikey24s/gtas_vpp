using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services
{
    public interface IPriceListService
    {
        Task<List<PriceListResDTO>> ListAsync(bool showDeleted = false);
        Task<(List<PriceListResDTO> Data, int TotalCount)> QueryAsync(
            bool showDeleted = false,
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null);
        Task<PriceListResDTO?> GetByIdAsync(Guid id);
        Task<PriceListResDTO> CreateAsync(PriceListCreateReqDTO req, int userId);
        Task<PriceListResDTO> UpdateAsync(PriceListUpdateReqDTO req, int userId);
        Task<PriceListResDTO> SetDeletedAsync(Guid id, bool isDeleted, int userId);
        Task DeleteAsync(Guid id, int userId);
        Task HardDeleteAsync(Guid id);
        Task SetDefaultAsync(Guid id, int userId);
        Task<PriceListResDTO> CloneAsync(PriceListCloneReqDTO req, int userId);
    }
}
