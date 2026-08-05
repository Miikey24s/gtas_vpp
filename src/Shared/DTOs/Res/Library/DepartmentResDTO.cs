using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class DepartmentResDTO : BaseResDTO
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public Guid? ParentDepartmentId { get; set; }
        public ICollection<UserGroupMembershipResDTO>? UserGroupMemberships { get; set; }
    }
}
