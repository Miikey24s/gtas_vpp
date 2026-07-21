using gtas_vpp_shared.DTOs.Res.Auth;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class DepartmentResDTO : LocalizedBusinessDataResDTO
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public Guid? ParentDepartmentId { get; set; }
        public ICollection<UserGroupMembershipResDTO>? UserGroupMemberships { get; set; }
    }
}
