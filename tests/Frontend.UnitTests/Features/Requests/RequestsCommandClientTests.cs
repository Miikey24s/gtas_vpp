using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class RequestsCommandClientTests
{
    [Fact]
    public async Task OrderAndApprovalCommands_UseExpectedMethodsEndpointsAndContracts()
    {
        var orderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var calls = new List<(string Method, string Endpoint, Type BodyType, Type ResponseType)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, body, responseType) =>
            {
                calls.Add(("POST", endpoint, body?.GetType() ?? typeof(object), responseType));
                return Task.FromResult<object?>(null);
            },
            PutAsync = (endpoint, body, responseType) =>
            {
                calls.Add(("PUT", endpoint, body.GetType(), responseType));
                return Task.FromResult<object?>(null);
            }
        };
        var client = new RequestsCommandClient(api);

        await client.CreateAsync(new VppRequestCreateReqDTO());
        await client.UpdateAsync(orderId, new VppRequestUpdateReqDTO());
        await client.AdjustAfterCloseAsync(orderId, new VppManagerOrderAdjustmentReqDTO());
        await client.RecreateAsync(orderId, new VppRequestRecreateReqDTO());
        await client.CancelAsync(orderId, new VppRequestCancelReqDTO());
        await client.RestoreAsync(orderId, new VppRequestRestoreReqDTO());
        await client.ApproveAdditionalAsync(orderId, new ApproveOrderReqDTO());
        await client.RejectAdditionalAsync(orderId, new RejectOrderReqDTO());

        Assert.Equal(
            [
                ("POST", "/api/VPPRequest/orders", typeof(VppRequestCreateReqDTO), typeof(VppRequestResDTO)),
                ("PUT", $"/api/VPPRequest/orders/{orderId}", typeof(VppRequestUpdateReqDTO), typeof(VppRequestResDTO)),
                ("POST", $"/api/VPPRequest/orders/{orderId}/manager-adjustment", typeof(VppManagerOrderAdjustmentReqDTO), typeof(VppRequestResDTO)),
                ("POST", $"/api/VPPRequest/orders/{orderId}/recreate", typeof(VppRequestRecreateReqDTO), typeof(VppRequestResDTO)),
                ("POST", $"/api/VPPRequest/orders/{orderId}/cancel", typeof(VppRequestCancelReqDTO), typeof(object)),
                ("POST", $"/api/VPPRequest/orders/{orderId}/restore", typeof(VppRequestRestoreReqDTO), typeof(VppRequestResDTO)),
                ("POST", $"/api/VPPRequest/additional-orders/{orderId}/approve", typeof(ApproveOrderReqDTO), typeof(object)),
                ("POST", $"/api/VPPRequest/additional-orders/{orderId}/reject", typeof(RejectOrderReqDTO), typeof(object))
            ],
            calls);
    }
}
