using gtas_vpp_be.Model.Helpers;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("LookupValues")]
    public class LookupValue : BaseModel
    {
        public LookupCategory? Category { get; set; }
        public Guid? LookupCategoryId { get; set; }
        public required string Code { get; set; }
        public required string Value { get; set; }
        public string? ExtraField1 { get; set; }
        public string? ExtraField2 { get; set; }
        public string? ExtraField3 { get; set; }
        public int Sort { get; set; }
        public virtual ICollection<VppItem>? VppItemsByUom { get; set; }
        public LookupValue() { }
        [NotMapped]
        public string? CreatedByUserName { get; set; }
        [NotMapped]
        public string? UpdatedByUserName { get; set; }
    }
}
