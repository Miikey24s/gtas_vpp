using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Requests.Api;

public sealed class RequestsCommandClient(IAPIServices api)
{
    private const string RequestsBase = "/api/VPPRequest";

    public Task<VppRequestResDTO?> CreateAsync(VppRequestCreateReqDTO request) =>
        api.PostFromApiAsync<VppRequestResDTO>($"{RequestsBase}/orders", request);

    public Task<VppRequestResDTO?> UpdateAsync(Guid orderId, VppRequestUpdateReqDTO request) =>
        api.PutFromApiAsync<VppRequestResDTO>($"{RequestsBase}/orders/{orderId}", request);

    public Task<VppRequestResDTO?> AdjustAfterCloseAsync(
        Guid orderId,
        VppManagerOrderAdjustmentReqDTO request) =>
        api.PostFromApiAsync<VppRequestResDTO>(
            $"{RequestsBase}/orders/{orderId}/manager-adjustment",
            request);

    public Task<VppRequestResDTO?> RecreateAsync(Guid orderId, VppRequestRecreateReqDTO request) =>
        api.PostFromApiAsync<VppRequestResDTO>($"{RequestsBase}/orders/{orderId}/recreate", request);

    public async Task CancelAsync(Guid orderId, VppRequestCancelReqDTO request)
    {
        _ = await api.PostFromApiAsync<object>($"{RequestsBase}/orders/{orderId}/cancel", request);
    }

    public Task<VppRequestResDTO?> RestoreAsync(Guid orderId, VppRequestRestoreReqDTO request) =>
        api.PostFromApiAsync<VppRequestResDTO>($"{RequestsBase}/orders/{orderId}/restore", request);

    public async Task ApproveAdditionalAsync(Guid orderId, ApproveOrderReqDTO request)
    {
        _ = await api.PostFromApiAsync<object>($"{RequestsBase}/additional-orders/{orderId}/approve", request);
    }

    public async Task RejectAdditionalAsync(Guid orderId, RejectOrderReqDTO request)
    {
        _ = await api.PostFromApiAsync<object>($"{RequestsBase}/additional-orders/{orderId}/reject", request);
    }
}
