namespace gtas_vpp_fe.Helpers.DTOs.Req
{
    public class sp_Authentication_LoginReqDTO
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public bool isRememberPass { get; set; }
        public string? selected_server { get; set; }
    }
}
