using System.ComponentModel.DataAnnotations;

namespace gtas_vpp_shared.DTOs.Req.Library;

public sealed class BusinessDataOriginalLanguageUpdateReqDTO
{
    [Required, RegularExpression("^(vi|en)$")]
    public string LanguageCode { get; set; } = "vi";
}
