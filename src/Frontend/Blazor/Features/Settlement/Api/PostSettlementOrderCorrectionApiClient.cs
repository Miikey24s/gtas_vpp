using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Settlement.Api;

public sealed class PostSettlementOrderCorrectionApiClient(IAPIServices api)
{
    private const string BasePath = "/api/post-settlement-order-corrections";

    public async Task<IReadOnlyList<PostSettlementOrderCorrectionResDTO>> ListAsync(Guid? periodId = null) =>
        await api.GetFromApiAsync<List<PostSettlementOrderCorrectionResDTO>>(periodId.HasValue
            ? $"{BasePath}?periodId={periodId.Value}"
            : BasePath) ?? [];

    public Task<PostSettlementOrderCorrectionResDTO?> CreateAsync(PostSettlementOrderCorrectionCreateReqDTO request) =>
        api.PostFromApiAsync<PostSettlementOrderCorrectionResDTO>(BasePath, request);

    public Task<PostSettlementOrderCorrectionResDTO?> ConfirmAsync(
        Guid id,
        PostSettlementOrderCorrectionDecisionReqDTO request) =>
        api.PostFromApiAsync<PostSettlementOrderCorrectionResDTO>($"{BasePath}/{id}/confirm", request);

    public Task<PostSettlementOrderCorrectionResDTO?> RejectAsync(
        Guid id,
        PostSettlementOrderCorrectionDecisionReqDTO request) =>
        api.PostFromApiAsync<PostSettlementOrderCorrectionResDTO>($"{BasePath}/{id}/reject", request);
}
