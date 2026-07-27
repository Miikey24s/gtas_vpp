namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Primitive nhỏ theo nguyên tắc response mới nhất thắng cho page tương tác.
/// Ngăn request chậm hơn render dữ liệu cũ sau khi user đổi bộ lọc.
/// </summary>
public sealed class AsyncLoadVersion
{
    private int _version;

    public int Begin() => Interlocked.Increment(ref _version);

    public bool IsCurrent(int version) => Volatile.Read(ref _version) == version;
}
