using Microsoft.EntityFrameworkCore;

namespace gtas_vpp_shared.DTOs.Res
{
    [Keyless]
    public class v_WFXCompany
    {
        public long MemberCompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyShortName { get; set; }
    }
}
