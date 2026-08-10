using gtas_vpp_be.Model.Helpers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("PriceLists")]
    public class PriceList : BaseModel
    {
        [StringLength(50)]
        public string? PriceListCode { get; set; }

        [StringLength(200)]
        public string? PriceListName { get; set; }

        public bool IsDefault { get; set; }

        public Guid? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public int Version { get; set; } = 1;
        public DateTime EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public PriceListStatus Status { get; set; } = PriceListStatus.Draft;
        [StringLength(3)]
        public string CurrencyCode { get; set; } = "VND";
        [StringLength(32)]
        public string VatPolicy { get; set; } = "item-rate";
        [StringLength(128)]
        public string? ContractCode { get; set; }
        [StringLength(64)]
        public string? LegacyBackfillStatus { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountRate { get; set; }
        [Column(TypeName = "decimal(19,4)")]
        public decimal RebateAmount { get; set; }
        [Column(TypeName = "decimal(19,4)")]
        public decimal FeeAmount { get; set; }
        [Column(TypeName = "decimal(19,4)")]
        public decimal ShippingAmount { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public int? PublishedByUserId { get; set; }
        public DateTime? ExpiredAtUtc { get; set; }
        public int? ExpiredByUserId { get; set; }
        [StringLength(500)]
        public string? StatusReason { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        public virtual ICollection<SupplierProductMapping>? SupplierProductMappings { get; set; }
        public virtual ICollection<PriceListImportBatch>? ImportBatches { get; set; }

        public PriceList() { }
    }
}
