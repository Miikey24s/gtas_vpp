using gtas_vpp_be.Model.Library;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_be.Service.Helpers;
using gtas_vpp_shared.DTOs.Req.Library;
using gtas_vpp_shared.DTOs.Res.Library;
using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_be.Service.Services;

public sealed class PriceAsOfResolver : IPriceAsOfResolver
{
    public const string CurrentCalculationVersion = PriceCalculationEngine.CurrentVersion;

    private readonly IUnitOfWork _unitOfWork;

    public PriceAsOfResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PriceResolutionResDTO> ResolveAsync(
        PriceResolutionReqDTO request,
        CancellationToken cancellationToken = default)
    {
        var asOfUtc = NormalizeUtc(request.PriceAsOfUtc);
        var result = NewResult(request, asOfUtc);

        if (request.VppId == Guid.Empty || request.Quantity <= 0)
        {
            return Block(result, PriceResolutionBlockerCode.InvalidRequest,
                "VPP item and a positive quantity are required.");
        }

        if (request.LockedPriceListId.HasValue)
        {
            return await ResolveLockedAsync(request, result, asOfUtc, cancellationToken);
        }

        var activeRows = await CandidateRows(request.VppId, request.SupplierId, asOfUtc)
            .ToListAsync(cancellationToken);

        if (activeRows.Count == 0)
        {
            var unresolvedLegacy = await HasUnresolvedLegacyRowsAsync(
                request.VppId, request.SupplierId, asOfUtc, cancellationToken);
            if (unresolvedLegacy)
            {
                return Block(result, PriceResolutionBlockerCode.IncompleteLegacyBackfill,
                    "Legacy price data has no unambiguous supplier ownership.");
            }

            var expired = await HasExpiredRowsAsync(request.VppId, request.SupplierId, asOfUtc, cancellationToken);
            return Block(result,
                expired ? PriceResolutionBlockerCode.ExpiredPrice : PriceResolutionBlockerCode.MissingPrice,
                expired ? "The matching price has expired or is not yet effective." : "No published price covers this item and time.");
        }

        var bestPriority = activeRows.Min(x => x.Priority);
        var bestRows = activeRows.Where(x => x.Priority == bestPriority).ToList();
        if (bestRows.Count != 1)
        {
            return Block(result, PriceResolutionBlockerCode.AmbiguousPrice,
                "More than one published price covers this item, supplier and time.");
        }

        return Complete(result, bestRows[0], request.Quantity);
    }

    private async Task<PriceResolutionResDTO> ResolveLockedAsync(
        PriceResolutionReqDTO request,
        PriceResolutionResDTO result,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        var locked = await _unitOfWork.VPPContext.Set<L07_PriceList>()
            .AsNoTracking()
            .Where(x => x.Id == request.LockedPriceListId!.Value && !x.IsDeleted)
            .Select(x => new
            {
                PriceList = x,
                SupplierName = x.Supplier == null ? null : x.Supplier.SupplierName
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (locked == null)
        {
            return Block(result, PriceResolutionBlockerCode.MissingPrice, "The locked price book does not exist.");
        }

        if (!locked.PriceList.SupplierId.HasValue)
        {
            return Block(result, PriceResolutionBlockerCode.IncompleteLegacyBackfill,
                "The locked legacy price book has no unambiguous supplier ownership.");
        }

        if (request.SupplierId.HasValue && locked.PriceList.SupplierId != request.SupplierId)
        {
            return Block(result, PriceResolutionBlockerCode.SupplierMismatch,
                "The locked price book belongs to a different supplier.");
        }

        if (locked.PriceList.Status != L07_PriceListStatus.Published
            || locked.PriceList.EffectiveFromUtc > asOfUtc
            || (locked.PriceList.EffectiveToUtc.HasValue && asOfUtc >= locked.PriceList.EffectiveToUtc.Value))
        {
            return Block(result, PriceResolutionBlockerCode.ExpiredPrice,
                "The locked price book is not published and effective at PriceAsOfUtc.");
        }

        var items = await _unitOfWork.VPPContext.Set<L06_VPPSupplierMapping>()
            .AsNoTracking()
            .Where(x => x.L07_PriceListId == locked.PriceList.Id
                        && x.L04_VPPId == request.VppId
                        && !x.IsDeleted)
            .Select(x => new PriceCandidate(
                locked.PriceList.Id,
                locked.PriceList.PriceListCode,
                locked.PriceList.Version,
                locked.PriceList.SupplierId!.Value,
                locked.SupplierName,
                locked.PriceList.CurrencyCode,
                x.Id,
                x.NetPrice,
                x.Price,
                x.VatRate,
                x.MinimumOrderQuantity,
                x.LeadTimeDays,
                x.SupplierSku,
                0))
            .ToListAsync(cancellationToken);

        return items.Count switch
        {
            0 => Block(result, PriceResolutionBlockerCode.MissingPrice,
                "The locked price book does not contain this item."),
            > 1 => Block(result, PriceResolutionBlockerCode.AmbiguousPrice,
                "The locked price book contains duplicate active rows for this item."),
            _ => Complete(result, items[0], request.Quantity)
        };
    }

    private IQueryable<PriceCandidate> CandidateRows(Guid vppId, Guid? supplierId, DateTime asOfUtc)
    {
        var books = _unitOfWork.VPPContext.Set<L07_PriceList>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                        && x.SupplierId.HasValue
                        && x.Status == L07_PriceListStatus.Published
                        && x.EffectiveFromUtc <= asOfUtc
                        && (!x.EffectiveToUtc.HasValue || asOfUtc < x.EffectiveToUtc.Value));

        if (supplierId.HasValue)
        {
            books = books.Where(x => x.SupplierId == supplierId.Value);
        }

        return from book in books
               join item in _unitOfWork.VPPContext.Set<L06_VPPSupplierMapping>().AsNoTracking()
                   on book.Id equals item.L07_PriceListId
               where item.L04_VPPId == vppId && !item.IsDeleted
               select new PriceCandidate(
                   book.Id,
                   book.PriceListCode,
                   book.Version,
                   book.SupplierId!.Value,
                   book.Supplier == null ? null : book.Supplier.SupplierName,
                   book.CurrencyCode,
                   item.Id,
                   item.NetPrice,
                   item.Price,
                   item.VatRate,
                   item.MinimumOrderQuantity,
                   item.LeadTimeDays,
                   item.SupplierSku,
                   book.ContractCode != null && book.ContractCode != string.Empty ? 1 : book.IsDefault ? 3 : 2);
    }

    private async Task<bool> HasUnresolvedLegacyRowsAsync(
        Guid vppId,
        Guid? supplierId,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        return await (from book in _unitOfWork.VPPContext.Set<L07_PriceList>().AsNoTracking()
                      join item in _unitOfWork.VPPContext.Set<L06_VPPSupplierMapping>().AsNoTracking()
                          on book.Id equals item.L07_PriceListId
                      where !book.IsDeleted
                            && !book.SupplierId.HasValue
                            && book.Status == L07_PriceListStatus.Published
                            && book.EffectiveFromUtc <= asOfUtc
                            && (!book.EffectiveToUtc.HasValue || asOfUtc < book.EffectiveToUtc.Value)
                            && !item.IsDeleted
                            && item.L04_VPPId == vppId
                            && (!supplierId.HasValue || item.L05_VPPSupplierId == supplierId.Value)
                      select item.Id).AnyAsync(cancellationToken);
    }

    private async Task<bool> HasExpiredRowsAsync(
        Guid vppId,
        Guid? supplierId,
        DateTime asOfUtc,
        CancellationToken cancellationToken)
    {
        var books = _unitOfWork.VPPContext.Set<L07_PriceList>()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.SupplierId.HasValue);
        if (supplierId.HasValue)
        {
            books = books.Where(x => x.SupplierId == supplierId.Value);
        }

        return await (from book in books
                      join item in _unitOfWork.VPPContext.Set<L06_VPPSupplierMapping>().AsNoTracking()
                          on book.Id equals item.L07_PriceListId
                      where item.L04_VPPId == vppId
                            && !item.IsDeleted
                            && (book.Status != L07_PriceListStatus.Published
                                || book.EffectiveFromUtc > asOfUtc
                                || (book.EffectiveToUtc.HasValue && asOfUtc >= book.EffectiveToUtc.Value))
                      select item.Id).AnyAsync(cancellationToken);
    }

