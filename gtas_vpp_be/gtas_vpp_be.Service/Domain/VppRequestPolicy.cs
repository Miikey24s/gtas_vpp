using Microsoft.Extensions.Configuration;

namespace gtas_vpp_be.Service.Domain;

/// <summary>
/// Validated, application-owned limits for the request/supplement workflow.
/// Keeping these values in one immutable object makes the same policy
/// available to services, period creation and tests instead of duplicating
/// magic numbers in controllers or the UI.
/// </summary>
public sealed class VppRequestPolicy
{
    public const int DefaultDeadlineDay = 5;
    public const int DefaultSupplementApprovalGraceDays = 2;
    public const int DefaultMaxApprovedSupplements = 1;
    public const int DefaultMaxSupplementAttempts = 6;

    public VppRequestPolicy(
        int deadlineDay = DefaultDeadlineDay,
        int supplementApprovalGraceDays = DefaultSupplementApprovalGraceDays,
        int maxApprovedSupplements = DefaultMaxApprovedSupplements,
        int maxSupplementAttempts = DefaultMaxSupplementAttempts)
    {
        Validate(
            deadlineDay,
            supplementApprovalGraceDays,
            maxApprovedSupplements,
            maxSupplementAttempts);

        DeadlineDay = deadlineDay;
        SupplementApprovalGraceDays = supplementApprovalGraceDays;
        MaxApprovedSupplements = maxApprovedSupplements;
        MaxSupplementAttempts = maxSupplementAttempts;
    }

    public int DeadlineDay { get; }

    public int SupplementApprovalGraceDays { get; }

    public TimeSpan SupplementApprovalGrace =>
        TimeSpan.FromDays(SupplementApprovalGraceDays);

    public int MaxApprovedSupplements { get; }

    public int MaxSupplementAttempts { get; }

    public static VppRequestPolicy FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection("VPP");
        var deadlineDay = section.GetValue<int?>("DeadlineDay")
            ?? configuration.GetValue("VPPDeadlineDay", DefaultDeadlineDay);
        var approvalGraceDays = section.GetValue(
            "SupplementApprovalGraceDays", DefaultSupplementApprovalGraceDays);
        var maxApproved = section.GetValue(
            "MaxApprovedSupplements", DefaultMaxApprovedSupplements);
        var maxAttempts = section.GetValue(
            "MaxSupplementAttempts", DefaultMaxSupplementAttempts);

        return new VppRequestPolicy(
            deadlineDay,
            approvalGraceDays,
            maxApproved,
            maxAttempts);
    }

    public static void Validate(
        int deadlineDay,
        int supplementApprovalGraceDays,
        int maxApprovedSupplements,
        int maxSupplementAttempts)
    {
        if (deadlineDay is < 1 or > 28)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deadlineDay), deadlineDay,
                "DeadlineDay must be between 1 and 28.");
        }

        if (supplementApprovalGraceDays < 0 || supplementApprovalGraceDays > 31)
        {
            throw new ArgumentOutOfRangeException(
                nameof(supplementApprovalGraceDays), supplementApprovalGraceDays,
                "SupplementApprovalGraceDays must be between 0 and 31.");
        }

        if (maxApprovedSupplements < 1 || maxApprovedSupplements > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxApprovedSupplements), maxApprovedSupplements,
                "MaxApprovedSupplements must be between 1 and 100.");
        }

        if (maxSupplementAttempts < 1 || maxSupplementAttempts > 1_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxSupplementAttempts), maxSupplementAttempts,
                "MaxSupplementAttempts must be between 1 and 1000.");
        }
    }
}
