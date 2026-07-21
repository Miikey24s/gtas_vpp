using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class BusinessDataTranslationUpsertReqDTO
{
    [Required, StringLength(250, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required, RegularExpression("^(Draft|Approved)$")]
    public string Status { get; set; } = "Draft";

    [Required, RegularExpression("^(Manual|Import|AiDraft)$")]
    public string Source { get; set; } = "Manual";
}
