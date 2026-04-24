using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L05_VPPSupplier")]
    public class L05_VPPSupplier : BaseModel
    {
        public string? SupplierShortName { get; set; }
        public string? SupplierName { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public virtual ICollection<L06_VPPSupplierMapping>? L06_VPPSupplierMappings { get; set; }
        public L05_VPPSupplier() { }
    }
}
