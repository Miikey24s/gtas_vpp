namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class UserAdministrationResDTO
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string? UserLogin { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? GoogleEmail { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public string? GroupName { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? CreatedAtUtc { get; set; }
        public string? CreatedByUserName { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public string? UpdatedByUserName { get; set; }
        public bool IsDeleted { get; set; }
        public string? UserType { get; set; }
        public string? Description { get; set; }
        public PermissionGroupResDTO? UserGroup { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? AccountStatus { get; set; }
        public long SessionVersion { get; set; }
        public string? GroupCode { get; set; }
        public bool IsActive { get; set; }
        public byte[]? RowVersion { get; set; }
    }
}
