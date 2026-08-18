using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Exceptions;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

// Quản lý vòng đời bảng giá và so sánh báo giá theo cùng một mốc thời gian.
// Publish/Expire ghi trạng thái trong transaction; Compare chỉ đọc và xếp hạng các lựa chọn hợp lệ.
public sealed class PriceBookWorkflowService : IPriceBookWorkflowService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPriceListService _priceListService;
    private readonly PricingFeaturePolicy _pricingPolicy;

    public PriceBookWorkflowService(
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IPriceListService priceListService,
        PricingFeaturePolicy? pricingPolicy = null)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _priceListService = priceListService;
        _pricingPolicy = pricingPolicy ?? PricingFeaturePolicy.Disabled;
    }

    public async Task<PriceListResDTO> PublishAsync(
        Guid id,
        PriceBookStatusReqDTO request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Publish chỉ nhận bảng giá nháp đã đủ nhà cung cấp, dòng giá và điều kiện thương mại.
        var reason = ValidateReason(request.Reason);
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var entity = await _unitOfWork.VPPContext.Set<PriceList>()
                .Include(x => x.SupplierProductMappings)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
                ?? throw new BusinessException("Price book not found.");

            if (entity.Status != PriceListStatus.Draft)
            {
                throw new BusinessException("Only a draft price book can be published.");
            }

            ValidateRowVersion(entity, request.RowVersion);
            await ValidateForPublishAsync(entity, cancellationToken);

            var nowUtc = PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);
            entity.Status = PriceListStatus.Published;
            entity.PublishedAtUtc = nowUtc;
            entity.PublishedByUserId = userId;
            entity.ExpiredAtUtc = null;
            entity.ExpiredByUserId = null;
            entity.StatusReason = reason;
            entity.UpdatedAtUtc = nowUtc;
            entity.UpdatedByUserId = userId;

            await _unitOfWork.CommitAsync();
            return (await _priceListService.GetByIdAsync(id))!;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<PriceListResDTO> ExpireAsync(
        Guid id,
        PriceBookStatusReqDTO request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        // Expire đóng hiệu lực tại một mốc cụ thể; không xóa dữ liệu để lịch sử chốt vẫn tra cứu được.
        var reason = ValidateReason(request.Reason);
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var entity = await _unitOfWork.VPPContext.Set<PriceList>()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken)
                ?? throw new BusinessException("Price book not found.");

            if (entity.Status != PriceListStatus.Published)
            {
                throw new BusinessException("Only a published price book can be expired.");
            }

            ValidateRowVersion(entity, request.RowVersion);
            var nowUtc = PeriodCalculator.NormalizeNowUtc(_dateTimeProvider.Now);
            var effectiveToUtc = NormalizeUtc(request.EffectiveToUtc ?? nowUtc);
            if (effectiveToUtc <= entity.EffectiveFromUtc)
            {
                throw new BusinessException("Expiry must be later than EffectiveFromUtc.");
            }

            entity.Status = PriceListStatus.Expired;
            entity.EffectiveToUtc = effectiveToUtc;
            entity.ExpiredAtUtc = nowUtc;
            entity.ExpiredByUserId = userId;
            entity.StatusReason = reason;
            entity.UpdatedAtUtc = nowUtc;
            entity.UpdatedByUserId = userId;

            await _unitOfWork.CommitAsync();
            return (await _priceListService.GetByIdAsync(id))!;
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<PriceBookComparisonResDTO> CompareAsync(
        PriceBookComparisonReqDTO request,
        CancellationToken cancellationToken = default)
    {
        // Compare lọc bảng giá đang hiệu lực, tính từng dòng rồi xếp hạng; không tự chọn hay ghi NCC.
        var items = NormalizeItems(request.Items);
        var asOfUtc = NormalizeUtc(request.PriceAsOfUtc);
        var itemIds = items.Keys.ToArray();

        var booksQuery = _unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                        && x.Status == PriceListStatus.Published
                        && x.SupplierId.HasValue
                        && x.EffectiveFromUtc <= asOfUtc
                        && (!x.EffectiveToUtc.HasValue || asOfUtc < x.EffectiveToUtc.Value));

        if (request.SupplierIds is { Count: > 0 })
        {
            var supplierIds = request.SupplierIds.Distinct().ToArray();
            booksQuery = booksQuery.Where(x => x.SupplierId.HasValue && supplierIds.Contains(x.SupplierId.Value));
        }

        var books = await booksQuery
            .Select(book => new
            {
                book.Id,
                book.PriceListCode,
                book.Version,
                SupplierId = book.SupplierId!.Value,
                SupplierName = book.Supplier == null ? null : book.Supplier.SupplierName,
                book.CurrencyCode,
                book.EffectiveFromUtc,
                book.EffectiveToUtc,
                book.DiscountRate,
                book.RebateAmount,
                book.FeeAmount,
                book.ShippingAmount,
                Items = book.SupplierProductMappings!
                    .Where(item => !item.IsDeleted && itemIds.Contains(item.VppItemId))
                    .Select(item => new
                    {
                        item.VppItemId,
                        item.NetPrice,
                        item.Price,
                        item.VatRate,
                        item.MinimumOrderQuantity,
                        item.LeadTimeDays
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var quotes = new List<PriceBookQuoteResDTO>(books.Count);
        foreach (var book in books)
        {
            var commercialTerms = _pricingPolicy.Resolve(
                book.DiscountRate, book.RebateAmount, book.FeeAmount, book.ShippingAmount);
            var quote = new PriceBookQuoteResDTO
            {
                PriceListId = book.Id,
                PriceListCode = book.PriceListCode,
                Version = book.Version,
                SupplierId = book.SupplierId,
                SupplierName = book.SupplierName,
                CurrencyCode = book.CurrencyCode,
                EffectiveFromUtc = book.EffectiveFromUtc,
                EffectiveToUtc = book.EffectiveToUtc,
                RequestedItemCount = items.Count,
                RebateAmount = commercialTerms.RebateAmount,
                FeeAmount = commercialTerms.FeeAmount,
                ShippingAmount = commercialTerms.ShippingAmount
            };

            foreach (var requested in items)
            {
                var matching = book.Items.Where(x => x.VppItemId == requested.Key).ToList();
                if (matching.Count == 0)
                {
                    quote.MissingVppIds.Add(requested.Key);
                    continue;
                }

                if (matching.Count > 1)
                {
                    quote.Blockers.Add($"AMBIGUOUS_ITEM:{requested.Key}");
                    continue;
                }

                var item = matching[0];
                if (item.MinimumOrderQuantity > 0 && requested.Value < item.MinimumOrderQuantity)
                {
                    quote.Blockers.Add($"MOQ:{requested.Key}:{item.MinimumOrderQuantity}");
                    continue;
                }

                var netPrice = item.NetPrice == 0m && item.Price != 0m ? item.Price : item.NetPrice;
                var calculation = PriceCalculationEngine.CalculateLine(netPrice, item.VatRate, requested.Value);
                quote.Lines.Add(new PriceBookQuoteLineResDTO
                {
                    VppId = requested.Key,
                    Quantity = requested.Value,
                    NetUnitPrice = netPrice,
                    VatRate = item.VatRate,
                    NetAmount = calculation.NetAmount,
                    VatAmount = calculation.VatAmount,
                    GrossAmount = calculation.GrossAmount
                });
                quote.Subtotal += calculation.NetAmount;
                quote.VatAmount += calculation.VatAmount;
                quote.CoveredItemCount++;
                quote.MaximumLeadTimeDays = Math.Max(quote.MaximumLeadTimeDays, item.LeadTimeDays);
            }

            quote.Subtotal = PriceCalculationEngine.RoundMoney(quote.Subtotal);
            quote.VatAmount = PriceCalculationEngine.RoundMoney(quote.VatAmount);
            var basket = PriceCalculationEngine.CalculateBasket(
                quote.Subtotal,
                quote.VatAmount,
                commercialTerms.DiscountRate,
                commercialTerms.RebateAmount,
                commercialTerms.FeeAmount,
                commercialTerms.ShippingAmount);
            quote.DiscountAmount = basket.DiscountAmount;
            quote.GrandTotal = basket.GrandTotal;
            quote.CoveragePercent = items.Count == 0
                ? 0m
                : decimal.Round(quote.CoveredItemCount * 100m / items.Count, 2, MidpointRounding.AwayFromZero);

            if (quote.MissingVppIds.Count > 0)
            {
                quote.Blockers.Add($"MISSING_ITEMS:{quote.MissingVppIds.Count}");
            }
            if (quote.GrandTotal < 0)
            {
                quote.Blockers.Add("COMMERCIAL_TERMS_EXCEED_SUBTOTAL");
            }
            if (!string.Equals(quote.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase))
            {
                quote.Blockers.Add("UNSUPPORTED_CURRENCY");
            }

            quote.IsEligible = quote.CoveredItemCount == items.Count && quote.Blockers.Count == 0;
            quotes.Add(quote);
        }

        var ordered = quotes
            .OrderByDescending(x => x.IsEligible)
            .ThenByDescending(x => x.CoveragePercent)
            .ThenBy(x => x.IsEligible ? x.GrandTotal : decimal.MaxValue)
            .ThenBy(x => x.MaximumLeadTimeDays)
            .ThenBy(x => x.SupplierName)
            .ThenBy(x => x.PriceListCode)
            .ThenBy(x => x.Version)
            .ThenBy(x => x.PriceListId)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].Rank = index + 1;
        }

        return new PriceBookComparisonResDTO
        {
            PriceAsOfUtc = asOfUtc,
            CalculationVersion = PriceCalculationEngine.CurrentVersion,
            RequestedItemCount = items.Count,
            Quotes = ordered
        };
    }

    private async Task ValidateForPublishAsync(PriceList entity, CancellationToken cancellationToken)
    {
        if (!entity.SupplierId.HasValue)
        {
            throw new BusinessException("Supplier ownership is required before publish.");
        }
        if (string.IsNullOrWhiteSpace(entity.PriceListCode) || string.IsNullOrWhiteSpace(entity.PriceListName))
        {
            throw new BusinessException("Price book code and name are required before publish.");
        }
        if (!string.Equals(entity.CurrencyCode, "VND", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("PRICE-002 currently supports VND price books only.");
        }
        if (_pricingPolicy.CommercialTermsEnabled)
        {
            ValidateCommercialTerms(entity.DiscountRate, entity.RebateAmount, entity.FeeAmount, entity.ShippingAmount);
        }

        var supplierActive = await _unitOfWork.VPPContext.Set<Supplier>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == entity.SupplierId.Value && !x.IsDeleted, cancellationToken);
        if (!supplierActive)
        {
            throw new BusinessException("Supplier is missing or inactive.");
        }

        var activeItems = entity.SupplierProductMappings?.Where(x => !x.IsDeleted).ToList() ?? [];
        if (activeItems.Count == 0)
        {
            throw new BusinessException("At least one active price-book item is required before publish.");
        }
        if (activeItems.Any(x => x.SupplierId != entity.SupplierId.Value))
        {
            throw new BusinessException("Every price-book item must belong to the price book supplier.");
        }
        if (activeItems.GroupBy(x => x.VppItemId).Any(group => group.Count() > 1))
        {
            throw new BusinessException("Duplicate active item rows must be resolved before publish.");
        }
        if (activeItems.Any(x => x.NetPrice < 0 || x.VatRate < 0 || x.VatRate > 100
                                 || x.MinimumOrderQuantity < 0 || x.LeadTimeDays < 0))
        {
            throw new BusinessException("Price, VAT, MOQ or lead-time values are invalid.");
        }

        var itemIds = activeItems.Select(x => x.VppItemId).Distinct().ToArray();
        var overlaps = await _unitOfWork.VPPContext.Set<PriceList>()
            .AsNoTracking()
            .AnyAsync(other => other.Id != entity.Id
                               && !other.IsDeleted
                               && other.Status == PriceListStatus.Published
                               && other.SupplierId == entity.SupplierId
                               && (other.EffectiveToUtc == null || entity.EffectiveFromUtc < other.EffectiveToUtc)
                               && (entity.EffectiveToUtc == null || other.EffectiveFromUtc < entity.EffectiveToUtc)
                               && other.SupplierProductMappings!.Any(item =>
                                   !item.IsDeleted && itemIds.Contains(item.VppItemId)),
                cancellationToken);
        if (overlaps)
        {
            throw new BusinessException("Published price books cannot overlap for the same supplier and item.");
        }
    }

    private void ValidateRowVersion(PriceList entity, byte[]? supplied)
    {
        if (supplied is not { Length: > 0 })
        {
            throw new BusinessException("RowVersion is required.");
        }
        if (entity.RowVersion is not { Length: > 0 } || !entity.RowVersion.SequenceEqual(supplied))
        {
            throw new ConflictException("The price book changed. Reload before retrying.");
        }

        _unitOfWork.VPPContext.Entry(entity).Property(x => x.RowVersion).OriginalValue = supplied;
    }

    private static Dictionary<Guid, decimal> NormalizeItems(IEnumerable<PriceBookComparisonItemReqDTO>? source)
    {
        var rows = source?.ToList() ?? [];
        if (rows.Count == 0 || rows.Count > 1000)
        {
            throw new BusinessException("Comparison requires between one and 1,000 item rows.");
        }
        if (rows.Any(x => x.VppId == Guid.Empty || x.Quantity <= 0))
        {
            throw new BusinessException("Every comparison item requires a VPP id and positive quantity.");
        }

        return rows
            .GroupBy(x => x.VppId)
            .ToDictionary(group => group.Key, group => group.Sum(x => x.Quantity));
    }

    private static string ValidateReason(string? reason)
    {
        var value = reason?.Trim();
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 5 or > 500)
        {
            throw new BusinessException("Reason must contain between 5 and 500 characters.");
        }
        return value;
    }

    internal static void ValidateCommercialTerms(
        decimal discountRate,
        decimal rebateAmount,
        decimal feeAmount,
        decimal shippingAmount)
    {
        if (discountRate < 0 || discountRate > 100)
        {
            throw new BusinessException("Discount rate must be between zero and 100.");
        }
        if (rebateAmount < 0 || feeAmount < 0 || shippingAmount < 0)
        {
            throw new BusinessException("Rebate, fee and shipping amounts cannot be negative.");
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
