namespace gtas_vpp_be.Service.Services.AuthBootstrap;

public interface IAuthBootstrapProvisioner
{
    Task<AuthBootstrapResult> ProvisionAsync(CancellationToken cancellationToken = default);
}

public enum AuthBootstrapOutcome
{
    Provisioned = 0,
    AlreadyCompleted = 1
}

/// <summary>
/// Chủ động không chứa credential hoặc dữ liệu hồ sơ owner.
/// </summary>
public sealed record AuthBootstrapResult(int AccountId, AuthBootstrapOutcome Outcome);

public enum AuthBootstrapFailure
{
    Disabled = 0,
    InvalidOptions,
    UnsafeDatabaseBinding,
    UnsupportedProvider,
    UserManagerContextMismatch,
    LockUnavailable,
    LegacyContainmentDrift,
    OperationFingerprintMismatch,
    OperationStateInvalid,
    PreexistingActiveAccount,
    PreexistingActiveMembership,
    MissingSystemAdminGroup,
    MissingPrimaryDepartment,
    InvalidPrimaryDepartment,
    MissingPermissionManage,
    AccountCreationFailed,
    InvalidGeneratedAccountId,
    CompletedStateMismatch,
    PersistenceFailed,
    TestRollbackFailed
}

public sealed class AuthBootstrapProvisioningException : InvalidOperationException
{
    public AuthBootstrapProvisioningException(
        AuthBootstrapFailure failure,
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Failure = failure;
    }

    public AuthBootstrapFailure Failure { get; }
}
