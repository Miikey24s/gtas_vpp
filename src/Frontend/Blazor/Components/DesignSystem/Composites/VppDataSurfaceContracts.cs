namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Cách route cung cấp dữ liệu cho một data surface.
/// </summary>
public enum VppDataSourceMode
{
    Static,
    ServerPaging,
    ClientSnapshotVirtualized,
    ServerVirtualizedPrefetch
}

/// <summary>
/// Nhịp chiều cao của dòng dữ liệu; route chọn theo lượng nội dung thực tế.
/// </summary>
public enum VppDataDensity
{
    Compact,
    RichTwoLine
}

/// <summary>
/// Nội dung điều hướng/tóm tắt mà footer được phép hiển thị.
/// </summary>
public enum VppDataFooterMode
{
    Paged,
    Virtualized,
    Static,
    UnknownTotal
}

/// <summary>
/// Kiểu nội dung rút gọn trong cell để áp dụng typography và hướng neo phù hợp.
/// </summary>
public enum VppCellValueKind
{
    Text,
    Code,
    Note
}
