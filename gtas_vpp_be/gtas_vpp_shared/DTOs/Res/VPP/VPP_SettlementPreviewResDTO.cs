using gtas_vpp_shared.DTOs.Res.Library;

namespace gtas_vpp_shared.DTOs.Res.VPP;

public sealed class VPP_SettlementPreviewResDTO
{
    public int Y { get; set; }
    public int M { get; set; }
    public DateTime PriceAsOfUtc { get; set; }
    public string InputHash { get; set; } = string.Empty;
    public int RequestedItemCount { get; set; }
    public int RequestedLineCount { get; set; }
    public int PendingAdditionalCount { get; set; }
    public Guid? PrimarySupplierId { get; set; }
    public Guid? PrimaryPriceListId { get; set; }
    public int? PrimaryPriceListVersion { get; set; }
    public PriceBookQuoteResDTO? PrimaryQuote { get; set; }
    public List<PriceBookQuoteResDTO> Quotes { get; set; } = [];
    public List<string> Blockers { get; set; } = [];
    public List<VPP_SettlementExceptionResDTO> Exceptions { get; set; } = [];
}

public sealed class VPP_SettlementExceptionResDTO
{
    public Guid VppId { get; set; }
    public Guid SupplierId { get; set; }
    public string? Reason { get; set; }
    public bool IsValid { get; set; }
}
