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
    [Table("RequestDetails")]
    public class VppRequestDetail : BaseModel
    {
        public Guid VppId { get; set; }
        public VppItem VppItem { get; set; } = default!;
        public int Qty { get; set; }
        [Column(TypeName = "bigint")]
        public long CurrentSinglePrice { get; set; }
        public Guid RequestId { get; set; }
        public VppRequest Request { get; set; } = default!;
        public VppRequestDetail() { }
    }
}
