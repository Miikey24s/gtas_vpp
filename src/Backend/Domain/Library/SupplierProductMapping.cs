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
    [Table("SupplierProductMappings")]
    public class SupplierProductMapping : BaseModel
    {
        [Column(TypeName = "decimal(19,4)")]
        public decimal Price { get; set; }
        [Column(TypeName = "decimal(19,4)")]
        public decimal NetPrice { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; }
        [Column(TypeName = "decimal(19,4)")]
        public decimal MinimumOrderQuantity { get; set; }
        public int LeadTimeDays { get; set; }
        [StringLength(128)]
        public string? SupplierSku { get; set; }
        public bool IsDefault { get; set; }
        public Guid VppItemId { get; set; }
        public VppItem? VppItem { get; set; }
        public Guid SupplierId { get; set; }
        public Supplier? Supplier { get; set; }
        public Guid PriceListId { get; set; }
        public PriceList? PriceList { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
        public SupplierProductMapping() { }
    }
}
