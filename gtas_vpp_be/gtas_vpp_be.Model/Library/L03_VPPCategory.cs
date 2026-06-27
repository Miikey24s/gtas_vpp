using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L03_VPPCategory")]
    public class L03_VPPCategory : BaseModel
    {
        public string? VPPCategoryCode { get; set; }
        public string? VPPCategoryName { get; set; }
        public virtual ICollection<L04_VPP>? VPPs { get; set; }
        public L03_VPPCategory() { }
    }
}
