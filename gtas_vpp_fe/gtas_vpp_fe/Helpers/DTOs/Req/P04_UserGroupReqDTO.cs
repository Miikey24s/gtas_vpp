using gtas_vpp_fe.Helpers.DTOs.Share;

namespace gtas_vpp_fe.Helpers.DTOs.Req
{
    public class P04_UserGroupReqDTO : BaseReqDTO
    {
        public int UserId { get; set; }
        public Guid P02_GroupId { get; set; }
    }
}