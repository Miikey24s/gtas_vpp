using gtas_vpp_fe.Helpers.DTOs.Share;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_fe.Helpers.DTOs.Res.Auth
{
    public class P02_GroupResDTO : BaseResDTO
    {
        public string? CreateUserName { get; set; }
        public string? UpdateUserName { get; set; }
        public decimal MemberCompanyCode { get; set; }
        public string? GroupName { get; set; }
        public Guid? ParentGroupId { get; set; }
        public List<sp_Authen_Permission_GetPageWithComponentByGroupId> Permission { get; set; } = new List<sp_Authen_Permission_GetPageWithComponentByGroupId> { };
    }
}
    