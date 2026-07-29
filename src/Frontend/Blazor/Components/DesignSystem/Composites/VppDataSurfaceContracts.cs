namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Cách route cung cấp dữ liệu cho một data surface.
/// </summary>
public enum VppDataSourceMode
{
    Static,
    ServerPaging,
    ClientSnapshotPaged,
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
/// Profile điều hướng dữ liệu đã được owner duyệt cho từng archetype màn hình.
/// </summary>
public enum VppPagingProfile
{
    SplitList,
    Collection,
    LargeWorkingSet,
    SmallStatic
}

public sealed record VppPagingProfileDefinition(
    int DefaultPageSize,
    IReadOnlyList<int> PageSizeOptions,
    int PagingThreshold);

public static class VppPagingProfiles
{
    public static VppPagingProfileDefinition SplitList { get; } = new(20, [10, 20, 50], 0);
    public static VppPagingProfileDefinition Collection { get; } = new(50, [20, 50, 100], 0);
    public static VppPagingProfileDefinition LargeWorkingSet { get; } = new(100, [25, 50, 100], 0);
    public static VppPagingProfileDefinition SmallStatic { get; } = new(100, [25, 50, 100], 100);

    public static VppPagingProfileDefinition Get(VppPagingProfile profile) => profile switch
    {
        VppPagingProfile.SplitList => SplitList,
        VppPagingProfile.Collection => Collection,
        VppPagingProfile.LargeWorkingSet => LargeWorkingSet,
        _ => SmallStatic
    };
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
