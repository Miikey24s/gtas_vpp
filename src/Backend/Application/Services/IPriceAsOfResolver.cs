using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_be.Service.Services;

public interface IPriceAsOfResolver
{
    Task<PriceResolutionResDTO> ResolveAsync(
        PriceResolutionReqDTO request,
        CancellationToken cancellationToken = default);
}
