using gtas_vpp_fe.Helpers;
using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_shared.Enums;

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
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount,
    string TopItemName,
    int TopItemQuantity,
    IReadOnlyList<SettlementOrderStatusCount> StatusCounts,
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
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount,
    IReadOnlyList<SettlementOrderStatusCount> StatusCounts,
    SettlementOrderGroupStatus Status);

public sealed record SettlementGroupTotals(
    int OrderCount,
    int RegularOrderCount,
    int AdditionalOrderCount,
    int TotalLines,
    int TotalQuantity,
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount)
{
    public static SettlementGroupTotals Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);
}

public sealed record SettlementOrderStatusCount(
    int Status,
    int Count);

public sealed record SettlementItemFinancialValues(
    decimal NetUnitPrice,
    decimal VatRate,
    decimal NetAmount,
    decimal GrossAmount);

public sealed record SettlementItemTotals(
    int OrderCount,
    int TotalQuantity,
    decimal NetAmount,
    decimal GrossAmount)
{
    public static SettlementItemTotals Empty { get; } = new(0, 0, 0, 0);
}

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
        var search = VppSearchText.Normalize(filter.Search);
        return items
            .Where(item =>
                VppSearchText.MatchesAny(search, item.VppCode, item.VppName)
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
        SettlementOrderGroupFilter filter,
        IReadOnlyList<SettlementFinancialAllocationResDTO>? allocations = null,
        IReadOnlyList<AggregatedVppItemResDTO>? demandItems = null)
    {
        var search = VppSearchText.Normalize(filter.Search);
        return orders
            .Where(order => MatchesOrderGroupFilter(order, departments, filter, search))
            .GroupBy(
                order => DisplayDepartment(order.DepartmentCode),
                StringComparer.CurrentCultureIgnoreCase)
            .Select(group => CreateDepartmentRow(
                group.Key,
                group.ToList(),
                departments,
                allocations ?? [],
                demandItems ?? []))
            .OrderBy(row => row.DepartmentName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static List<SettlementRequesterRow> BuildRequesterRows(
        IEnumerable<VppRequestResDTO> orders,
        IReadOnlyList<DepartmentResDTO> departments,
        SettlementOrderGroupFilter filter,
        IReadOnlyList<SettlementFinancialAllocationResDTO>? allocations = null)
    {
        var search = VppSearchText.Normalize(filter.Search);
        return orders
            .Where(order => MatchesOrderGroupFilter(order, departments, filter, search))
            .GroupBy(GetRequesterGroupKey)
            .Select(group => CreateRequesterRow(group.ToList(), departments, allocations ?? []))
            .OrderBy(row => row.RequesterName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static SettlementGroupTotals SummarizeRows(IEnumerable<SettlementDepartmentRow> rows)
    {
        var materialized = rows.ToArray();
        return new SettlementGroupTotals(
            materialized.Sum(row => row.OrderCount),
            materialized.Sum(row => row.RegularOrderCount),
            materialized.Sum(row => row.AdditionalOrderCount),
            materialized.Sum(row => row.TotalLines),
            materialized.Sum(row => row.TotalQuantity),
            materialized.Sum(row => row.NetAmount),
            materialized.Sum(row => row.VatAmount),
            materialized.Sum(row => row.GrossAmount));
    }

    public static SettlementGroupTotals SummarizeRows(IEnumerable<SettlementRequesterRow> rows)
    {
        var materialized = rows.ToArray();
        return new SettlementGroupTotals(
            materialized.Sum(row => row.OrderCount),
            materialized.Sum(row => row.RegularOrderCount),
            materialized.Sum(row => row.AdditionalOrderCount),
            materialized.Sum(row => row.TotalLines),
            materialized.Sum(row => row.TotalQuantity),
            materialized.Sum(row => row.NetAmount),
            materialized.Sum(row => row.VatAmount),
            materialized.Sum(row => row.GrossAmount));
    }

    public static IReadOnlyList<SettlementOrderStatusCount> SummarizeStatuses(
        IEnumerable<SettlementDepartmentRow> rows) =>
        SummarizeStatuses(rows.SelectMany(row => row.StatusCounts));

    public static IReadOnlyList<SettlementOrderStatusCount> SummarizeStatuses(
        IEnumerable<SettlementRequesterRow> rows) =>
        SummarizeStatuses(rows.SelectMany(row => row.StatusCounts));

    public static IReadOnlyList<SettlementOrderStatusCount> BuildVisibleStatusCounts(
        IEnumerable<SettlementOrderStatusCount> statusCounts,
        int? selectedStatus)
    {
        var counts = statusCounts
            .GroupBy(item => item.Status)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Count));

        if (selectedStatus.HasValue)
        {
            return
            [
                new SettlementOrderStatusCount(
                    selectedStatus.Value,
                    counts.GetValueOrDefault(selectedStatus.Value))
            ];
        }

        var requiredStatuses = new[]
        {
            (int)VPPStatus.Submitted,
            (int)VPPStatus.Approved
        };

        return requiredStatuses
            .Concat(counts.Keys.Except(requiredStatuses).OrderBy(status => status))
            .Select(status => new SettlementOrderStatusCount(status, counts.GetValueOrDefault(status)))
            .ToArray();
    }

    public static SettlementItemTotals SummarizeItems(
        IEnumerable<AggregatedVppItemResDTO> rows,
        IReadOnlyDictionary<Guid, SettlementItemFinancialValues> financials)
    {
        var materialized = rows.ToArray();
        var orderCount = materialized
            .SelectMany(row => row.Breakdown)
            .Select(detail => detail.Code?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .Count();

        return new SettlementItemTotals(
            orderCount,
            materialized.Sum(row => row.TotalQty),
            materialized.Sum(row => financials.GetValueOrDefault(row.VppId)?.NetAmount ?? 0),
            materialized.Sum(row => financials.GetValueOrDefault(row.VppId)?.GrossAmount ?? 0));
    }

    private static bool MatchesOrderGroupFilter(
        VppRequestResDTO order,
        IReadOnlyList<DepartmentResDTO> departments,
        SettlementOrderGroupFilter filter,
        string search) =>
        VppSearchText.MatchesAny(
            search,
            order.VppCode,
            order.RequesterName,
            order.Description,
            order.DepartmentCode,
            GetDepartmentName(order.DepartmentCode, departments))
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
        IReadOnlyList<DepartmentResDTO> departments,
        IReadOnlyList<SettlementFinancialAllocationResDTO> allocations,
        IReadOnlyList<AggregatedVppItemResDTO> demandItems)
    {
        var financials = SumFinancials(departmentOrders, allocations);
        var topItem = GetTopItem(departmentOrders, demandItems);
        return new SettlementDepartmentRow(
            departmentCode,
            GetDepartmentName(departmentCode, departments),
            departmentOrders.Count,
            departmentOrders.Count(order => !order.IsAdditionalOrder),
            departmentOrders.Count(order => order.IsAdditionalOrder),
            departmentOrders.Sum(order => order.TotalLines),
            departmentOrders.Sum(order => order.TotalQty),
            departmentOrders.Sum(order => order.TotalAmount),
            financials.NetAmount,
            financials.VatAmount,
            financials.GrossAmount,
            topItem.Name,
            topItem.Quantity,
            GetStatusCounts(departmentOrders),
            ResolveGroupStatus(departmentOrders));
    }

    private static SettlementRequesterRow CreateRequesterRow(
        IReadOnlyList<VppRequestResDTO> requesterOrders,
        IReadOnlyList<DepartmentResDTO> departments,
        IReadOnlyList<SettlementFinancialAllocationResDTO> allocations)
    {
        var firstOrder = requesterOrders[0];
        var financials = SumFinancials(requesterOrders, allocations);
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
            financials.NetAmount,
            financials.VatAmount,
            financials.GrossAmount,
            GetStatusCounts(requesterOrders),
            ResolveGroupStatus(requesterOrders));
    }

    private static (decimal NetAmount, decimal VatAmount, decimal GrossAmount) SumFinancials(
        IReadOnlyList<VppRequestResDTO> orders,
        IReadOnlyList<SettlementFinancialAllocationResDTO> allocations)
    {
        var orderIds = orders.Select(order => order.Id).ToHashSet();
        var matching = allocations.Where(allocation => orderIds.Contains(allocation.RequestHeaderId)).ToArray();
        return (
            matching.Sum(allocation => allocation.NetAmount),
            matching.Sum(allocation => allocation.VatAmount),
            matching.Sum(allocation => allocation.NetAmount + allocation.VatAmount));
    }

    private static IReadOnlyList<SettlementOrderStatusCount> SummarizeStatuses(
        IEnumerable<SettlementOrderStatusCount> statusCounts) =>
        statusCounts
            .GroupBy(item => item.Status)
            .Select(group => new SettlementOrderStatusCount(group.Key, group.Sum(item => item.Count)))
            .OrderBy(item => item.Status)
            .ToArray();

    private static IReadOnlyList<SettlementOrderStatusCount> GetStatusCounts(
        IEnumerable<VppRequestResDTO> orders) =>
        orders
            .GroupBy(order => order.Status)
            .Select(group => new SettlementOrderStatusCount(group.Key, group.Count()))
            .OrderBy(item => item.Status)
            .ToArray();

    private static (string Name, int Quantity) GetTopItem(
        IReadOnlyList<VppRequestResDTO> orders,
        IReadOnlyList<AggregatedVppItemResDTO> demandItems)
    {
        var requestCodes = orders
            .Select(order => order.VppCode?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);

        var topDemandItem = demandItems
            .Select(item => new
            {
                Name = string.IsNullOrWhiteSpace(item.VppName) ? DisplayItem(item.VppCode) : item.VppName.Trim(),
                Quantity = item.Breakdown
                    .Where(detail => !string.IsNullOrWhiteSpace(detail.Code) && requestCodes.Contains(detail.Code.Trim()))
                    .Sum(detail => detail.Qty)
            })
            .Where(item => item.Quantity > 0)
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();
        if (topDemandItem is not null)
        {
            return (topDemandItem.Name, topDemandItem.Quantity);
        }

        var topOrderItem = orders
            .SelectMany(order => order.Items)
            .GroupBy(
                item => new
                {
                    item.VppId,
                    Name = string.IsNullOrWhiteSpace(item.VppName) ? DisplayItem(item.VppCode) : item.VppName.Trim()
                })
            .Select(group => new
            {
                group.Key.Name,
                Quantity = group.Sum(item => item.Qty)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .FirstOrDefault();

        return topOrderItem is null ? ("–", 0) : (topOrderItem.Name, topOrderItem.Quantity);
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

    private static string DisplayItem(string? value) =>
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
