using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace gtas_vpp_shared.DTOs.Res.Auth
{
    [StructLayout(LayoutKind.Auto)]
    public class sp_Authentication_Login
    {
        public int UserID { get; set; }
        public string? UserLogin { get; set; } = string.Empty;
        [JsonIgnore]
        public string? PasswordChar { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? GoogleEmail { get; set; }
        public bool IsAdmin { get; set; }
        public Guid GroupId { get; set; }
        public string? GroupName { get; set; }
        public string? MemberCompanyCode { get; set; }
        public string? MemberCompanyName { get; set; }
        public string? MemberCompanyShortName { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentCode { get; set; }
        public string? AccessToken { get; set; }
        public List<sp_Authentication_GetPermissionSinglePage> List_PagePermission { get; set; } = new List<sp_Authentication_GetPermissionSinglePage>();
        public sp_Authentication_Login() { }
        private bool _isDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    //Do your unmanaged disposing here
                    _isDisposed = true;
                }
            }
        }
    }
}
