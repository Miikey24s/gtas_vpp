using System.Globalization;
using gtas_vpp_shared.Constants;

namespace gtas_vpp_fe.Helpers;

/// <summary>
/// Phần trình bày frontend cho trạng thái VPP. Text contract và ngữ nghĩa resource vẫn dùng chung.
/// </summary>
public static class StatusDisplay
{
    public static string GetResourceKey(
        int status,
        bool isDeadlinePassed = false,
        bool isAdditionalOrder = false) =>
        VppStatusContract.GetResourceKey(status, isDeadlinePassed, isAdditionalOrder);

    public static string GetText(
        int status,
        bool isDeadlinePassed = false,
        bool isAdditionalOrder = false,
        CultureInfo? culture = null) =>
        VppStatusContract.GetText(status, isDeadlinePassed, isAdditionalOrder, culture);

    public static string GetCssClass(int status) => status switch
    {
        1 => "vpp-badge-submitted",
        4 => "vpp-badge-cancelled",
        6 => "vpp-badge-pending",
        7 => "vpp-badge-approved",
        8 => "vpp-badge-rejected",
        _ => "vpp-badge-default"
    };

    public static string GetBadgeStyleName(int status) => status switch
    {
        1 => "Success",
        4 => "Danger",
        6 => "Warning",
        7 => "Success",
        8 => "Danger",
        _ => "Light"
    };
}
