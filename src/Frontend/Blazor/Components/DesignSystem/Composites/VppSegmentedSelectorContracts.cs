namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Ngữ nghĩa của selector ngang; visual giống nhau nhưng ARIA phải đúng mục đích sử dụng.
/// </summary>
/// <summary>
/// Một lựa chọn typed trong selector ngang dùng chung.
/// </summary>
public sealed record VppSegmentedOption<TValue>(
    TValue Value,
    string Label,
    string? Icon = null,
    string? Badge = null,
    int? Ordinal = null,
    bool IsEnabled = true,
    bool? IsExpanded = null);
