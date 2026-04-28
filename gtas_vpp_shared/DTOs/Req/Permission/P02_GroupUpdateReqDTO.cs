namespace gtas_vpp_shared.DTOs.Req.Permission
{
    public class P02_GroupUpdateReqDTO
    {
        public string? GroupName { get; set; }
        public string? Description { get; set; }
        public Guid? ParentGroupId { get; set; }
        public bool IsDeleted { get; set; }

        public int UpdateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}
