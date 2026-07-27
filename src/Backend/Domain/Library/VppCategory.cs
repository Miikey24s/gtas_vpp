using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("VppCategories")]
    public class VppCategory : BaseModel, ITranslatableBusinessEntity
    {
        public string? VppCategoryCode { get; set; }
        public string? VppCategoryName { get; set; }
        public string OriginalLanguageCode { get; set; } = "vi";
        public virtual ICollection<VppItem>? VppItems { get; set; }
        public virtual ICollection<VppCategoryTranslation> Translations { get; set; } = [];
        public VppCategory() { }
    }
}
