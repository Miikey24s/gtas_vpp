namespace gtas_vpp_fe.Components.DesignSystem.Primitives;

/// <summary>
/// Các trạng thái nội dung dùng chung; route chịu trách nhiệm xác định khi nào trạng thái xuất hiện.
/// </summary>
public enum VppContentStateKind
{
    Empty,
    FilteredEmpty,
    Loading,
    Error,
    Denied,
    Disabled,
    Success,
    Warning
}
