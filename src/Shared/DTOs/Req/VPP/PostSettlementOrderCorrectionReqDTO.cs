namespace gtas_vpp_shared.DTOs.Req.VPP;

public sealed class PostSettlementOrderCorrectionCreateReqDTO
{
    public Guid RequestId { get; set; }
    public string Action { get; set; } = "Adjust";
    public string Reason { get; set; } = string.Empty;
    public string EmployeeNote { get; set; } = string.Empty;
    public byte[]? RequestRowVersion { get; set; }
    public List<PostSettlementOrderCorrectionItemReqDTO> Items { get; set; } = [];
}

public sealed class PostSettlementOrderCorrectionItemReqDTO
{
    public Guid VppId { get; set; }
    public int Qty { get; set; }
    public string? Description { get; set; }
}

public sealed class PostSettlementOrderCorrectionDecisionReqDTO
{
    public string? Reason { get; set; }
    public byte[]? RowVersion { get; set; }
}
