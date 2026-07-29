namespace gtas_vpp_fe.Components.Pages.Permission.Models;

public sealed class UserMembershipEditModel
{
    public int AccountId { get; set; }
    public Guid GroupId { get; set; }
    public Guid PrimaryDepartmentId { get; set; }
    public string? ExpectedRowVersion { get; set; }
    public string? Reason { get; set; }
}
