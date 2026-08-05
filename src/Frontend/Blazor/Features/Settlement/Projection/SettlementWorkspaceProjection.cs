using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Settlement.Projection;

public sealed record SettlementDepartmentFilter(
    string Search,
    string OrderType,
    int? Status,
    string DepartmentCode);

public sealed record SettlementItemFilter(
    string Search,
    string Category,
    string Unit);

public enum SettlementDepartmentStatus
{
    Pending,
    Approved,
    NeedsReview,
    Submitted
}

public sealed record SettlementDepartmentOption(string Code, string Name);

public sealed record SettlementDepartmentRow(
    string DepartmentCode,
    string DepartmentName,
    string RequesterNames,
    int OrderCount,
    int RegularOrderCount,
    int AdditionalOrderCount,
    int TotalLines,
    int TotalQuantity,
    long TotalAmount,
    SettlementDepartmentStatus Status);

public static class SettlementWorkspaceProjection
{
    public static IReadOnlyList<SettlementDepartmentOption> BuildDepartmentOptions(
        IEnumerable<VppRequestResDTO> orders,
        IReadOnlyList<DepartmentResDTO> departments) =>
        orders
            .Select(order => order.DepartmentCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Select(code => new SettlementDepartmentOption(
                code,
                GetDepartmentName(code, departments)))
            .OrderBy(option => option.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    public static IReadOnlyList<string> GetDistinctItemCategories(
        IEnumerable<AggregatedVppItemResDTO> items) =>
        GetDistinctValues(items.Select(item => item.CategoryName));

    public static IReadOnlyList<string> GetDistinctItemUnits(
        IEnumerable<AggregatedVppItemResDTO> items) =>
        GetDistinctValues(items.Select(item => item.UomName));

    public static List<AggregatedVppItemResDTO> FilterItems(
        IEnumerable<AggregatedVppItemResDTO> items,
        SettlementItemFilter filter)
    {
        var search = filter.Search.Trim();
        return items
            .Where(item =>
                (string.IsNullOrWhiteSpace(search)
                 || (item.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
                 || (item.VppName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false))
                && (string.IsNullOrWhiteSpace(filter.Category)
                    || string.Equals(
                        item.CategoryName,
                        filter.Category,
                        StringComparison.CurrentCultureIgnoreCase))
                && (string.IsNullOrWhiteSpace(filter.Unit)
                    || string.Equals(
                        item.UomName,
                        filter.Unit,
                        StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
    }

    public static List<SettlementDepartmentRow> BuildDepartmentRows(
        IEnumerable<VppRequestResDTO> orders,
        IReadOnlyList<DepartmentResDTO> departments,
        SettlementDepartmentFilter filter)
    {
        var search = filter.Search.Trim();
        return orders
            .Where(order => MatchesDepartmentFilter(order, departments, filter, search))
            .GroupBy(
                order => DisplayDepartment(order.DepartmentCode),
                StringComparer.CurrentCultureIgnoreCase)
            .Select(group => CreateDepartmentRow(group.Key, group.ToList(), departments))
            .OrderBy(row => row.DepartmentName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool MatchesDepartmentFilter(
        VppRequestResDTO order,
        IReadOnlyList<DepartmentResDTO> departments,
        SettlementDepartmentFilter filter,
        string search) =>
        (string.IsNullOrWhiteSpace(search)
         || (order.VppCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
         || (order.RequesterName?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
         || (order.Description?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
         || (order.DepartmentCode?.Contains(search, StringComparison.CurrentCultureIgnoreCase) ?? false)
         || GetDepartmentName(order.DepartmentCode, departments)
             .Contains(search, StringComparison.CurrentCultureIgnoreCase))
        && (filter.OrderType switch
        {
            "regular" => !order.IsAdditionalOrder,
            "additional" => order.IsAdditionalOrder,
            _ => true
        })
        && (!filter.Status.HasValue || order.Status == filter.Status.Value)
        && (string.IsNullOrWhiteSpace(filter.DepartmentCode)
            || string.Equals(
                order.DepartmentCode,
                filter.DepartmentCode,
                StringComparison.CurrentCultureIgnoreCase));

    private static SettlementDepartmentRow CreateDepartmentRow(
        string departmentCode,
        IReadOnlyList<VppRequestResDTO> departmentOrders,
        IReadOnlyList<DepartmentResDTO> departments)
    {
        var status = departmentOrders.Any(order => order.Status == 6)
            ? SettlementDepartmentStatus.Pending
            : departmentOrders.All(order => order.Status == 7)
                ? SettlementDepartmentStatus.Approved
                : departmentOrders.Any(order => order.Status is 4 or 8)
                    ? SettlementDepartmentStatus.NeedsReview
                    : SettlementDepartmentStatus.Submitted;

        return new SettlementDepartmentRow(
            departmentCode,
            GetDepartmentName(departmentCode, departments),
            GetRequesterNames(departmentOrders),
            departmentOrders.Count,
            departmentOrders.Count(order => !order.IsAdditionalOrder),
            departmentOrders.Count(order => order.IsAdditionalOrder),
            departmentOrders.Sum(order => order.TotalLines),
            departmentOrders.Sum(order => order.TotalQty),
            departmentOrders.Sum(order => order.TotalAmount),
            status);
    }

    private static string GetRequesterNames(IEnumerable<VppRequestResDTO> orders)
    {
        var names = orders
            .Select(order => order.RequesterName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase);

        var result = string.Join(", ", names);
        return string.IsNullOrWhiteSpace(result) ? "–" : result;
    }

    private static IReadOnlyList<string> GetDistinctValues(IEnumerable<string?> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

    private static string DisplayDepartment(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "–" : value;

    private static string GetDepartmentName(
        string? code,
        IReadOnlyList<DepartmentResDTO> departments)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "–";
        }

        return departments.FirstOrDefault(department =>
                   string.Equals(
                       department.Code,
                       code,
                       StringComparison.CurrentCultureIgnoreCase))?.Name
               ?? code;
    }
}
