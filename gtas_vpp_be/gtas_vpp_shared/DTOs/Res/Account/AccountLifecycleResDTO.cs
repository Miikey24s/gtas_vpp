namespace gtas_vpp_shared.DTOs.Res.Account;

public sealed class AccountLifecycleResDTO
{
    public int AccountId { get; init; }

    public string AccountStatus { get; init; } = string.Empty;

    public bool MustChangePassword { get; init; }

    public bool EmailConfirmed { get; init; }

    public string Message { get; init; } = string.Empty;
}
