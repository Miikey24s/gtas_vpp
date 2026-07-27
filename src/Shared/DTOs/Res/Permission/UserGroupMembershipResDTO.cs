namespace gtas_vpp_shared.DTOs.Res.Permission
{
    public class UserGroupMembershipResDTO
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public Guid PermissionGroupId { get; set; }
        public Guid DepartmentId { get; set; }
        public bool IsDeleted { get; set; }
    }
}
