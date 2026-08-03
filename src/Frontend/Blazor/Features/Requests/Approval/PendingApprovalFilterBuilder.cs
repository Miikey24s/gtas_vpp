namespace gtas_vpp_fe.Features.Requests.Approval;

public static class PendingApprovalFilterBuilder
{
    public static string? Build(string searchText, string departmentCode)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = EscapeValue(searchText);
            filters.Add($"((VppCode != null && VppCode.ToLower().Contains(\"{search}\")) || "
                + $"(RequesterName != null && RequesterName.ToLower().Contains(\"{search}\")) || "
                + $"(DepartmentCode != null && DepartmentCode.ToLower().Contains(\"{search}\")) || "
                + $"(Description != null && Description.ToLower().Contains(\"{search}\")))");
        }

        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var department = EscapeValue(departmentCode);
            filters.Add($"(DepartmentCode != null && DepartmentCode.ToLower() == \"{department}\")");
        }

        return filters.Count == 0 ? null : string.Join(" && ", filters);
    }

    private static string EscapeValue(string value) => value
        .Trim()
        .ToLowerInvariant()
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal);
}
