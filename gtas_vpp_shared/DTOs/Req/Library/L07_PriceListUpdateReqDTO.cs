namespace gtas_vpp_shared.DTOs.Req.Library
{
    public class L07_PriceListUpdateReqDTO
    {
        public Guid Id { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
    }
}
