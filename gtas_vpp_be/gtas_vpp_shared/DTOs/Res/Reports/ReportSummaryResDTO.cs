namespace gtas_vpp_shared.DTOs.Res.Reports;

public static class ReportScopes
{
    public const string Own = "own";
    public const string Department = "department";
    public const string All = "all";

    public static bool IsValid(string? scope) => scope is Own or Department or All;
}

public sealed class ReportSummaryResDTO
{
    public string Scope { get; set; } = ReportScopes.Own;
    public int? Year { get; set; }
    public int? Month { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<int> AvailableYears { get; set; } = [];
    public int TotalOrders { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalRequesters { get; set; }
    public int TotalLines { get; set; }
    public int TotalQuantity { get; set; }
    public long TotalAmount { get; set; }
    public List<ReportPeriodPointResDTO> PeriodTrend { get; set; } = [];
    public List<ReportStatusPointResDTO> StatusBreakdown { get; set; } = [];
    public List<ReportDepartmentPointResDTO> DepartmentBreakdown { get; set; } = [];
    public List<ReportProductPointResDTO> TopProducts { get; set; } = [];
}

public sealed class ReportPeriodPointResDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Period { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public long TotalAmount { get; set; }
}

public sealed class ReportStatusPointResDTO
{
    public int Status { get; set; }
    public string ResourceKey { get; set; } = string.Empty;
    public int OrderCount { get; set; }
}

public sealed class ReportDepartmentPointResDTO
{
    public string DepartmentCode { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public int TotalQuantity { get; set; }
    public long TotalAmount { get; set; }
}

public sealed class ReportProductPointResDTO
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public long TotalAmount { get; set; }
}

public sealed record ReportExportResult(byte[] Content, string FileName, string ContentType);
