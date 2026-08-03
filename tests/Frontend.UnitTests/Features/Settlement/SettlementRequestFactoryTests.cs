using gtas_vpp_fe.Features.Settlement.Submission;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Settlement;

public sealed class SettlementRequestFactoryTests
{
    [Fact]
    public void BuildPreview_PreservesSelectionTimeAndClonesExceptions()
    {
        var supplierId = Guid.NewGuid();
        var priceListId = Guid.NewGuid();
        var priceAsOfUtc = new DateTime(2026, 7, 10, 9, 15, 0, DateTimeKind.Utc);
        var exceptions = CreateExceptions();

        var request = SettlementRequestFactory.BuildPreview(
            2026,
            7,
            supplierId,
            priceListId,
            priceAsOfUtc,
            exceptions);

        Assert.Equal(2026, request.Year);
        Assert.Equal(7, request.Month);
        Assert.Equal(supplierId, request.PrimarySupplierId);
        Assert.Equal(priceListId, request.PriceListId);
        Assert.Equal(priceAsOfUtc, request.PriceAsOfUtc);
        Assert.Equal(DateTimeKind.Utc, request.PriceAsOfUtc!.Value.Kind);
        Assert.NotSame(exceptions, request.Exceptions);
        Assert.Equal(["ngoại lệ 1", null], request.Exceptions.Select(item => item.Reason));
    }

    [Fact]
    public void BuildConfirm_CopiesPreviewIdentityAndClonesExceptionsInOrder()
    {
        var preview = CreatePreview();
        var exceptions = CreateExceptions();

        var request = SettlementRequestFactory.BuildConfirm(
            2026,
            7,
            preview,
            "  settlement-key  ",
            exceptions);

        Assert.Equal(2026, request.Year);
        Assert.Equal(7, request.Month);
        Assert.Equal(preview.PriceAsOfUtc, request.PriceAsOfUtc);
        Assert.Equal(preview.InputHash, request.InputHash);
        Assert.Equal(preview.PrimarySupplierId, request.PrimarySupplierId);
        Assert.Equal(preview.PrimaryPriceListId, request.PriceListId);
        Assert.Equal("  settlement-key  ", request.IdempotencyKey);
        Assert.NotSame(exceptions, request.Exceptions);
        Assert.Equal(exceptions.Select(item => item.VppId), request.Exceptions.Select(item => item.VppId));
        Assert.Equal(exceptions.Select(item => item.SupplierId), request.Exceptions.Select(item => item.SupplierId));
        Assert.NotSame(exceptions[0], request.Exceptions[0]);
        Assert.NotSame(exceptions[1], request.Exceptions[1]);
        Assert.Equal("ngoại lệ 1", request.Exceptions[0].Reason);
        Assert.Null(request.Exceptions[1].Reason);
        Assert.Equal("  ngoại lệ 1  ", exceptions[0].Reason);
    }

    [Fact]
    public void BuildCorrection_ReusesConfirmationSnapshotAndTrimsReason()
    {
        var preview = CreatePreview();
        var exceptions = CreateExceptions();

        var request = SettlementRequestFactory.BuildCorrection(
            2026,
            7,
            preview,
            "correction-key",
            exceptions,
            "  điều chỉnh nhà cung cấp  ");

        Assert.Equal(2026, request.Year);
        Assert.Equal(7, request.Month);
        Assert.Equal(preview.PriceAsOfUtc, request.PriceAsOfUtc);
        Assert.Equal(preview.InputHash, request.InputHash);
        Assert.Equal(preview.PrimarySupplierId, request.PrimarySupplierId);
        Assert.Equal(preview.PrimaryPriceListId, request.PriceListId);
        Assert.Equal("correction-key", request.IdempotencyKey);
        Assert.Equal("điều chỉnh nhà cung cấp", request.Reason);
        Assert.Equal(["ngoại lệ 1", null], request.Exceptions.Select(item => item.Reason));
    }

    [Fact]
    public void BuildConfirm_WithoutPrimarySelection_PreservesLegacyValueFailure()
    {
        var preview = CreatePreview();
        preview.PrimarySupplierId = null;

        Assert.Throws<InvalidOperationException>(() => SettlementRequestFactory.BuildConfirm(
            2026,
            7,
            preview,
            "settlement-key",
            []));
    }

    private static SettlementPreviewResDTO CreatePreview() => new()
    {
        Year = 2030,
        Month = 12,
        PriceAsOfUtc = new DateTime(2026, 7, 15, 8, 30, 0, DateTimeKind.Utc),
        InputHash = "preview-hash",
        PrimarySupplierId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        PrimaryPriceListId = Guid.Parse("22222222-2222-2222-2222-222222222222")
    };

    private static List<SettlementExceptionReqDTO> CreateExceptions() =>
    [
        new()
        {
            VppId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            SupplierId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Reason = "  ngoại lệ 1  "
        },
        new()
        {
            VppId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            SupplierId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Reason = null
        }
    ];
}
