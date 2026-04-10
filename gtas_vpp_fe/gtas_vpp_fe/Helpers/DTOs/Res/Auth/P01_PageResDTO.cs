using gtas_vpp_fe.Helpers.DTOs.Share;

namespace gtas_vpp_fe.Helpers.DTOs.Res.Auth
{
    public class P01_PageResDTO : BaseResDTO
    {
        public string? PageCode { get; set; }
        public string? PageName { get; set; }
        public string? Type { get; set; }
    }
}