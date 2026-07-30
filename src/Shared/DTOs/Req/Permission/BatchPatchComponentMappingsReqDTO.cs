namespace gtas_vpp_shared.DTOs.Req.Permission;

public sealed class BatchPatchComponentMappingsReqDTO
{
    public Guid PermissionGroupId { get; set; }
    public string? Reason { get; set; }
    public List<BatchPatchComponentMappingItemReqDTO> Items { get; set; } = [];
}

public sealed class BatchPatchComponentMappingItemReqDTO
{
    public Guid PageComponentMappingId { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnable { get; set; }
}
