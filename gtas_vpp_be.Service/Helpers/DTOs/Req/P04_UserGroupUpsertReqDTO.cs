namespace gtas_vpp_be.Service.Helpers.DTOs.Req.Permission
{
    public class P04_UserGroupUpsertReqDTO
    {
        public int UserId { get; set; }
        public Guid P02_GroupId { get; set; }
        public Guid? LEX02_CompanyDepartmentLocationId { get; set; }

        public bool IsDeleted { get; set; }

        public int CreateUserId { get; set; }
        public DateTime? CreateDate { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
    }
}