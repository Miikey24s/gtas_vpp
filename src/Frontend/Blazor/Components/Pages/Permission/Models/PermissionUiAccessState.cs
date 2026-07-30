namespace gtas_vpp_fe.Components.Pages.Permission.Models;

public enum PermissionUiAccessState
{
    Hidden,
    ReadOnly,
    Enabled
}

public sealed class PermissionUiMappingEditModel
{
    public Guid PageComponentMappingId { get; init; }
    public string ComponentCode { get; init; } = string.Empty;
    public string ComponentName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActionGrant { get; init; }
    public bool CanConfigure { get; init; }
    public string AdministrationMode { get; init; } = string.Empty;
    public PermissionUiAccessState OriginalAccessState { get; init; }
    public PermissionUiAccessState AccessState { get; set; }
    public bool IsDirty => AccessState != OriginalAccessState;
}

public sealed class PermissionUiPageEditModel
{
    public Guid PageId { get; init; }
    public string PageCode { get; init; } = string.Empty;
    public string PageName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public List<PermissionUiMappingEditModel> Components { get; init; } = [];
}
