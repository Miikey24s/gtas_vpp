using gtas_vpp_shared.DTOs.Share;

namespace gtas_vpp_shared.DTOs.Res.Library
{
    public class LookupValueResDTO : BaseResDTO
    {
        public LookupCategoryResDTO? Category { get; set; }

        public Guid? LookupCategoryId { get; set; }

        public string? Code { get; set; }

        public string? Value { get; set; }

        public string? ExtraField1 { get; set; }

        public string? ExtraField2 { get; set; }

        public string? ExtraField3 { get; set; }

        public int Sort { get; set; }

        public ICollection<VppItemResDTO>? VppItemsByUom { get; set; }

        public string? CreatedByUserName { get; set; }

        public string? UpdatedByUserName { get; set; }
    }
}
