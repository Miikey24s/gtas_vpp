using gtas_vpp_be.Model.VPP;
using gtas_vpp_be.Service.Domain;

namespace gtas_vpp_be.Service.Services;

/// <summary>
/// Durable period lifecycle boundary used by request and settlement services.
/// Implementations must make Ensure/Advance idempotent and perform legal
/// transitions atomically against the period row.
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

    /// <summary>Recovers all known company periods (used by the hosted worker).</summary>
    Task<IReadOnlyList<VppPeriod>> AdvanceDuePeriodsAsync(
        CancellationToken cancellationToken = default);

    Task TransitionAsync(
        Guid periodId,
        VppPeriodState targetState,
        int actorUserId,
        string? reason = null,
        CancellationToken cancellationToken = default);
}
