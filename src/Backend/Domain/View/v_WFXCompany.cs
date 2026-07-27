namespace gtas_vpp_be.Model.View;

/// <summary>
/// Projection chỉ đọc trên view công ty của ứng dụng.
/// Ánh xạ persistence được cấu hình trong VPPContext.
/// </summary>
public class v_WFXCompany
{
    public long MemberCompanyCode { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyShortName { get; set; }
}
