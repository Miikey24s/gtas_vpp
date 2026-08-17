using gtas_vpp_shared.DTOs.Res.Library;
using gtas_vpp_shared.DTOs.Res.VPP;
using gtas_vpp_be.Service.Domain;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Đề xuất tối đa hai nhà cung cấp theo giá sau VAT của từng mặt hàng.
/// Không tính phí vận chuyển hay điều khoản hợp đồng vì chưa có dữ liệu đủ tin cậy.
/// </summary>
public static class SettlementSupplierOptimizer
{
    public const decimal MinimumSavingsPercent = 2m;
    private const string SuggestedReason = "Giá sau VAT thấp hơn theo đề xuất của hệ thống.";

    public static SettlementSupplierRecommendationResDTO? Build(
        IEnumerable<PriceBookQuoteResDTO> source,
        int requestedItemCount)
    {
        if (requestedItemCount <= 0)
        {
            return null;
        }

        var quotes = source
            .Where(IsComparable)
            .Select(quote => new { Quote = quote, Lines = BuildLineMap(quote) })
            .Where(candidate => candidate.Lines is not null)
            .Select(candidate => new Candidate(candidate.Quote, candidate.Lines!))
            .ToArray();
        if (quotes.Length < 2)
        {
            return null;
        }

        var bestSingle = quotes
            .Where(candidate => candidate.Quote.IsEligible
                && candidate.Lines.Count == requestedItemCount)
            .OrderBy(candidate => Total(candidate.Lines.Values))
            .ThenBy(candidate => candidate.Quote.Rank)
            .ThenBy(candidate => candidate.Quote.PriceListId)
            .FirstOrDefault();

        PairPlan? bestPair = null;
        for (var leftIndex = 0; leftIndex < quotes.Length - 1; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < quotes.Length; rightIndex++)
            {
                var left = quotes[leftIndex];
                var right = quotes[rightIndex];
                if (left.Quote.SupplierId == right.Quote.SupplierId)
                {
                    continue;
                }

                var plan = BuildPair(left, right, requestedItemCount);
                if (plan is null || (bestPair is not null && Compare(plan, bestPair) >= 0))
                {
                    continue;
                }

                bestPair = plan;
            }
        }

        if (bestPair is null)
        {
            return null;
        }

        var baselineTotal = bestSingle is null ? (decimal?)null : Total(bestSingle.Lines.Values);
        var savingsAmount = baselineTotal.HasValue
            ? PriceCalculationEngine.RoundMoney(baselineTotal.Value - bestPair.Total)
            : 0m;
        var savingsPercent = baselineTotal is > 0m
            ? decimal.Round(savingsAmount * 100m / baselineTotal.Value, 2, MidpointRounding.AwayFromZero)
            : 0m;
        var isCoverageRecommendation = !baselineTotal.HasValue;
        var isRecommended = isCoverageRecommendation
            || (savingsAmount > 0m && savingsPercent >= MinimumSavingsPercent);

        var primary = ResolvePrimary(bestPair, bestSingle);
        var secondary = ReferenceEquals(primary, bestPair.Left) ? bestPair.Right : bestPair.Left;
        var primaryAssignments = bestPair.Assignments.Where(item => ReferenceEquals(item.Supplier, primary)).ToArray();
        var secondaryAssignments = bestPair.Assignments.Where(item => ReferenceEquals(item.Supplier, secondary)).ToArray();

