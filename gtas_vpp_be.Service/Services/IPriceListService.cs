using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services
{
    public interface IPriceListService
    {
        Task<List<L07_PriceListResDTO>> ListAsync(bool showDeleted = false);
        Task<(List<L07_PriceListResDTO> Data, int TotalCount)> QueryAsync(
            bool showDeleted = false,
            string? filter = null,
            int? skip = null,
            int? top = null,
            string? orderby = null,
            string? distinct = null,
            string? distinctFilter = null);
        Task<L07_PriceListResDTO?> GetByIdAsync(Guid id);
        Task<L07_PriceListResDTO> CreateAsync(L07_PriceListCreateReqDTO req, int userId);
        Task<L07_PriceListResDTO> UpdateAsync(L07_PriceListUpdateReqDTO req, int userId);
        Task<L07_PriceListResDTO> SetDeletedAsync(Guid id, bool isDeleted, int userId);
        Task DeleteAsync(Guid id, int userId);
        Task HardDeleteAsync(Guid id);
        Task SetDefaultAsync(Guid id, int userId);
        Task<L07_PriceListResDTO> CloneAsync(L07_PriceListCloneReqDTO req, int userId);
    }
}
