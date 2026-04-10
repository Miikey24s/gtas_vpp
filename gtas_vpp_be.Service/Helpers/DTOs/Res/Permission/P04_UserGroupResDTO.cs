namespace gtas_vpp_be.Service.Helpers.DTOs.Res.Permission
{
    public class P04_UserGroupResDTO
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public Guid P02_GroupId { get; set; }
        public Guid LEX02_CompanyDepartmentLocationId { get; set; }
        public bool IsDeleted { get; set; }
    }
}