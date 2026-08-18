using gtas_vpp_fe.Services;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_fe.Features.Requests.Api;

public sealed class OrderPeriodApiClient(IAPIServices api)
{
    private const string BasePath = "/api/order-periods";

    public async Task<IReadOnlyList<VppManagedPeriodResDTO>> ListAsync() =>
        await api.GetFromApiAsync<List<VppManagedPeriodResDTO>>(BasePath) ?? [];

    public Task<VppOrderPeriodSettingsResDTO?> GetSettingsAsync() =>
        api.GetFromApiAsync<VppOrderPeriodSettingsResDTO>($"{BasePath}/settings");

    public Task<VppOrderPeriodSettingsResDTO?> GetCurrentSettingsAsync() =>
        api.GetFromApiAsync<VppOrderPeriodSettingsResDTO>($"{BasePath}/settings/current");

    public async Task<IReadOnlyList<VppOrderPeriodSettingsResDTO>> ListSettingsHistoryAsync() =>
        await api.GetFromApiAsync<List<VppOrderPeriodSettingsResDTO>>(
            $"{BasePath}/settings/history") ?? [];

    public Task<VppOrderPeriodSettingsResDTO?> SaveSettingsAsync(VppOrderPeriodSettingsReqDTO request) =>
        api.PostFromApiAsync<VppOrderPeriodSettingsResDTO>($"{BasePath}/settings", request);

    public Task<VppPeriodHorizonPreviewResDTO?> PreviewAsync() =>
        api.PostFromApiAsync<VppPeriodHorizonPreviewResDTO>($"{BasePath}/horizon-preview", new { });

    public async Task<IReadOnlyList<VppManagedPeriodResDTO>> TopUpAsync() =>
        await api.PostFromApiAsync<List<VppManagedPeriodResDTO>>($"{BasePath}/top-up", new { }) ?? [];

    public Task<VppManagedPeriodResDTO?> CreateManualAsync(VppOrderPeriodManualCreateReqDTO request) =>
        api.PostFromApiAsync<VppManagedPeriodResDTO>(BasePath, request);

    public Task<VppManagedPeriodResDTO?> UpdateAsync(Guid id, VppOrderPeriodUpdateReqDTO request) =>
        api.PutFromApiAsync<VppManagedPeriodResDTO>($"{BasePath}/{id}", request);

    public Task<VppManagedPeriodResDTO?> ExtendAsync(Guid id, VppOrderPeriodExtendDeadlineReqDTO request) =>
        api.PostFromApiAsync<VppManagedPeriodResDTO>($"{BasePath}/{id}/extend-deadline", request);

    public Task<VppManagedPeriodResDTO?> CloseAsync(Guid id, VppOrderPeriodCommandReqDTO request) =>
        api.PostFromApiAsync<VppManagedPeriodResDTO>($"{BasePath}/{id}/close-submissions", request);

    public Task<VppManagedPeriodResDTO?> ReopenAsync(
        Guid id,
        VppOrderPeriodReopenSubmissionsReqDTO request) =>
        api.PostFromApiAsync<VppManagedPeriodResDTO>(
            $"{BasePath}/{id}/reopen-submissions",
            request);

    public async Task DeactivateAsync(Guid id, VppOrderPeriodCommandReqDTO request)
        => _ = await api.PostFromApiAsync<object>($"{BasePath}/{id}/delete", request);

    public Task<VppManagedPeriodResDTO?> RestoreAsync(
        Guid id,
        VppOrderPeriodCommandReqDTO request) =>
        api.PostFromApiAsync<VppManagedPeriodResDTO>($"{BasePath}/{id}/restore", request);

    public async Task HardDeleteAsync(Guid id, VppOrderPeriodCommandReqDTO request)
        => _ = await api.PostFromApiAsync<object>($"{BasePath}/{id}/hard-delete", request);
}
