using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Settlement.Submission;

public static class SettlementRequestFactory
{
    public static SettlementPreviewReqDTO BuildPreview(
        int year,
        int month,
        Guid? primarySupplierId,
        Guid? priceListId,
        DateTime priceAsOfUtc,
        IEnumerable<SettlementExceptionReqDTO> exceptions) => new()
        {
            Year = year,
            Month = month,
            PrimarySupplierId = primarySupplierId,
            PriceListId = priceListId,
            PriceAsOfUtc = priceAsOfUtc,
            Exceptions = CloneExceptions(exceptions)
        };

    public static SettlementConfirmReqDTO BuildConfirm(
        int year,
        int month,
        SettlementPreviewResDTO preview,
        string idempotencyKey,
        IEnumerable<SettlementExceptionReqDTO> exceptions) => new()
        {
            Year = year,
            Month = month,
            PriceAsOfUtc = preview.PriceAsOfUtc,
            InputHash = preview.InputHash,
            PrimarySupplierId = preview.PrimarySupplierId!.Value,
            PriceListId = preview.PrimaryPriceListId!.Value,
            IdempotencyKey = idempotencyKey,
            Exceptions = CloneExceptions(exceptions)
        };

    public static SettlementCorrectionReqDTO BuildCorrection(
        int year,
        int month,
        SettlementPreviewResDTO preview,
        string idempotencyKey,
        IEnumerable<SettlementExceptionReqDTO> exceptions,
        string correctionReason)
    {
        var confirmation = BuildConfirm(
            year,
            month,
            preview,
            idempotencyKey,
            exceptions);

        return new SettlementCorrectionReqDTO
        {
            Year = confirmation.Year,
            Month = confirmation.Month,
            PriceAsOfUtc = confirmation.PriceAsOfUtc,
            InputHash = confirmation.InputHash,
            PrimarySupplierId = confirmation.PrimarySupplierId,
            PriceListId = confirmation.PriceListId,
            IdempotencyKey = confirmation.IdempotencyKey,
            Exceptions = confirmation.Exceptions,
            Reason = correctionReason.Trim()
        };
    }

    private static List<SettlementExceptionReqDTO> CloneExceptions(
        IEnumerable<SettlementExceptionReqDTO> exceptions) =>
        exceptions.Select(source => new SettlementExceptionReqDTO
        {
            VppId = source.VppId,
            SupplierId = source.SupplierId,
            PriceListId = source.PriceListId,
            Reason = source.Reason?.Trim()
        }).ToList();
}
