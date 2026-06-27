namespace gtas_vpp_shared.DTOs.Req
{
    public class PatchComponentMappingReqDTO
    {
        public Guid P05_PageComponentMappingId { get; set; }
        public Guid P02_GroupId { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
