using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class PermissionPageResDTO : BaseResDTO
    {
        public string? PageCode { get; set; }
        public string? PageName { get; set; }
        public string? Type { get; set; }
    }
}
