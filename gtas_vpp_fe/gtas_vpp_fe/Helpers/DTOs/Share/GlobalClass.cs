using gtas_vpp_fe.Helpers.DTOs.Res.Auth;

namespace gtas_vpp_fe.Helpers.DTOs.Share
{
    public class GlobalClass
    {
        public sp_Authentication_Login UserInfo = new sp_Authentication_Login();
        public string? Server { get; set; }
        public bool isBusyPage { get; set; } = false;
        public string BaseUrl { get; set; } = string.Empty;
    }
}
