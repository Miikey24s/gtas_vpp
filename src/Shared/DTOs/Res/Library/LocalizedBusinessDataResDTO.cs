using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library;

public abstract class LocalizedBusinessDataResDTO : BaseResDTO
{
    public string OriginalLanguageCode { get; set; } = "vi";
    public string? DisplayName { get; set; }
    public string? DisplayDescription { get; set; }
    public string ResolvedLanguageCode { get; set; } = "vi";
    public bool IsTranslationFallback { get; set; }
}
