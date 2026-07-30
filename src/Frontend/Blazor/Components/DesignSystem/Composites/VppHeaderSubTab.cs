namespace gtas_vpp_fe.Components.DesignSystem.Composites;

/// <summary>
/// Một đích điều hướng cấp hai trong primary header; trạng thái active luôn do URL hiện tại quyết định.
/// </summary>
public sealed record VppHeaderSubTab(string Label, string Path, bool IsActive);
