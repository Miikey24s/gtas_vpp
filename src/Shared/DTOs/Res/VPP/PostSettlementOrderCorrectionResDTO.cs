namespace gtas_vpp_shared.DTOs.Res.VPP;

public sealed class PostSettlementOrderCorrectionResDTO
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid RequestId { get; set; }
    public Guid RequestSeriesId { get; set; }
    public int RequestRevisionNumber { get; set; }
    public string RequestCode { get; set; } = string.Empty;
    public int RequestOwnerUserId { get; set; }
    public Guid SettlementId { get; set; }
    public int SettlementRevisionNumber { get; set; }
    public string MemberCompanyCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string EmployeeNote { get; set; } = string.Empty;
    public string? DecisionReason { get; set; }
    public int RequestedByUserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public Guid? ResultRequestId { get; set; }
    public Guid? ResultSettlementId { get; set; }
    public int ChangedItemCount { get; set; }
    public int RemovedItemCount { get; set; }
    public IReadOnlyList<Guid> RemovedItemIds { get; set; } = [];
    public byte[]? RowVersion { get; set; }
    public IReadOnlyList<PostSettlementOrderCorrectionItemResDTO> Items { get; set; } = [];
}

public sealed class PostSettlementOrderCorrectionItemResDTO
{
    public Guid VppId { get; set; }
    public int Qty { get; set; }
    public string? Description { get; set; }
}
