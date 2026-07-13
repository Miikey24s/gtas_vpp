namespace gtas_vpp_shared.DTOs.Res.Auth;

public sealed class PermissionSnapshotResDTO
{
    public long Version { get; set; }
    public Guid GroupId { get; set; }
    public long? MemberCompanyCode { get; set; }
    public List<string> Permissions { get; set; } = new();
    public List<PermissionPageResDTO> Pages { get; set; } = new();
}

public sealed class PermissionPageResDTO
{
    public string PageCode { get; set; } = string.Empty;
    public List<PermissionComponentResDTO> Components { get; set; } = new();
}

public sealed class PermissionComponentResDTO
{
    public string ComponentCode { get; set; } = string.Empty;
    public bool IsVisible { get; set; }
    public bool IsEnable { get; set; }
}
