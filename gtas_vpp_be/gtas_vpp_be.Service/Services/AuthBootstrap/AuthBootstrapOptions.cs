namespace gtas_vpp_be.Service.Services.AuthBootstrap;

/// <summary>
/// Explicit inputs for the one-time clean-owner bootstrap. The feature is
/// disabled unless an operator deliberately enables it for a single run.
/// Never log or serialize an instance of this type because it contains the
/// initial password.
/// </summary>
public sealed class AuthBootstrapOptions
{
    public const string SectionName = "AuthBootstrap";

    public bool Enabled { get; set; }

    public string OperationKey { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string InitialPassword { get; set; } = string.Empty;

    public string PrimaryDepartmentCode { get; set; } = string.Empty;

    /// <summary>
    /// Local-only convenience for a brand-new TEST/DEMO database. Production
    /// bootstrap must point to an existing organizational department.
    /// </summary>
    public bool CreatePrimaryDepartmentIfMissing { get; set; }

    public string PrimaryDepartmentName { get; set; } = string.Empty;

    public override string ToString() => nameof(AuthBootstrapOptions);
}
