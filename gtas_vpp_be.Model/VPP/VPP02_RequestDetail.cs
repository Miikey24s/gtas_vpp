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
    [Table("VPP02_RequestDetail")]
    public class VPP02_RequestDetail : BaseModel
    {
        public Guid VPPId { get; set; }
        public L04_VPP VPP { get; set; }
        public int Qty { get; set; }
        [Column(TypeName = "bigint")]
        public long CurrentSinglePrice { get; set; }
        public Guid VPP01_RequestHeaderId { get; set; }
        public VPP01_RequestHeader VPP01_RequestHeader { get; set; }
        public VPP02_RequestDetail() { }
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
