using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services;

public interface IPriceBookWorkflowService
{
    Task<L07_PriceListResDTO> PublishAsync(
        Guid id,
        PriceBookStatusReqDTO request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<L07_PriceListResDTO> ExpireAsync(
        Guid id,
        PriceBookStatusReqDTO request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<PriceBookComparisonResDTO> CompareAsync(
        PriceBookComparisonReqDTO request,
        CancellationToken cancellationToken = default);
}
