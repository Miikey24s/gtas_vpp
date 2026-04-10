using gtas_vpp_fe.Helpers.DTOs.Share;

namespace gtas_vpp_fe.Helpers.DTOs.Res.Auth
{
    public class P04_UserGroupResDTO : BaseResDTO
    {
        public int UserId { get; set; }
        public Guid P02_GroupId { get; set; }
        public Guid LEX02_CompanyDepartmentLocationId { get; set; }
        
    }
}