using gtas_vpp_be.Model.Helpers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L07_PriceList")]
    public class L07_PriceList : BaseModel
    {
        [StringLength(50)]
        public string? PriceListCode { get; set; }

        [StringLength(200)]
        public string? PriceListName { get; set; }

        public bool IsDefault { get; set; }

        public virtual ICollection<L06_VPPSupplierMapping>? L06_VPPSupplierMappings { get; set; }

        public L07_PriceList() { }
    }
}