    private static PriceResolutionResDTO Complete(
        PriceResolutionResDTO result,
        PriceCandidate candidate,
        decimal quantity)
    {
        if (candidate.MinimumOrderQuantity > 0 && quantity < candidate.MinimumOrderQuantity)
        {
            return Block(result, PriceResolutionBlockerCode.MinimumOrderQuantity,
                $"Minimum order quantity is {candidate.MinimumOrderQuantity}.");
        }

        var netUnitPrice = candidate.NetPrice == 0m && candidate.LegacyPrice != 0m
            ? candidate.LegacyPrice
            : candidate.NetPrice;
        var calculation = PriceCalculationEngine.CalculateLine(netUnitPrice, candidate.VatRate, quantity);

        result.IsResolved = true;
        result.BlockerCode = PriceResolutionBlockerCode.None;
        result.SupplierId = candidate.SupplierId;
        result.SupplierName = candidate.SupplierName;
        result.PriceListId = candidate.PriceListId;
        result.PriceListCode = candidate.PriceListCode;
        result.PriceListVersion = candidate.Version;
        result.PriceBookItemId = candidate.ItemId;
        result.CurrencyCode = candidate.CurrencyCode;
        result.NetUnitPrice = netUnitPrice;
        result.VatRate = candidate.VatRate;
        result.NetAmount = calculation.NetAmount;
        result.VatAmount = calculation.VatAmount;
        result.GrossAmount = calculation.GrossAmount;
        result.MinimumOrderQuantity = candidate.MinimumOrderQuantity;
        result.LeadTimeDays = candidate.LeadTimeDays;
        result.SupplierSku = candidate.SupplierSku;
        return result;
    }

    private static PriceResolutionResDTO NewResult(PriceResolutionReqDTO request, DateTime asOfUtc)
        => new()
        {
            PriceAsOfUtc = asOfUtc,
            VppId = request.VppId,
            SupplierId = request.SupplierId,
            Quantity = request.Quantity,
            CalculationVersion = CurrentCalculationVersion
        };

    private static PriceResolutionResDTO Block(
        PriceResolutionResDTO result,
        PriceResolutionBlockerCode code,
        string message)
    {
        result.IsResolved = false;
        result.BlockerCode = code;
        result.BlockerMessage = message;
        return result;
    }

    private static DateTime NormalizeUtc(DateTime value)
        => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private sealed record PriceCandidate(
        Guid PriceListId,
        string? PriceListCode,
        int Version,
        Guid SupplierId,
        string? SupplierName,
        string CurrencyCode,
        Guid ItemId,
        decimal NetPrice,
        decimal LegacyPrice,
        decimal VatRate,
        decimal MinimumOrderQuantity,
        int LeadTimeDays,
        string? SupplierSku,
        int Priority);
}
