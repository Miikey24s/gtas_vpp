using gtas_vpp_shared.DTOs.Res.Reports;

namespace gtas_vpp_be.Service.Services;

public sealed record ReportQueryContext(
    string Scope,
    int UserId,
    string DepartmentCode,
    string MemberCompanyCode,
    int? Year,
    int? Month)
{
    public void Validate()
    {
        if (!ReportScopes.IsValid(Scope))
        {
            throw new ArgumentException("Report scope is invalid.", nameof(Scope));
        }

        if (string.IsNullOrWhiteSpace(MemberCompanyCode))
        {
            throw new ArgumentException("Member company is required.", nameof(MemberCompanyCode));
        }

        if (Scope == ReportScopes.Department && string.IsNullOrWhiteSpace(DepartmentCode))
        {
            throw new ArgumentException(
                "Department is required for department reports.",
                nameof(DepartmentCode));
        }

        if (Year is < 2000 or > 2100)
        {
            throw new ArgumentOutOfRangeException(nameof(Year));
        }

        if (Month is < 1 or > 12)
        {
            throw new ArgumentOutOfRangeException(nameof(Month));
        }
    }
}
