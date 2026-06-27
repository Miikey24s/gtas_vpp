namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L06_VPPItemPriceResDTO
    {
        public Guid VPPId { get; set; }
        public string? VPPCode { get; set; }
        public string? VPPName { get; set; }
        public string? CategoryName { get; set; }
        public string? UOMName { get; set; }
        public Guid? PriceMappingId { get; set; }
        public decimal? Price { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }
        public string? Description { get; set; }
    }
}
