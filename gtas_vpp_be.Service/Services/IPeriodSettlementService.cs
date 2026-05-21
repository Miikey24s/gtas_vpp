using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_be.Service.Services
{
    public interface IPeriodSettlementService
    {
        Task<VPP_PeriodSettlementResDTO> SettleAsync(VPP_SettlePeriodReqDTO req, int userId);
        Task<VPP_PeriodSettlementResDTO> GetStatusAsync(int y, int m);
        Task<List<VPP_PeriodSettlementResDTO>> ListSettledAsync();
    }
}
