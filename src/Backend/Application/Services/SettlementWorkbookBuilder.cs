using System.Globalization;
using gtas_vpp_be.Model.VPP;

namespace gtas_vpp_be.Service.Services;

public static class SettlementWorkbookBuilder
{
    public static byte[] Build(Settlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        var culture = CultureInfo.GetCultureInfo("vi-VN");
        var summaryRows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "GTAS VPP — Period settlement", null },
            new object?[] { "Period", $"{settlement.Month:00}/{settlement.Year}" },
            new object?[] { "Revision", settlement.RevisionNumber },
            new object?[] { "Current revision", settlement.IsCurrentRevision ? "YES" : "NO" },
            new object?[] { "Correction", settlement.IsCorrection ? "YES" : "NO" },
            new object?[] { "Correction reason", settlement.CorrectionReason ?? "-" },
            new object?[] { "Primary supplier", settlement.PrimarySupplierName },
            new object?[] { "Price book", settlement.PriceListName },
            new object?[] { "Price book version", settlement.PriceListVersion },
            new object?[] { "Price as of", settlement.PriceAsOfUtc.ToString("HH:mm dd/MM/yyyy", culture) },
            new object?[] { "Currency", settlement.CurrencyCode },
            new object?[] { "Subtotal", settlement.Subtotal },
            new object?[] { "Discount", settlement.DiscountAmount },
            new object?[] { "Rebate", settlement.RebateAmount },
            new object?[] { "Fee", settlement.FeeAmount },
            new object?[] { "Shipping", settlement.ShippingAmount },
            new object?[] { "VAT", settlement.VatAmount },
            new object?[] { "Rounding", settlement.RoundingAdjustment },
            new object?[] { "Grand total", settlement.GrandTotal },
            new object?[] { "Confirmed at", settlement.ConfirmedAtUtc.ToString("HH:mm dd/MM/yyyy", culture) },
            new object?[] { "Confirmed by user", settlement.ConfirmedByUserId },
            new object?[] { "Calculation version", settlement.CalculationVersion },
            new object?[] { "Input hash", settlement.InputHash }
        };

        var itemRows = settlement.Items
            .OrderBy(item => item.VppName)
            .ThenBy(item => item.VppCode)
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.VppCode,
                item.VppName,
                item.UomName,
                item.Quantity,
                item.NetUnitPrice,
                item.VatRate,
                item.NetAmount,
                item.VatAmount,
                item.GrossAmount,
                item.SupplierSku,
                item.IsSupplierException ? "YES" : "NO",
                item.SupplierExceptionReason ?? "-"
            })
            .ToArray();

        var allocationRows = settlement.Allocations
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.RequesterUserId)
            .ThenBy(item => item.RequestDetailId)
            .Select((item, index) => (IReadOnlyList<object?>)new object?[]
            {
                index + 1,
                item.DepartmentCode,
                item.RequesterUserId,
                item.SettlementItem.VppCode,
                item.SettlementItem.VppName,
                item.Quantity,
                item.NetAmount,
                item.VatAmount,
                item.CommercialAdjustmentAmount,
                item.RoundingAdjustment,
                item.GrossAmount
            })
            .ToArray();

        return SimpleWorkbookBuilder.Build([
            new SimpleWorkbookSheet("Settlement", ["Field", "Value"], summaryRows),
            new SimpleWorkbookSheet(
                "Items",
                [
                    "#", "Item code", "Item name", "Unit", "Quantity", "Net unit price",
                    "VAT rate", "Net amount", "VAT amount", "Gross amount", "Supplier SKU",
                    "Supplier exception", "Exception reason"
                ],
                itemRows),
            new SimpleWorkbookSheet(
                "Allocations",
                [
                    "#", "Department", "Requester user", "Item code", "Item name", "Quantity",
                    "Net amount", "VAT amount", "Commercial adjustment", "Rounding", "Gross amount"
                ],
                allocationRows)
        ]);
    }
}
