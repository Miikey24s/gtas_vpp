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
        public L04_VPP VPP { get; set; } = default!;
        public int Qty { get; set; }
        [Column(TypeName = "bigint")]
        public long CurrentSinglePrice { get; set; }
        public Guid VPP01_RequestHeaderId { get; set; }
        public VPP01_RequestHeader VPP01_RequestHeader { get; set; } = default!;
        public VPP02_RequestDetail() { }
    }
}
