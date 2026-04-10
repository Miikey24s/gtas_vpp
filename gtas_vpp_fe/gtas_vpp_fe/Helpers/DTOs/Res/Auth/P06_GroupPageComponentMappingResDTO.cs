namespace gtas_vpp_fe.Helpers.DTOs.Res.Auth
{
    public class P06_GroupPageComponentMappingResDTO
    {
        public Guid P05_PageComponentMappingId { get; set; }
        public Guid P02_GroupId { get; set; }
        public int CreateUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public long MemberCompanyCode { get; set; } = 77500;
    }
}