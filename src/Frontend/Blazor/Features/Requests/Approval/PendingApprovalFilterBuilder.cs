namespace gtas_vpp_fe.Features.Requests.Approval;

public static class PendingApprovalFilterBuilder
{
    public static string? Build(string departmentCode)
    {
        var filters = new List<string>();

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
