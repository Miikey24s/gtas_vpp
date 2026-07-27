namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class GroupPageComponentMappingResDTO
    {
        public Guid PageComponentMappingId { get; set; }
        public Guid PermissionGroupId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public long MemberCompanyCode { get; set; } = 77500;
    }
}
