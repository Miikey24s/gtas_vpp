namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class PermissionPageComponentResDTO
    {
        public Guid GroupId { get; set; }
        public Guid PageId { get; set; }
        public string? PageName { get; set; }
        public string? Description { get; set; }
        public string? PageCode { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? CreatedByUserName { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string? UpdatedByUserName { get; set; }
        public bool IsDeleted { get; set; }
        public List<PermissionComponentAccessResDTO> Components { get; set; } = new List<PermissionComponentAccessResDTO>();

    }
    public class PermissionComponentAccessResDTO
    {
        public Guid ComponentId { get; set; }
        public string? ComponentCode { get; set; }
        public string? ComponentName { get; set; }
        public bool IsVisible { get; set; } = false;
        public bool IsEnable { get; set; } = false;
        public Guid PageId { get; set; }
        public Guid GroupId { get; set; }
        public Guid GroupPageComponentMappingId { get; set; }
        public string? Description { get; set; }
        public long MemberCompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyShortName { get; set; }
        public bool IsDeleted { get; set; }
    }
}

