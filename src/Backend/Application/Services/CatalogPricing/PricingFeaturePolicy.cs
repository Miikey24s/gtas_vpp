using gtas_vpp_be.Service.Exceptions;
using Microsoft.Extensions.Options;

namespace gtas_vpp_be.Service.Services;

public sealed class PricingFeatureOptions
{
    public const string SectionName = "Pricing";

    public bool CommercialTermsEnabled { get; set; }
}

public sealed record PricingCommercialTerms(
    decimal DiscountRate,
    decimal RebateAmount,
    decimal FeeAmount,
    decimal ShippingAmount);

// Tạm khóa điều khoản thương mại cho đến khi có module hợp đồng và vận chuyển đầy đủ.
public sealed class PricingFeaturePolicy
{
    public static PricingFeaturePolicy Disabled { get; } = new(new PricingFeatureOptions());

    private readonly PricingFeatureOptions _options;

    public PricingFeaturePolicy(IOptions<PricingFeatureOptions> options)
        : this(options.Value)
    {
    }

    private PricingFeaturePolicy(PricingFeatureOptions options)
    {
        _options = options;
    }

    public bool CommercialTermsEnabled => _options.CommercialTermsEnabled;

    public void EnsureCommercialTermsAllowed(
        string? contractCode,
        decimal discountRate,
        decimal rebateAmount,
        decimal feeAmount,
        decimal shippingAmount)
    {
        if (CommercialTermsEnabled)
        {
            PriceBookWorkflowService.ValidateCommercialTerms(
                discountRate, rebateAmount, feeAmount, shippingAmount);
            return;
        }

        if (!string.IsNullOrWhiteSpace(contractCode)
            || discountRate != 0m
            || rebateAmount != 0m
            || feeAmount != 0m
            || shippingAmount != 0m)
        {
            throw new BusinessException(
                "Điều khoản hợp đồng, chiết khấu và các khoản phí đang tạm tắt.");
        }
    }

    public PricingCommercialTerms Resolve(
        decimal discountRate,
        decimal rebateAmount,
        decimal feeAmount,
        decimal shippingAmount)
        => CommercialTermsEnabled
            ? new(discountRate, rebateAmount, feeAmount, shippingAmount)
            : new(0m, 0m, 0m, 0m);
}
