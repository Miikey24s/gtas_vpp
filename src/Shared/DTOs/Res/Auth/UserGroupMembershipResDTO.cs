using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class UserGroupMembershipResDTO : BaseResDTO
    {
        public int UserId { get; set; }
        public Guid PermissionGroupId { get; set; }
        public Guid DepartmentId { get; set; }
    }
}
