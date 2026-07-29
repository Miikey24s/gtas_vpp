namespace gtas_vpp_shared.DTOs.Res.Library;

/// <summary>
/// Kết quả kiểm tra quan hệ đang sử dụng trước thao tác vô hiệu hóa.
/// </summary>
public sealed class LibraryDependencyImpactResDTO
{
    public Guid RecordId { get; set; }
    public string TableCode { get; set; } = string.Empty;
    public string DependencyKind { get; set; } = string.Empty;
    public int ActiveReferenceCount { get; set; }
    public bool CanDeactivate { get; set; }
}
