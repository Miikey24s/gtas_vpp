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

        public Guid? SupplierId { get; set; }
        public L05_VPPSupplier? Supplier { get; set; }
        public int Version { get; set; } = 1;
        public DateTime EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public L07_PriceListStatus Status { get; set; } = L07_PriceListStatus.Draft;
        [StringLength(3)]
        public string CurrencyCode { get; set; } = "VND";
        [StringLength(32)]
        public string VatPolicy { get; set; } = "item-rate";
        [StringLength(128)]
        public string? ContractCode { get; set; }
        [StringLength(64)]
        public string? LegacyBackfillStatus { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        public virtual ICollection<L06_VPPSupplierMapping>? L06_VPPSupplierMappings { get; set; }

        public L07_PriceList() { }
    }
}
