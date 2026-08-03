using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Features.Requests.Submission;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class OrderSubmissionCoordinatorTests
{
    [Fact]
    public async Task SubmitAsync_Create_PreservesSeparateRouteAndEditorAdditionalFlags()
    {
        string? endpoint = null;
        object? body = null;
        var api = new StubApiServices
        {
            PostAsync = (capturedEndpoint, capturedBody, _) =>
            {
                endpoint = capturedEndpoint;
                body = capturedBody;
                return Task.FromResult<object?>(null);
            }
        };
        var coordinator = new OrderSubmissionCoordinator(new RequestsCommandClient(api));
        var baseRequestId = Guid.NewGuid();

        var outcome = await coordinator.SubmitAsync(new OrderSubmissionOperation.Create(
            CreateSubmission(),
            2026,
            8,
            IsAdditionalOrder: false,
            IncludeAdditionalContext: true,
            BaseRequestId: baseRequestId));

        Assert.Equal(OrderSubmissionOutcome.Created, outcome);
        Assert.Equal("/api/VPPRequest/orders", endpoint);
        var request = Assert.IsType<VppRequestCreateReqDTO>(body);
        Assert.False(request.IsAdditionalOrder);
        Assert.Equal(baseRequestId, request.BaseRequestId);
        Assert.Equal("lý do bổ sung", request.SupplementReason);
        Assert.Equal(2026, request.Year);
        Assert.Equal(8, request.Month);
        Assert.Equal("submission-key", request.IdempotencyKey);
        Assert.Equal([2, 3], request.Items.Select(item => item.Qty));
    }

    [Fact]
    public async Task SubmitAsync_Update_UsesOrderIdRouteAdditionalAndRowVersion()
    {
        string? endpoint = null;
        object? body = null;
        var api = new StubApiServices
        {
            PutAsync = (capturedEndpoint, capturedBody, _) =>
            {
                endpoint = capturedEndpoint;
                body = capturedBody;
                return Task.FromResult<object?>(null);
            }
        };
        var coordinator = new OrderSubmissionCoordinator(new RequestsCommandClient(api));
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 1, 2, 3 };

        var outcome = await coordinator.SubmitAsync(new OrderSubmissionOperation.Update(
            CreateSubmission(),
            orderId,
            IsAdditionalOrder: true,
            RowVersion: rowVersion));

        Assert.Equal(OrderSubmissionOutcome.Updated, outcome);
        Assert.Equal($"/api/VPPRequest/orders/{orderId}", endpoint);
        var request = Assert.IsType<VppRequestUpdateReqDTO>(body);
        Assert.Equal(orderId, request.Id);
        Assert.True(request.IsAdditionalOrder);
        Assert.Same(rowVersion, request.RowVersion);
    }

    [Fact]
    public async Task SubmitAsync_Recreate_UsesEditorAdditionalAndReturnsRecreatedOutcome()
    {
        string? endpoint = null;
        object? body = null;
        var api = new StubApiServices
        {
            PostAsync = (capturedEndpoint, capturedBody, _) =>
            {
                endpoint = capturedEndpoint;
                body = capturedBody;
                return Task.FromResult<object?>(null);
            }
        };
        var coordinator = new OrderSubmissionCoordinator(new RequestsCommandClient(api));
        var orderId = Guid.NewGuid();
        var rowVersion = new byte[] { 4, 5, 6 };

        var outcome = await coordinator.SubmitAsync(new OrderSubmissionOperation.Recreate(
            CreateSubmission(),
            orderId,
            IsAdditionalOrder: true,
            RowVersion: rowVersion));

        Assert.Equal(OrderSubmissionOutcome.Recreated, outcome);
        Assert.Equal($"/api/VPPRequest/orders/{orderId}/recreate", endpoint);
        var request = Assert.IsType<VppRequestRecreateReqDTO>(body);
        Assert.Equal("lý do bổ sung", request.SupplementReason);
        Assert.Same(rowVersion, request.RowVersion);
    }

    [Fact]
    public async Task SubmitAsync_PropagatesTransportFailureToThePage()
    {
        var expected = new InvalidOperationException("transport failed");
        var api = new StubApiServices
        {
            PostAsync = (_, _, _) => Task.FromException<object?>(expected)
        };
        var coordinator = new OrderSubmissionCoordinator(new RequestsCommandClient(api));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            coordinator.SubmitAsync(new OrderSubmissionOperation.Create(
                CreateSubmission(),
                2026,
                8,
                IsAdditionalOrder: false,
                IncludeAdditionalContext: false,
                BaseRequestId: null)));

        Assert.Same(expected, thrown);
    }

    private static OrderSubmissionSnapshot CreateSubmission() => new(
        Description: "ghi chú",
        SupplementReason: "  lý do bổ sung  ",
        IdempotencyKey: "submission-key",
        Items:
        [
            new OrderSubmissionItem(Guid.NewGuid(), 2, "mục 1"),
            new OrderSubmissionItem(Guid.NewGuid(), 3, "mục 2")
        ]);
}
