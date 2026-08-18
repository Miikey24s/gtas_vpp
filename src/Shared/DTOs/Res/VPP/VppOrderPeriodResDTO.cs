namespace gtas_vpp_shared.DTOs.Res.VPP;

public sealed class VppOrderPeriodSettingsResDTO
{
    public Guid? Id { get; set; }
    public int VersionNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultOpenPeriodCount { get; set; } = 1;
    public int DefaultNewPeriodOpenDay { get; set; } = 5;
    public int DefaultPeriodCloseDay { get; set; } = 5;
    public TimeSpan LocalTimeOfDay { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public int SupplementApprovalGraceDays { get; set; } = 5;
    public int PostCloseAdjustmentDays { get; set; } = 10;
    public int EffectiveFromYear { get; set; }
    public int EffectiveFromMonth { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class VppManagedPeriodResDTO
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string State { get; set; } = string.Empty;
    public string DisplayState { get; set; } = string.Empty;
    public DateTime OpenAtLocal { get; set; }
    public DateTime CloseAtLocal { get; set; }
    public DateTime SupplementApprovalDeadlineLocal { get; set; }
    public DateTime PostCloseAdjustmentDeadlineLocal { get; set; }
    public bool HasOrders { get; set; }
    public int OrderCount { get; set; }
    public bool CanEditSchedule { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExtendDeadline { get; set; }
    public bool CanCloseSubmissions { get; set; }
    public bool CanReopenSubmissions { get; set; }
    public bool CanAdjustOrders { get; set; }
    public bool CanCorrectSettledOrders { get; set; }
    public string? LastTransitionReason { get; set; }
    public int? LastTransitionUserId { get; set; }
    public DateTime? LastTransitionAtLocal { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class VppPeriodHorizonPreviewResDTO
{
    public VppOrderPeriodSettingsResDTO Settings { get; set; } = new();
    public int CurrentOpenCount { get; set; }
    public int MissingCount { get; set; }
    public IReadOnlyList<VppManagedPeriodResDTO> ExistingOpenPeriods { get; set; } = [];
    public IReadOnlyList<VppManagedPeriodResDTO> ProposedPeriods { get; set; } = [];
}

public sealed class VppOpenPeriodOptionResDTO
{
    public Guid PeriodId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DeadlineDate { get; set; }
    public bool IsAnchor { get; set; }
    public bool HasRegularOrder { get; set; }
}
