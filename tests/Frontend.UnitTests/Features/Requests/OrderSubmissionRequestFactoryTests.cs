using gtas_vpp_fe.Features.Requests.Submission;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class OrderSubmissionRequestFactoryTests
{
    [Fact]
    public void BuildCreateRequest_MapsPeriodItemsAndAdditionalRequestScope()
    {
        var baseRequestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var itemId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var submission = new OrderSubmissionSnapshot(
            "Đơn tháng 7",
            "  Bổ sung theo biên bản  ",
            "create-key",
            [new OrderSubmissionItem(itemId, 3, "Giao đầu tháng")]);

        var regular = OrderSubmissionRequestFactory.BuildCreateRequest(
            submission,
            2026,
            7,
            false,
            false,
            baseRequestId);
        var additional = OrderSubmissionRequestFactory.BuildCreateRequest(
            submission,
            2026,
            7,
            true,
            true,
            baseRequestId);

        Assert.Equal(2026, regular.Year);
        Assert.Equal(7, regular.Month);
        Assert.False(regular.IsAdditionalOrder);
        Assert.Null(regular.BaseRequestId);
        Assert.Null(regular.SupplementReason);
        Assert.Equal("create-key", regular.IdempotencyKey);
        var regularItem = Assert.Single(regular.Items);
        Assert.Equal(itemId, regularItem.VppId);
        Assert.Equal(3, regularItem.Qty);
        Assert.Equal("Giao đầu tháng", regularItem.Description);

        Assert.True(additional.IsAdditionalOrder);
        Assert.Equal(baseRequestId, additional.BaseRequestId);
        Assert.Equal("Bổ sung theo biên bản", additional.SupplementReason);
        Assert.Equal("Đơn tháng 7", additional.Description);
    }

    [Fact]
    public void BuildCreateRequest_PreservesDistinctRouteAndWizardAdditionalFlags()
    {
        var baseRequestId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var submission = new OrderSubmissionSnapshot(
            "Đơn chuyển query",
            "  Lý do từ context  ",
            "mismatch-key",
            []);

        var routeOnly = OrderSubmissionRequestFactory.BuildCreateRequest(
            submission,
            2026,
            7,
            true,
            false,
            baseRequestId);
        var contextOnly = OrderSubmissionRequestFactory.BuildCreateRequest(
            submission,
            2026,
            7,
            false,
            true,
            baseRequestId);

        Assert.True(routeOnly.IsAdditionalOrder);
        Assert.Null(routeOnly.BaseRequestId);
        Assert.Null(routeOnly.SupplementReason);
        Assert.False(contextOnly.IsAdditionalOrder);
        Assert.Equal(baseRequestId, contextOnly.BaseRequestId);
        Assert.Equal("Lý do từ context", contextOnly.SupplementReason);
    }

    [Fact]
    public void BuildUpdateRequest_PreservesOrderIdentityRowVersionAndExistingReasonPayload()
    {
        var orderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        var submission = new OrderSubmissionSnapshot(
            "Cập nhật số lượng",
            "  lý do hiện hành giữ nguyên  ",
            "update-key",
            [new OrderSubmissionItem(Guid.NewGuid(), 4, "Hàng cập nhật")]);

        var request = OrderSubmissionRequestFactory.BuildUpdateRequest(
            submission,
            orderId,
            true,
            rowVersion);

        Assert.Equal(orderId, request.Id);
        Assert.Equal("Cập nhật số lượng", request.Description);
        Assert.True(request.IsAdditionalOrder);
        Assert.Equal("  lý do hiện hành giữ nguyên  ", request.SupplementReason);
        Assert.Same(rowVersion, request.RowVersion);
        Assert.Equal("update-key", request.IdempotencyKey);
        var item = Assert.Single(request.Items);
        Assert.Equal(4, item.Qty);
        Assert.Equal("Hàng cập nhật", item.Description);
    }

    [Fact]
    public void BuildRecreateRequest_PreservesConcurrencyAndTrimsAdditionalReason()
    {
        var rowVersion = new byte[] { 5, 6, 7, 8 };
        var submission = new OrderSubmissionSnapshot(
            "Tạo lại đơn",
            "  Tạo lại theo yêu cầu quản lý  ",
            "recreate-key",
            [new OrderSubmissionItem(Guid.NewGuid(), 2, "Hàng tạo lại")]);

        var request = OrderSubmissionRequestFactory.BuildRecreateRequest(
            submission,
            true,
            rowVersion);

        Assert.Equal("Tạo lại đơn", request.Description);
        Assert.Equal("Tạo lại theo yêu cầu quản lý", request.SupplementReason);
        Assert.Same(rowVersion, request.RowVersion);
        Assert.Equal("recreate-key", request.IdempotencyKey);
        Assert.Equal(2, Assert.Single(request.Items).Qty);

        var regularRequest = OrderSubmissionRequestFactory.BuildRecreateRequest(
            submission,
            false,
            rowVersion);
        Assert.Null(regularRequest.SupplementReason);
    }
}
