namespace gtas_vpp_shared.DTOs.Res.Permission
{
    public class PermissionGroupResDTO
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsDeleted { get; set; }

        public string? CreatedByUserName { get; set; }
        public string? UpdatedByUserName { get; set; }
        public decimal MemberCompanyCode { get; set; }
        public string? GroupCode { get; set; }
        public string? GroupName { get; set; }
        public Guid? ParentGroupId { get; set; }
    }
}
