namespace gtas_vpp_shared.DTOs.Req.Library
{
    public class L07_PriceListCloneReqDTO
    {
        public Guid SourceId { get; set; }
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
