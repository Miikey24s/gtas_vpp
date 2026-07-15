namespace gtas_vpp_be.Model.View;

/// <summary>
/// Read-only projection over the application-owned user view.
/// Persistence mapping is configured in VPPContext.
/// </summary>
public class v_Users
{
    public int UserID { get; set; }
    public string? UserLogin { get; set; }
    public string? FullName { get; set; }
    public string? EmailAddress1 { get; set; }
    public string? EmailAddress2 { get; set; }
    public string? GoogleEmail { get; set; }
    public string? PhoneNo1 { get; set; }
    public string? PhoneNo2 { get; set; }
    public string? MemberCompanyName { get; set; }
    public string? DepartmentCode { get; set; }
}
