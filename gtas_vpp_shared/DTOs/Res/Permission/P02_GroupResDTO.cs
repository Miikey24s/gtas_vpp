namespace gtas_vpp_shared.DTOs.Res.Permission
{
    public class P02_GroupResDTO
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public int CreateUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool IsDeleted { get; set; }

        public string? CreateUserName { get; set; }
        public string? UpdateUserName { get; set; }
        public decimal MemberCompanyCode { get; set; }
        public string? GroupName { get; set; }
        public Guid? ParentGroupId { get; set; }
    }
}
