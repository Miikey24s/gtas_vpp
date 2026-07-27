using System.Runtime.InteropServices;

namespace gtas_vpp_shared.DTOs.Res.Auth
{
    [StructLayout(LayoutKind.Auto)]
    public class PagePermissionResDTO
    {
        public Guid PageId { get; set; }
        public string? PageCode { get; set; }
        public string? PageName { get; set; }
        public string? PageDesctiprion { get; set; }
        public List<childModel_Authentication_GetPermissionSinglePage_Component> Components { get; set; } = new List<childModel_Authentication_GetPermissionSinglePage_Component>();
        public PagePermissionResDTO() { }
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
    [StructLayout(LayoutKind.Auto)]
    public class childModel_Authentication_GetPermissionSinglePage_Component
    {
        public Guid ComponentId { get; set; }
        public string? ComponentCode { get; set; }
        public string? ComponentName { get; set; }
        public string? ComponentDescription { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public string? Role { get; set; }
        public string? Policy { get; set; }
        public childModel_Authentication_GetPermissionSinglePage_Component() { }
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
