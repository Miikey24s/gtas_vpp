using System.Globalization;

namespace gtas_vpp_shared.Constants;

/// <summary>
/// Stable status-to-resource and status-to-text semantics shared by API contracts and clients.
/// Visual styling belongs to the consuming UI.
/// </summary>
public static class VppStatusContract
{
    private const string SubmittedPeriodClosedEn = "Submitted (Period Closed)";
    private const string SubmittedPeriodClosedVi = "Đã gửi (đã khóa kỳ)";

    public static string GetResourceKey(
        int status,
        bool isDeadlinePassed = false,
        bool isAdditionalOrder = false) =>
        status == 1 && isDeadlinePassed && !isAdditionalOrder
            ? "SubmittedPeriodClosed"
            : status switch
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
        "SubmittedPeriodClosed" => SubmittedPeriodClosedEn,
        "Submitted" => "Submitted",
        "Cancelled" => "Cancelled",
        "Pending" => "Pending",
        "Approved" => "Approved",
        "Rejected" => "Rejected",
        _ => "-"
    };

    private static string GetVietnameseText(string resourceKey) => resourceKey switch
    {
        "SubmittedPeriodClosed" => SubmittedPeriodClosedVi,
        "Submitted" => "Đã gửi",
        "Cancelled" => "Đã hủy",
        "Pending" => "Chờ duyệt",
        "Approved" => "Đã duyệt",
        "Rejected" => "Từ chối",
        _ => "-"
    };
}
