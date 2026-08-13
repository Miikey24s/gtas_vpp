namespace gtas_vpp_fe.Components.DesignSystem.Composites;

public enum VppAdminActionTone
{
    Default,
    Warning,
    Danger
}

public sealed record VppAdminActionMenuItem(
    string Key,
    string Text,
    string Icon,
    Func<Task> ExecuteAsync,
    bool Disabled = false,
    VppAdminActionTone Tone = VppAdminActionTone.Default,
    string? DisabledReason = null);
