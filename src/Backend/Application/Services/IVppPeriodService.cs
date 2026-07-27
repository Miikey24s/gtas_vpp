using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Boundary vòng đời kỳ bền vững dùng bởi service yêu cầu và chốt kỳ.
/// Implementation phải bảo đảm Ensure/Advance idempotent và thực hiện transition
/// hợp lệ theo cách atomic trên dòng kỳ.
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

    Task AdvanceDuePeriodsAsync(
        string memberCompanyCode,
        CancellationToken cancellationToken = default);

    /// <summary>Phục hồi mọi kỳ công ty đã biết, dùng bởi hosted worker.</summary>
    Task<IReadOnlyList<VppPeriod>> AdvanceDuePeriodsAsync(
        CancellationToken cancellationToken = default);

    Task TransitionAsync(
        Guid periodId,
        VppPeriodState targetState,
        int actorUserId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
