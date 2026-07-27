using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("Suppliers")]
    public class Supplier : BaseModel, ITranslatableBusinessEntity
    {
        public string? SupplierShortName { get; set; }
        public string? SupplierName { get; set; }
        public string OriginalLanguageCode { get; set; } = "vi";
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Ward { get; set; }
        public string? City { get; set; }
        public virtual ICollection<SupplierProductMapping>? SupplierProductMappings { get; set; }
        public virtual ICollection<PriceList>? PriceLists { get; set; }
        public virtual ICollection<SupplierTranslation> Translations { get; set; } = [];
        public Supplier() { }
    }
}
