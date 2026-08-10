using System.Globalization;

namespace gtas_vpp_shared.Constants;

/// <summary>
/// Ngữ nghĩa ổn định ánh xạ trạng thái sang resource và text, dùng chung cho API contract và client.
/// Phần trình bày trực quan thuộc trách nhiệm của UI sử dụng contract.
/// </summary>
public static class VppStatusContract
{
    public static string GetResourceKey(
        int status,
        bool isDeadlinePassed = false,
        bool isAdditionalOrder = false) =>
        status switch
        {
            1 => "Submitted",
            4 => "Cancelled",
            6 => "Pending",
            7 => "Approved",
            8 => "Rejected",
            _ => "StatusUnknown"
        };

    public static string GetText(int status) => GetEnglishText(GetResourceKey(status));

    public static string GetText(
        int status,
        bool isDeadlinePassed = false,
        bool isAdditionalOrder = false,
        CultureInfo? culture = null)
    {
        var resourceKey = GetResourceKey(status, isDeadlinePassed, isAdditionalOrder);
        var effectiveCulture = culture ?? CultureInfo.CurrentUICulture;

        return string.Equals(
            effectiveCulture.TwoLetterISOLanguageName,
            "vi",
            StringComparison.OrdinalIgnoreCase)
            ? GetVietnameseText(resourceKey)
            : GetEnglishText(resourceKey);
    }

    private static string GetEnglishText(string resourceKey) => resourceKey switch
    {
        "Submitted" => "Submitted",
        "Cancelled" => "Cancelled",
        "Pending" => "Pending",
        "Approved" => "Approved",
        "Rejected" => "Rejected",
        _ => "-"
    };

    private static string GetVietnameseText(string resourceKey) => resourceKey switch
    {
        "Submitted" => "Đã gửi",
        "Cancelled" => "Đã hủy",
        "Pending" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "Rejected" => "Từ chối",
        _ => "-"
    };
}
