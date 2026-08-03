using gtas_vpp_fe.Features.Settlement.State;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Settlement;

public sealed class PeriodSettlementStateTests
{
    [Fact]
    public void SetPeriod_WhenPeriodChanges_ClearsPreviewExceptionsSupplierAndIdempotencyKey()
    {
        var state = new PeriodSettlementState();
        state.SetPeriod(2026, 7);
        state.Exceptions.Add(new SettlementExceptionReqDTO());
        state.SelectedSupplierId = Guid.NewGuid();
        state.SetPreview(new SettlementPreviewResDTO { Year = 2026, Month = 7 });

        state.SetPeriod(2026, 8);

        Assert.Equal(2026, state.Year);
        Assert.Equal(8, state.Month);
        Assert.Null(state.Preview);
        Assert.Empty(state.Exceptions);
        Assert.Null(state.SelectedSupplierId);
        Assert.Null(state.IdempotencyKey);
    }

    [Fact]
    public void SetPreview_ForAnotherPeriod_IsRejected()
    {
        var state = new PeriodSettlementState();
        state.SetPeriod(2026, 7);

        var exception = Assert.Throws<InvalidOperationException>(
            () => state.SetPreview(new SettlementPreviewResDTO { Year = 2026, Month = 8 }));

        Assert.Equal("Bản xem trước không thuộc kỳ đang vận hành.", exception.Message);
        Assert.Null(state.Preview);
        Assert.Null(state.IdempotencyKey);
    }

    [Fact]
    public void SetPreview_RetryWithinSameAttempt_PreservesIdempotencyKey()
    {
        var state = new PeriodSettlementState();
        state.SetPeriod(2026, 7);
        state.SetPreview(new SettlementPreviewResDTO { Year = 2026, Month = 7, InputHash = "first" });
        var key = state.IdempotencyKey;

        state.SetPreview(new SettlementPreviewResDTO { Year = 2026, Month = 7, InputHash = "second" });

        Assert.NotNull(key);
        Assert.Equal(key, state.IdempotencyKey);
        Assert.Equal("second", state.Preview?.InputHash);
    }

    [Fact]
    public void RequireFreshPreviewForNextSubmission_KeepsContextAndRotatesKeyOnNextPreview()
    {
        var preview = new SettlementPreviewResDTO { Year = 2026, Month = 7, InputHash = "submitted" };
        var state = new PeriodSettlementState();
        state.SetPeriod(2026, 7);
        state.SetPreview(preview);
        var submittedKey = state.IdempotencyKey;

        state.RequireFreshPreviewForNextSubmission();

        Assert.Null(state.IdempotencyKey);
        Assert.Same(preview, state.Preview);
        Assert.True(state.RequiresFreshPreviewForSubmission);

        state.SetPreview(new SettlementPreviewResDTO { Year = 2026, Month = 7, InputHash = "fresh" });

        Assert.NotNull(state.IdempotencyKey);
        Assert.False(state.RequiresFreshPreviewForSubmission);
        Assert.NotEqual(submittedKey, state.IdempotencyKey);
        Assert.Equal("fresh", state.Preview?.InputHash);
    }
}
