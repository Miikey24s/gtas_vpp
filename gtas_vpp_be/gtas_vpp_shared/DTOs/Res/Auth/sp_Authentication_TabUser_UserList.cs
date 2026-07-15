namespace gtas_vpp_shared.DTOs.Res.Auth
{
    public class sp_Authentication_TabUser_UserList
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }
        public string? UserLogin { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? GoogleEmail { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public string? GroupName { get; set; }
        public int CreateUserId { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? CreateUserName { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime? UpdateDate { get; set; }
        public string? UpdateUserName { get; set; }
        public bool IsDeleted { get; set; }
        public string? TypeOfUser { get; set; }
        public string? Description { get; set; }
        public P02_GroupResDTO? UserGroup { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? L05_DepartmentId { get; set; }
    }
}
