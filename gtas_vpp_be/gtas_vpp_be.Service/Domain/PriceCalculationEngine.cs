namespace gtas_vpp_be.Service.Domain;

public static class PriceCalculationEngine
{
    public const string CurrentVersion = "price-vat-v1";

    public static PriceLineCalculation CalculateLine(decimal netUnitPrice, decimal vatRate, decimal quantity)
    {
        var netAmount = RoundMoney(netUnitPrice * quantity);
        var vatAmount = RoundMoney(netAmount * vatRate / 100m);
        return new PriceLineCalculation(netAmount, vatAmount, netAmount + vatAmount);
    }

    public static PriceBasketCalculation CalculateBasket(
        decimal subtotal,
        decimal vatAmount,
        decimal discountRate,
        decimal rebateAmount,
        decimal feeAmount,
        decimal shippingAmount)
    {
        var discountAmount = RoundMoney(subtotal * discountRate / 100m);
        var total = RoundMoney(subtotal - discountAmount - rebateAmount + feeAmount + shippingAmount + vatAmount);
        return new PriceBasketCalculation(discountAmount, total);
    }

    public static decimal RoundMoney(decimal value)
        => decimal.Round(value, 4, MidpointRounding.AwayFromZero);
}

public readonly record struct PriceLineCalculation(decimal NetAmount, decimal VatAmount, decimal GrossAmount);
public readonly record struct PriceBasketCalculation(decimal DiscountAmount, decimal GrandTotal);
