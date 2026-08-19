using gtas_vpp_be.Model.VPP;

namespace gtas_vpp_be.Service.Services;

public sealed record SettlementRequestExportInfo(
    string RequestCode,
    string RequestType,
    string RequesterName,
    string Status);

internal static class SettlementExportValueResolver
{
    // Snapshot cũ có thể đã lưu UomName bằng GUID khi navigation Uom chưa được nạp.
    // Ưu tiên text snapshot hợp lệ, sau đó mới dùng lookup hiện tại để sửa dữ liệu hiển thị legacy.
    public static string ResolveUnitName(
        SettlementItem item,
        IReadOnlyDictionary<Guid, string>? unitNames)
    {
        if (IsDisplayText(item.UomName))
        {
            return item.UomName.Trim();
        }

        if (unitNames is not null
            && unitNames.TryGetValue(item.UomId, out var lookupName)
            && !string.IsNullOrWhiteSpace(lookupName))
        {
            return lookupName.Trim();
        }

        return IsDisplayText(item.UomCode) ? item.UomCode.Trim() : "-";
    }

    private static bool IsDisplayText(string? value)
        => !string.IsNullOrWhiteSpace(value) && !Guid.TryParse(value.Trim(), out _);
}
