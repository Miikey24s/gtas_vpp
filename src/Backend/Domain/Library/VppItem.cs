using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("VppItems")]
    public class VppItem : BaseModel, ITranslatableBusinessEntity
    {
        public string? VppCode { get; set; }
        public string? VppName { get; set; }
        public string OriginalLanguageCode { get; set; } = "vi";
        public Guid UomId { get; set; }
        public LookupValue Uom { get; set; } = default!;
        public Guid VppCategoryId { get; set; }
        public VppCategory? VppCategory { get; set; }
        public virtual ICollection<SupplierProductMapping>? SupplierProductMappings { get; set; }
        public virtual ICollection<VppItemTranslation> Translations { get; set; } = [];
        public VppItem() { }
    }
}
