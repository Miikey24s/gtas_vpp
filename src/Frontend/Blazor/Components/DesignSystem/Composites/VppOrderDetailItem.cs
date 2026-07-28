namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Dữ liệu hiển thị của một dòng mặt hàng trong chi tiết đơn, tách khỏi DTO và nghiệp vụ của route.
/// </summary>
public sealed record VppOrderDetailItem(
    int Number,
    string? Code,
    string Name,
    string? CategoryName,
    string? UomName,
    int Quantity,
    string? Note);
