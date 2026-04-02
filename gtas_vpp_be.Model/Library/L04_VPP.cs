using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L04_VPP")]
    public class L04_VPP : BaseModel
    {
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public Guid UOMId { get; set; }
        public L02_ClassDetail UOM { get; set; }
        public Guid VPPCategoryId { get; set; }
        public L03_VPPCategory? VPPCategory { get; set; }
        public virtual ICollection<L06_VPPSupplierMapping>? L06_VPPSupplierMappings { get; set; }
        public L04_VPP() { }
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
