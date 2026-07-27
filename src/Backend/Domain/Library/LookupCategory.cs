using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("LookupCategories")]
    public class LookupCategory : BaseModel, ITranslatableBusinessEntity
    {
        public string? Code { get; set; }
        [StringLength(200)]
        public string? Name { get; set; }
        [StringLength(200)]
        public string? ModuleName { get; set; }
        [StringLength(5)]
        public string OriginalLanguageCode { get; set; } = "vi";
        public virtual ICollection<LookupValue>? LookupValues { get; set; }
        public virtual ICollection<LookupCategoryTranslation> Translations { get; set; } = [];
        public LookupCategory() { }
        [NotMapped]
        public string? CreatedByUserName { get; set; }
        [NotMapped]
        public string? UpdatedByUserName { get; set; }
    }
}
