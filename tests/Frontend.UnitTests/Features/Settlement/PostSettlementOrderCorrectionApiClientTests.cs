using gtas_vpp_fe.Features.Settlement.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Settlement;

public sealed class PostSettlementOrderCorrectionApiClientTests
{
    [Fact]
    public async Task Create_confirm_and_reject_use_period_manager_endpoints()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var calls = new List<string>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, _, responseType) =>
            {
                calls.Add(endpoint);
                Assert.Equal(typeof(PostSettlementOrderCorrectionResDTO), responseType);
                return Task.FromResult<object?>(new PostSettlementOrderCorrectionResDTO());
            }
        };
        var client = new PostSettlementOrderCorrectionApiClient(api);

        await client.CreateAsync(new PostSettlementOrderCorrectionCreateReqDTO());
        await client.ConfirmAsync(id, new PostSettlementOrderCorrectionDecisionReqDTO());
        await client.RejectAsync(id, new PostSettlementOrderCorrectionDecisionReqDTO());

        Assert.Equal(
        [
            "/api/post-settlement-order-corrections",
            $"/api/post-settlement-order-corrections/{id}/confirm",
            $"/api/post-settlement-order-corrections/{id}/reject"
        ], calls);
    }
}
