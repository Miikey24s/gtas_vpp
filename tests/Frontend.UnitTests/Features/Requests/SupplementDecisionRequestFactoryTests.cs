using gtas_vpp_fe.Features.Requests.Approval;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class SupplementDecisionRequestFactoryTests
{
    [Fact]
    public void BuildApprove_ReusesKeyPerOrderAndActionAndPreservesRowVersion()
    {
        var factory = new SupplementDecisionRequestFactory();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        var first = factory.BuildApprove(orderId, rowVersion);
        var retry = factory.BuildApprove(orderId, rowVersion);
        var otherOrder = factory.BuildApprove(Guid.NewGuid(), rowVersion);

        Assert.Same(rowVersion, first.RowVersion);
        Assert.Equal(first.IdempotencyKey, retry.IdempotencyKey);
        Assert.NotEqual(first.IdempotencyKey, otherOrder.IdempotencyKey);
        Assert.Equal(32, first.IdempotencyKey?.Length);
        Assert.True(Guid.TryParseExact(first.IdempotencyKey, "N", out _));
    }

    [Fact]
    public void BuildReject_UsesSeparateActionKeyAndPreservesReasonExactly()
    {
        var factory = new SupplementDecisionRequestFactory();
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 4, 5, 6 };
        var approve = factory.BuildApprove(orderId, rowVersion);

        var firstReject = factory.BuildReject(orderId, rowVersion, "  lý do từ chối  ");
        var retryReject = factory.BuildReject(orderId, rowVersion, "lý do mới");

        Assert.Same(rowVersion, firstReject.RowVersion);
        Assert.Equal("  lý do từ chối  ", firstReject.Reason);
        Assert.NotEqual(approve.IdempotencyKey, firstReject.IdempotencyKey);
        Assert.Equal(firstReject.IdempotencyKey, retryReject.IdempotencyKey);
        Assert.Equal("lý do mới", retryReject.Reason);
    }
}
