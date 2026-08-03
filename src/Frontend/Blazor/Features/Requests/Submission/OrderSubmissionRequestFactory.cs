using gtas_vpp_shared.DTOs.Req.VPP;

namespace gtas_vpp_fe.Features.Requests.Submission;

public sealed record OrderSubmissionItem(
    Guid VppId,
    int Quantity,
    string? Description);

public sealed record OrderSubmissionSnapshot(
    string? Description,
    string? SupplementReason,
    string IdempotencyKey,
    IReadOnlyList<OrderSubmissionItem> Items);

public static class OrderSubmissionRequestFactory
{
    public static VppRequestCreateReqDTO BuildCreateRequest(
        OrderSubmissionSnapshot submission,
        int year,
        int month,
        bool isAdditionalOrder,
        bool includeAdditionalContext,
        Guid? baseRequestId) => new()
        {
            Year = year,
            Month = month,
            Description = submission.Description,
            IsAdditionalOrder = isAdditionalOrder,
            BaseRequestId = includeAdditionalContext ? baseRequestId : null,
            SupplementReason = includeAdditionalContext ? submission.SupplementReason?.Trim() : null,
            IdempotencyKey = submission.IdempotencyKey,
            Items = BuildItems(submission.Items)
        };

    public static VppRequestUpdateReqDTO BuildUpdateRequest(
        OrderSubmissionSnapshot submission,
        Guid orderId,
        bool isAdditional,
        byte[]? rowVersion) => new()
        {
            Id = orderId,
            Description = submission.Description,
            IsAdditionalOrder = isAdditional,
            // Giữ payload update hiện hành; backend tiếp tục là nơi kiểm tra nghiệp vụ của lý do bổ sung.
            SupplementReason = submission.SupplementReason,
            RowVersion = rowVersion,
            IdempotencyKey = submission.IdempotencyKey,
            Items = BuildItems(submission.Items)
        };

    public static VppRequestRecreateReqDTO BuildRecreateRequest(
        OrderSubmissionSnapshot submission,
        bool isAdditional,
        byte[]? rowVersion) => new()
        {
            Description = submission.Description,
            SupplementReason = isAdditional ? submission.SupplementReason?.Trim() : null,
            RowVersion = rowVersion,
            IdempotencyKey = submission.IdempotencyKey,
            Items = BuildItems(submission.Items)
        };

    private static List<VppRequestDetailItemReqDTO> BuildItems(
        IEnumerable<OrderSubmissionItem> items) =>
        items.Select(item => new VppRequestDetailItemReqDTO
        {
            VppId = item.VppId,
            Qty = item.Quantity,
            Description = item.Description
        }).ToList();
}
