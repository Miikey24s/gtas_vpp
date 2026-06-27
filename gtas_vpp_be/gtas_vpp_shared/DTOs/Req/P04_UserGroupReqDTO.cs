using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Req
{
    public class P04_UserGroupReqDTO : BaseReqDTO
    {
        public int UserId { get; set; }
        public Guid P02_GroupId { get; set; }
    }
}
