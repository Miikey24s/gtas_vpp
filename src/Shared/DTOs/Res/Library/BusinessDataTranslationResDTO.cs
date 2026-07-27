namespace gtas_vpp_shared.DTOs.Res.Library;

public sealed class BusinessDataTranslationResDTO
{
    public Guid Id { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public int UpdatedByUserId { get; set; }
}

public sealed class BusinessDataTranslationBundleResDTO
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string OriginalLanguageCode { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string? OriginalDescription { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? DisplayDescription { get; set; }
    public string ResolvedLanguageCode { get; set; } = string.Empty;
    public bool IsFallback { get; set; }
    public IReadOnlyList<BusinessDataTranslationResDTO> Translations { get; set; } = [];
}
