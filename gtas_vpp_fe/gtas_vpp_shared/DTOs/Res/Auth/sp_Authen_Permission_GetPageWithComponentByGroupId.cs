namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class sp_Authen_Permission_GetPageWithComponentByGroupId
    {
        public Guid GroupId { get; set; }
        public Guid PageId { get; set; }
        public string? PageName { get; set; }
        public string? Description { get; set; }
        public string? PageCode { get; set; }
        public int CreateUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public string? CreateUserName { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime UpdateDate { get; set; }
        public string? UpdateUserName { get; set; }
        public bool IsDeleted { get; set; }
        public List<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component> List_Component { get; set; } = new List<sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component>();

    }
    public class sp_Authen_Permission_GetPageWithComponentByGroupId_List_Component
    {
        public Guid ComponentId { get; set; }
        public string? ComponentCode { get; set; }
        public string? ComponentName { get; set; }
        public bool IsVisible { get; set; } = false;
        public bool IsEnable { get; set; } = false;
        public Guid PageId { get; set; }
        public Guid GroupId { get; set; }
        public Guid GroupPageComponentMappingId { get; set; }
        public string? Description { get; set; }
        public long MemberCompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyShortName { get; set; }
        public bool IsDeleted { get; set; }
    }
}

