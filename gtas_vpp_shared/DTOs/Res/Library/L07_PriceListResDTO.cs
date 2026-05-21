using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class L07_PriceListResDTO : BaseResDTO
    {
        public string? PriceListCode { get; set; }
        public string? PriceListName { get; set; }
        public bool IsDefault { get; set; }
        public int ItemCount { get; set; }
        public string? CreateUserName { get; set; }
        public string? UpdateUserName { get; set; }
    }
}
