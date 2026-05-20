using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services
{
    public interface IVPPPriceService
    {
        Task<List<L06_VPPSupplierMappingResDTO>> ListByVPPAsync(Guid vppId);
        Task<L06_VPPSupplierMappingResDTO> CreateAsync(L06_PriceCreateReqDTO req, int userId);
        Task<L06_VPPSupplierMappingResDTO> UpdateAsync(L06_PriceUpdateReqDTO req, int userId);
        Task DeleteAsync(Guid id, int userId);
        Task SetDefaultAsync(Guid id, int userId);
    }
}
