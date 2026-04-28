using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class LEX01_UserResDTO : BaseResDTO
    {
        public int UserId { get; set; }
        public string? UserLogin { get; set; }
        public string? FullName { get; set; }
        public string? PasswordChar { get; set; }
        public string? Email { get; set; }
    }
}
