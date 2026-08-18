namespace gtas_vpp_shared.DTOs.Req.VPP;

public sealed class VppOrderPeriodSettingsReqDTO
{
    public string Name { get; set; } = string.Empty;
    public int DefaultOpenPeriodCount { get; set; } = 1;
    public int DefaultNewPeriodOpenDay { get; set; } = 5;
    public int DefaultPeriodCloseDay { get; set; } = 5;
    public TimeSpan LocalTimeOfDay { get; set; } = TimeSpan.Zero;
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public int SupplementApprovalGraceDays { get; set; } = 5;
    public int PostCloseAdjustmentDays { get; set; } = 10;
    public int EffectiveFromYear { get; set; }
    public int EffectiveFromMonth { get; set; }
}

public sealed class VppManagerOrderAdjustmentReqDTO
{
    public string Action { get; set; } = "Adjust";
    public string Reason { get; set; } = string.Empty;
    public string EmployeeNote { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
    public string? IdempotencyKey { get; set; }
    public List<PostSettlementOrderCorrectionItemReqDTO> Items { get; set; } = [];
}

public sealed class VppOrderPeriodManualCreateReqDTO
{
    public int Year { get; set; }
    public int Month { get; set; }
    public DateTime OpenAtLocal { get; set; }
    public DateTime CloseAtLocal { get; set; }
    public DateTime SupplementApprovalDeadlineLocal { get; set; }
    public int? SupplementApprovalGraceDays { get; set; }
    public int? PostCloseAdjustmentDays { get; set; }
    public string? Reason { get; set; }
}

public sealed class VppOrderPeriodUpdateReqDTO
{
    public DateTime OpenAtLocal { get; set; }
    public DateTime CloseAtLocal { get; set; }
    public DateTime SupplementApprovalDeadlineLocal { get; set; }
    public int? SupplementApprovalGraceDays { get; set; }
    public int? PostCloseAdjustmentDays { get; set; }
    public string? Reason { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class VppOrderPeriodCommandReqDTO
{
    public string? Reason { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class VppOrderPeriodExtendDeadlineReqDTO
{
    public DateTime CloseAtLocal { get; set; }
    public DateTime SupplementApprovalDeadlineLocal { get; set; }
    public int? SupplementApprovalGraceDays { get; set; }
    public int? PostCloseAdjustmentDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
}

public sealed class VppOrderPeriodReopenSubmissionsReqDTO
{
    public DateTime CloseAtLocal { get; set; }
    public DateTime SupplementApprovalDeadlineLocal { get; set; }
    public string Reason { get; set; } = string.Empty;
    public byte[]? RowVersion { get; set; }
}
