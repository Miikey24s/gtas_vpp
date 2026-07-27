using System.Runtime.InteropServices;

namespace gtas_vpp_shared.DTOs.Res.Auth
{
    [StructLayout(LayoutKind.Auto)]
    public class AuthenticationResultDTO
    {
        public int UserID { get; set; }
        public string? UserLogin { get; set; } = string.Empty;
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
        public DateTime? AccessTokenExpiresAtUtc { get; set; }
        public long SessionVersion { get; set; }
        public string? AccountStatus { get; set; }
        public bool MustChangePassword { get; set; }
        public List<PagePermissionResDTO> PagePermissions { get; set; } = new List<PagePermissionResDTO>();
        public AuthenticationResultDTO() { }
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
                    // Giải phóng tài nguyên unmanaged tại đây.
                    _isDisposed = true;
                }
            }
        }
    }
}
