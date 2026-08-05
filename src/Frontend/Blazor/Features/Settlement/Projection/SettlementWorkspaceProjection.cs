using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Settlement.Projection;

public sealed record SettlementOrderGroupFilter(
    string Search,
    string OrderType,
    int? Status,
    string DepartmentCode);

public sealed record SettlementItemFilter(
    string Search,
    string Category,
    string Unit);

public enum SettlementOrderGroupStatus
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
    int OrderCount,
    int RegularOrderCount,
    int AdditionalOrderCount,
    int TotalLines,
    int TotalQuantity,
    long TotalAmount,
    SettlementOrderGroupStatus Status);

public sealed record SettlementRequesterRow(
    int UserId,
    string RequesterName,
    string DepartmentSummary,
    int OrderCount,
    int RegularOrderCount,
    int AdditionalOrderCount,
    int TotalLines,
    int TotalQuantity,
    long TotalAmount,
    SettlementOrderGroupStatus Status);

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
        SettlementOrderGroupFilter filter)
    {
        var search = filter.Search.Trim();
        return orders
            .Where(order => MatchesOrderGroupFilter(order, departments, filter, search))
            .GroupBy(
                order => DisplayDepartment(order.DepartmentCode),
                StringComparer.CurrentCultureIgnoreCase)
            .Select(group => CreateDepartmentRow(group.Key, group.ToList(), departments))
            .OrderBy(row => row.DepartmentName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static List<SettlementRequesterRow> BuildRequesterRows(
        IEnumerable<VppRequestResDTO> orders,
        IReadOnlyList<DepartmentResDTO> departments,
        SettlementOrderGroupFilter filter)
    {
        var search = filter.Search.Trim();
        return orders
            .Where(order => MatchesOrderGroupFilter(order, departments, filter, search))
            .GroupBy(GetRequesterGroupKey)
            .Select(group => CreateRequesterRow(group.ToList(), departments))
            .OrderBy(row => row.RequesterName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static bool MatchesOrderGroupFilter(
        VppRequestResDTO order,
        IReadOnlyList<DepartmentResDTO> departments,
        SettlementOrderGroupFilter filter,
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
        return new SettlementDepartmentRow(
            departmentCode,
            GetDepartmentName(departmentCode, departments),
            departmentOrders.Count,
            departmentOrders.Count(order => !order.IsAdditionalOrder),
            departmentOrders.Count(order => order.IsAdditionalOrder),
            departmentOrders.Sum(order => order.TotalLines),
            departmentOrders.Sum(order => order.TotalQty),
            departmentOrders.Sum(order => order.TotalAmount),
            ResolveGroupStatus(departmentOrders));
    }

    private static SettlementRequesterRow CreateRequesterRow(
        IReadOnlyList<VppRequestResDTO> requesterOrders,
        IReadOnlyList<DepartmentResDTO> departments)
    {
        var firstOrder = requesterOrders[0];
        return new SettlementRequesterRow(
            firstOrder.CreatedByUserId,
            GetRequesterName(requesterOrders),
            GetDepartmentSummary(requesterOrders, departments),
            requesterOrders.Count,
            requesterOrders.Count(order => !order.IsAdditionalOrder),
            requesterOrders.Count(order => order.IsAdditionalOrder),
            requesterOrders.Sum(order => order.TotalLines),
            requesterOrders.Sum(order => order.TotalQty),
            requesterOrders.Sum(order => order.TotalAmount),
            ResolveGroupStatus(requesterOrders));
    }

    private static SettlementOrderGroupStatus ResolveGroupStatus(IReadOnlyList<VppRequestResDTO> orders) =>
        orders.Any(order => order.Status == 6)
            ? SettlementOrderGroupStatus.Pending
            : orders.All(order => order.Status == 7)
                ? SettlementOrderGroupStatus.Approved
                : orders.Any(order => order.Status is 4 or 8)
                    ? SettlementOrderGroupStatus.NeedsReview
                    : SettlementOrderGroupStatus.Submitted;

    private static string GetRequesterGroupKey(VppRequestResDTO order) =>
        order.CreatedByUserId > 0
            ? $"id:{order.CreatedByUserId}"
            : $"name:{DisplayRequester(order.RequesterName).ToUpperInvariant()}";

    private static string GetRequesterName(IEnumerable<VppRequestResDTO> orders) =>
        orders
            .Select(order => DisplayRequester(order.RequesterName))
            .Where(name => name != "–")
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault() ?? "–";

    private static string GetDepartmentSummary(
        IEnumerable<VppRequestResDTO> orders,
        IReadOnlyList<DepartmentResDTO> departments)
    {
        var labels = orders
            .Select(order => DisplayDepartment(order.DepartmentCode))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(code => GetDepartmentName(code, departments), StringComparer.CurrentCultureIgnoreCase)
            .Select(code => $"{GetDepartmentName(code, departments)} · {code}");

        var result = string.Join(", ", labels);
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

    private static string DisplayRequester(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "–" : value.Trim();

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
