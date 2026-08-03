using gtas_vpp_shared.DTOs.Req.VPP;

namespace gtas_vpp_fe.Features.Requests.Approval;

public sealed class SupplementDecisionRequestFactory
{
    private readonly Dictionary<(Guid OrderId, SupplementDecision Action), string> _idempotencyKeys = [];

    public ApproveOrderReqDTO BuildApprove(Guid orderId, byte[]? rowVersion) => new()
    {
        RowVersion = rowVersion,
        IdempotencyKey = GetIdempotencyKey(orderId, SupplementDecision.Approve)
    };

    public RejectOrderReqDTO BuildReject(Guid orderId, byte[]? rowVersion, string reason) => new()
    {
        Reason = reason,
        RowVersion = rowVersion,
        IdempotencyKey = GetIdempotencyKey(orderId, SupplementDecision.Reject)
    };

    private string GetIdempotencyKey(Guid orderId, SupplementDecision action)
    {
        var key = (orderId, action);
        if (_idempotencyKeys.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var created = Guid.NewGuid().ToString("N");
        _idempotencyKeys[key] = created;
        return created;
    }

    private enum SupplementDecision
    {
        Approve,
        Reject
    }
}
