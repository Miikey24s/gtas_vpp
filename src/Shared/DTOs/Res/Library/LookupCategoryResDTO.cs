using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class LookupCategoryResDTO : BaseResDTO
    {
        public string? Code { get; set; }

        public string? Name { get; set; }

        public string? ModuleName { get; set; }

        public string? CreatedByUserName { get; set; }

        public string? UpdatedByUserName { get; set; }

        public int ValueCount { get; set; }

        public virtual ICollection<LookupValueResDTO>? LookupValues { get; set; }
    }
}
