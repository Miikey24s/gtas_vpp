using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_fe.Helpers.DTOs.Res
{
    [StructLayout(LayoutKind.Auto)]
    [NotMapped]
    public class sp_Authentication_GetPermissionSinglePage
    {
        [Key]
        public Guid PageId { get; set; }
        public string? PageCode { get; set; }
        public string? PageName { get; set; }
        public string? PageDesctiprion { get; set; }
        public List<childModel_Authentication_GetPermissionSinglePage_Component> List_Component { get; set; } = new List<childModel_Authentication_GetPermissionSinglePage_Component>();
        public sp_Authentication_GetPermissionSinglePage() { }
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
    [StructLayout(LayoutKind.Auto)]
    [Keyless]
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
                    //Do your unmanaged disposing here
                    _isDisposed = true;
                }
            }
        }
    }
}
