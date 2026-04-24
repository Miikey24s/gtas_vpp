using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L06_VPPSupplierMapping")]
    public class L06_VPPSupplierMapping : BaseModel
    {
        [Column(TypeName = "bigint")]
        public decimal Price { get; set; }
        public Guid L04_VPPId { get; set; }
        public L04_VPP? L04_VPP { get; set; }
        public Guid L05_VPPSupplierId { get; set; }
        public L05_VPPSupplier? L05_VPPSupplier { get; set; }
        public L06_VPPSupplierMapping() { }
    }
}
