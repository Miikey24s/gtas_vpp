using gtas_vpp_shared.DTOs.Share;
namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class PermissionGroupResDTO : BaseResDTO
    {
        public string? CreatedByUserName { get; set; }
        public string? UpdatedByUserName { get; set; }
        public decimal MemberCompanyCode { get; set; }
        public string? GroupName { get; set; }
        public Guid? ParentGroupId { get; set; }
        public List<PermissionPageComponentResDTO> Permissions { get; set; } = [];
    }
}
