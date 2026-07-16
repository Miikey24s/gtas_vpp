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
    [Table("L06_VPPSupplierMapping")]
    public class L06_VPPSupplierMapping : BaseModel
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
        public Guid L04_VPPId { get; set; }
        public L04_VPP? L04_VPP { get; set; }
        public Guid L05_VPPSupplierId { get; set; }
        public L05_VPPSupplier? L05_VPPSupplier { get; set; }
        public Guid L07_PriceListId { get; set; }
        public L07_PriceList? L07_PriceList { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; } = [];
        public L06_VPPSupplierMapping() { }
    }
}
