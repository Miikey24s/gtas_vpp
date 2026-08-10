using gtas_vpp_fe.Features.Requests.Api;
using gtas_vpp_fe.Tests.TestDoubles;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;
using Xunit;

namespace gtas_vpp_fe.Tests.Features.Requests;

public sealed class OrderPeriodApiClientTests
{
    [Fact]
    public async Task Settings_preview_and_top_up_use_canonical_endpoints()
    {
        var posts = new List<(string Endpoint, Type ResponseType)>();
        var api = new StubApiServices
        {
            PostAsync = (endpoint, _, responseType) =>
            {
                posts.Add((endpoint, responseType));
                object response = responseType == typeof(VppOrderPeriodSettingsResDTO)
                    ? new VppOrderPeriodSettingsResDTO()
                    : responseType == typeof(VppPeriodHorizonPreviewResDTO)
                        ? new VppPeriodHorizonPreviewResDTO()
                        : responseType == typeof(VppManagedPeriodResDTO)
                            ? new VppManagedPeriodResDTO()
                        : new List<VppManagedPeriodResDTO>();
                return Task.FromResult<object?>(response);
            }
        };
        var client = new OrderPeriodApiClient(api);

        await client.SaveSettingsAsync(new VppOrderPeriodSettingsReqDTO());
        await client.PreviewAsync();
        await client.TopUpAsync();
        var periodId = Guid.Parse("9f79b53d-30d6-4616-b81f-2ce4d22fb540");
        await client.ReopenAsync(periodId, new VppOrderPeriodReopenSubmissionsReqDTO());

        Assert.Equal(
        [
            ("/api/order-periods/settings", typeof(VppOrderPeriodSettingsResDTO)),
            ("/api/order-periods/horizon-preview", typeof(VppPeriodHorizonPreviewResDTO)),
            ("/api/order-periods/top-up", typeof(List<VppManagedPeriodResDTO>)),
            ($"/api/order-periods/{periodId}/reopen-submissions", typeof(VppManagedPeriodResDTO))
        ], posts);
    }

    [Fact]
    public async Task List_and_settings_use_order_period_resource()
    {
        var gets = new List<(string Endpoint, Type ResponseType)>();
        var api = new StubApiServices
        {
            GetAsync = (endpoint, responseType) =>
            {
                gets.Add((endpoint, responseType));
                object response = responseType == typeof(List<VppManagedPeriodResDTO>)
                    ? new List<VppManagedPeriodResDTO>()
                    : responseType == typeof(List<VppOrderPeriodSettingsResDTO>)
                        ? new List<VppOrderPeriodSettingsResDTO>()
                        : new VppOrderPeriodSettingsResDTO();
                return Task.FromResult<object?>(response);
            }
        };
        var client = new OrderPeriodApiClient(api);

        await client.ListAsync();
        await client.GetSettingsAsync();
        await client.GetCurrentSettingsAsync();
        await client.ListSettingsHistoryAsync();

        Assert.Equal(
        [
            ("/api/order-periods", typeof(List<VppManagedPeriodResDTO>)),
            ("/api/order-periods/settings", typeof(VppOrderPeriodSettingsResDTO)),
            ("/api/order-periods/settings/current", typeof(VppOrderPeriodSettingsResDTO)),
            ("/api/order-periods/settings/history", typeof(List<VppOrderPeriodSettingsResDTO>))
        ], gets);
    }
}
