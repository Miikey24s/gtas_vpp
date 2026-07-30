using System.Globalization;
using gtas_vpp_fe.Components.DesignSystem.Primitives;
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

    public static VppStatusTone GetTone(int status) => status switch
    {
        1 => VppStatusTone.Success,
        4 => VppStatusTone.Danger,
        6 => VppStatusTone.Warning,
        7 => VppStatusTone.Success,
        8 => VppStatusTone.Danger,
        _ => VppStatusTone.Neutral
    };
}