        return new SettlementSupplierRecommendationResDTO
        {
            IsAvailable = true,
            IsRecommended = isRecommended,
            ReasonCode = isCoverageRecommendation ? "FULL_COVERAGE" : isRecommended ? "COST_SAVING" : "BELOW_THRESHOLD",
            MinimumSavingsPercent = MinimumSavingsPercent,
            BaselineGrandTotal = baselineTotal,
            BaselineSupplierId = bestSingle?.Quote.SupplierId,
            BaselinePriceListId = bestSingle?.Quote.PriceListId,
            RecommendedGrandTotal = bestPair.Total,
            SavingsAmount = Math.Max(0m, savingsAmount),
            SavingsPercent = Math.Max(0m, savingsPercent),
            PrimarySupplierId = primary.Quote.SupplierId,
            PrimaryPriceListId = primary.Quote.PriceListId,
            Suppliers =
            [
                ToAllocation(primary, primaryAssignments, isPrimary: true),
                ToAllocation(secondary, secondaryAssignments, isPrimary: false)
            ],
            SuggestedExceptions = secondaryAssignments
                .OrderBy(item => item.VppId)
                .Select(item => new SettlementSupplierExceptionSuggestionResDTO
                {
                    VppId = item.VppId,
                    SupplierId = secondary.Quote.SupplierId,
                    PriceListId = secondary.Quote.PriceListId,
                    Reason = SuggestedReason
                })
                .ToList()
        };
    }

    private static bool IsComparable(PriceBookQuoteResDTO quote)
        => string.Equals(quote.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase)
            && quote.DiscountAmount == 0m
            && quote.RebateAmount == 0m
            && quote.FeeAmount == 0m
            && quote.ShippingAmount == 0m
            && quote.Blockers.All(blocker => blocker.StartsWith("MISSING_ITEMS:", StringComparison.Ordinal));

    private static Dictionary<Guid, PriceBookQuoteLineResDTO>? BuildLineMap(PriceBookQuoteResDTO quote)
    {
        var groups = quote.Lines.GroupBy(line => line.VppId).ToArray();
        return groups.Any(group => group.Count() != 1)
            ? null
            : groups.ToDictionary(group => group.Key, group => group.Single());
    }

    private static PairPlan? BuildPair(Candidate left, Candidate right, int requestedItemCount)
    {
        var itemIds = left.Lines.Keys.Union(right.Lines.Keys).OrderBy(id => id).ToArray();
        if (itemIds.Length != requestedItemCount)
        {
            return null;
        }

        var assignments = new List<Assignment>(itemIds.Length);
        foreach (var itemId in itemIds)
        {
            left.Lines.TryGetValue(itemId, out var leftLine);
            right.Lines.TryGetValue(itemId, out var rightLine);
            if (leftLine is null && rightLine is null)
            {
                return null;
            }

            if (rightLine is null
                || (leftLine is not null && leftLine.GrossAmount <= rightLine.GrossAmount))
            {
                assignments.Add(new Assignment(itemId, left, leftLine!));
            }
            else
            {
                assignments.Add(new Assignment(itemId, right, rightLine));
            }
        }

        if (assignments.All(item => ReferenceEquals(item.Supplier, left))
            || assignments.All(item => ReferenceEquals(item.Supplier, right)))
        {
            return null;
        }

        return new PairPlan(
            left,
            right,
            assignments,
            PriceCalculationEngine.RoundMoney(assignments.Sum(item => item.Line.GrossAmount)));
    }

    private static Candidate ResolvePrimary(PairPlan plan, Candidate? bestSingle)
    {
        if (bestSingle is not null)
        {
            if (bestSingle.Quote.PriceListId == plan.Left.Quote.PriceListId)
            {
                return plan.Left;
            }
            if (bestSingle.Quote.PriceListId == plan.Right.Quote.PriceListId)
            {
                return plan.Right;
            }
        }

        return plan.Assignments
            .GroupBy(item => item.Supplier)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Sum(item => item.Line.GrossAmount))
            .ThenBy(group => group.Key.Quote.Rank)
            .Select(group => group.Key)
            .First();
    }

    private static SettlementSupplierAllocationResDTO ToAllocation(
        Candidate candidate,
        IReadOnlyCollection<Assignment> assignments,
        bool isPrimary)
        => new()
        {
            SupplierId = candidate.Quote.SupplierId,
            SupplierName = candidate.Quote.SupplierName ?? "–",
            PriceListId = candidate.Quote.PriceListId,
            PriceListCode = candidate.Quote.PriceListCode ?? "–",
            IsPrimary = isPrimary,
            ItemCount = assignments.Count,
            GrossAmount = PriceCalculationEngine.RoundMoney(assignments.Sum(item => item.Line.GrossAmount))
        };

    private static decimal Total(IEnumerable<PriceBookQuoteLineResDTO> lines)
        => PriceCalculationEngine.RoundMoney(lines.Sum(line => line.GrossAmount));

    private static int Compare(PairPlan left, PairPlan right)
    {
        var total = left.Total.CompareTo(right.Total);
        if (total != 0)
        {
            return total;
        }

        var leftKey = $"{left.Left.Quote.PriceListId:N}:{left.Right.Quote.PriceListId:N}";
        var rightKey = $"{right.Left.Quote.PriceListId:N}:{right.Right.Quote.PriceListId:N}";
        return string.CompareOrdinal(leftKey, rightKey);
    }

    private sealed record Candidate(
        PriceBookQuoteResDTO Quote,
        Dictionary<Guid, PriceBookQuoteLineResDTO> Lines);

    private sealed record Assignment(
        Guid VppId,
        Candidate Supplier,
        PriceBookQuoteLineResDTO Line);

    private sealed record PairPlan(
        Candidate Left,
        Candidate Right,
        IReadOnlyList<Assignment> Assignments,
        decimal Total);
}
