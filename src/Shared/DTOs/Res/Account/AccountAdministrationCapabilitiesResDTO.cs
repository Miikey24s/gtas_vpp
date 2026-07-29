namespace gtas_vpp_shared.DTOs.Res.Account;

public sealed class AccountAdministrationCapabilitiesResDTO
{
    public bool InvitationEnabled { get; init; }
    public string DeliveryMode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
