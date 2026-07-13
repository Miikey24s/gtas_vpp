using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.Notifications;

[Table("N01_Notification")]
public sealed class N01_Notification
{
    [Key]
    public Guid Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(50)]
    public string MemberCompanyCode { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(250)]
    public string? Route { get; set; }

    [MaxLength(100)]
    public string? CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
