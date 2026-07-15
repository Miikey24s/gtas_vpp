namespace gtas_vpp_be.Model.View;

/// <summary>
/// Read-only projection over the application company view.
/// Persistence mapping is configured in VPPContext.
/// </summary>
public class v_WFXCompany
{
    public long MemberCompanyCode { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyShortName { get; set; }
}
