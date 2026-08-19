namespace gtas_vpp_shared.Constants;

/// <summary>
/// Giới hạn số lượng dùng chung cho một mặt hàng trong từng đơn đặt hàng.
/// </summary>
public static class VppOrderQuantityLimits
{
    public const int Minimum = 1;
    public const int Default = 300;
    public const int Maximum = 300;
}
