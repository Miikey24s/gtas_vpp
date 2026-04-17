using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.VPP
{
    [StructLayout(LayoutKind.Auto)]
    [Table("VPP01_RequestHeader")]
    public class VPP01_RequestHeader : BaseModel
    {
        public string? VPPCode { get; set; }
        public int Y { get; set; }
        public int M { get; set; }

        // Status & Workflow
        public int Status { get; set; } = (int)VPPStatus.Draft;
        public string? DepartmentCode { get; set; }
        public string? MemberCompanyCode { get; set; }
        public DateTime? SubmittedDate { get; set; }

        public virtual ICollection<VPP02_RequestDetail> VPP02_RequestDetails { get; set; }
        public VPP01_RequestHeader() { }
        [NotMapped]
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
