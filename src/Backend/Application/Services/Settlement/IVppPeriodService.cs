using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;
using gtas_vpp_shared.DTOs.Req.VPP;
using gtas_vpp_shared.DTOs.Res.VPP;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Boundary của rolling horizon và vòng đời kỳ. Period instances là source of
/// truth; settings chỉ được dùng khi tạo period tương lai.
/// </summary>
public interface IVppPeriodService
{
    Task<VppPeriod> EnsureCurrentAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<VppPeriod> EnsureAsync(
        string memberCompanyCode,
        int year,
        int month,
        CancellationToken cancellationToken = default);

    Task<VppPeriod> EnsureAsync(
        string memberCompanyCode,
        Period period,
        CancellationToken cancellationToken = default);

    Task<VppPeriod?> GetAsync(
        string memberCompanyCode,
        Period period,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VppPeriod>> GetOpenPeriodsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<VppOrderPeriodSettingsResDTO> GetSettingsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<VppOrderPeriodSettingsResDTO> GetEffectiveSettingsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VppOrderPeriodSettingsResDTO>> ListSettingsVersionsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<VppOrderPeriodSettingsResDTO> SaveSettingsAsync(
        string memberCompanyCode,
        int actorUserId,
        VppOrderPeriodSettingsReqDTO request,
        CancellationToken cancellationToken = default);

    Task<VppPeriodHorizonPreviewResDTO> PreviewHorizonAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VppManagedPeriodResDTO>> TopUpOpenHorizonAsync(
        string memberCompanyCode,
        int actorUserId = 0,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VppManagedPeriodResDTO>> ListManagedAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<VppManagedPeriodResDTO> CreateManualAsync(
        string memberCompanyCode,
        int actorUserId,
        VppOrderPeriodManualCreateReqDTO request,
        CancellationToken cancellationToken = default);

    Task<VppManagedPeriodResDTO> UpdateScheduleAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodUpdateReqDTO request,
        CancellationToken cancellationToken = default);

    Task<VppManagedPeriodResDTO> ExtendDeadlineAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodExtendDeadlineReqDTO request,
        CancellationToken cancellationToken = default);

    Task<VppManagedPeriodResDTO> CloseSubmissionsAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default);

    Task<VppManagedPeriodResDTO> ReopenSubmissionsAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodReopenSubmissionsReqDTO request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default);

    Task<VppManagedPeriodResDTO> RestoreAsync(
        Guid periodId,
        int actorUserId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default);

    Task HardDeleteAsync(
        Guid periodId,
        VppOrderPeriodCommandReqDTO request,
        CancellationToken cancellationToken = default);

    Task AdvanceDuePeriodsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VppPeriod>> AdvanceDuePeriodsAsync(
        CancellationToken cancellationToken = default);

    Task TransitionAsync(
        Guid periodId,
        VppPeriodState targetState,
        int actorUserId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
