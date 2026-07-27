using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.Notifications;

[Table("EmailOutboxMessages")]
public sealed class EmailOutboxMessage
{
    [Key]
    public Guid Id { get; set; }

    [MaxLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    [MaxLength(320)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(128)]
    public string DeduplicationKey { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string TextBody { get; set; } = string.Empty;

    public string? HtmlBody { get; set; }

    [MaxLength(24)]
    public string Status { get; set; } = "Pending";

    public int AttemptCount { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }

    [MaxLength(1000)]
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
