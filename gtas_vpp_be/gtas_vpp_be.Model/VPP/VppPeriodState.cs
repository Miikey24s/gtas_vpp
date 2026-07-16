namespace gtas_vpp_be.Model.VPP;

/// <summary>
/// Persisted lifecycle of a VPP period.  Values are intentionally explicit so
/// that the database and API can evolve without inferring state from request
/// rows.
/// </summary>
public enum VppPeriodState
{
    Open = 0,
    SubmissionClosed = 1,
    Pricing = 2,
    Settled = 3
}
