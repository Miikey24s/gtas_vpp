namespace gtas_vpp_be.Model.Library;

/// <summary>
/// Giới hạn được lưu trong schema cho số lượng một mặt hàng trên từng đơn.
/// </summary>
public static class VppItemQuantityLimits
{
    public const int Minimum = 1;
    public const int Default = 300;
    public const int Maximum = 300;
}
