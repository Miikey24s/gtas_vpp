using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L02_ClassDetail")]
    public class L02_ClassDetail : BaseModel
    {
        public L01_Class? Class { get; set; }
        public Guid? ClassId { get; set; }
        public required string ClassDetailCode { get; set; }
        public required string ClassDetailValue { get; set; }
        public string? ExtraField1 { get; set; }
        public string? ExtraField2 { get; set; }
        public string? ExtraField3 { get; set; }
        public int Sort { get; set; }
        public virtual ICollection<L04_VPP>? VPPs_UOM { get; set; }
        public L02_ClassDetail() {}
        [NotMapped]
        public string? CreateUserName { get; set; }
        [NotMapped]
        public string? UpdateUserName { get; set; }
    }
}