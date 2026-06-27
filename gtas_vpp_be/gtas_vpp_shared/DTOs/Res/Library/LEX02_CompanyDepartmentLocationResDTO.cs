using gtas_vpp_shared.DTOs.Res.Auth;
using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class LEX02_CompanyDepartmentLocationResDTO : BaseResDTO
    {
        public string? LEX02Code { get; set; }
        public string? LEX02Name { get; set; }
        public string? LEX02Type { get; set; }
        public Guid? ParentId { get; set; }
        public ICollection<P04_UserGroupResDTO>? P04_UserGroups { get; set; }
    }
}
