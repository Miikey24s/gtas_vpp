namespace gtas_vpp_shared.DTOs.Req.Permission
{
    public class PatchComponentMappingReqDTO
    {
        public Guid PageComponentMappingId { get; set; }
        public Guid PermissionGroupId { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
