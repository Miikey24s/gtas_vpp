namespace gtas_vpp_shared.DTOs.Req.Permission
{
    public class PermissionGroupUpdateReqDTO
    {
        public string? GroupName { get; set; }
        public string? Description { get; set; }
        public Guid? ParentGroupId { get; set; }
        public bool IsDeleted { get; set; }

        public int UpdatedByUserId { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}
