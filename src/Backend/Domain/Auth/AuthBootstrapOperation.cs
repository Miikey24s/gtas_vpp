using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.Auth;

/// <summary>
/// Sổ idempotency bền vững, không chứa secret, dành cho lần bootstrap owner đầu tiên.
/// </summary>
[Table("AuthBootstrapOperations")]
public sealed class AuthBootstrapOperation
{
    [Key, StringLength(128)]
    public string OperationKey { get; set; } = string.Empty;

    [Required, StringLength(64)]
    public string InputFingerprint { get; set; } = string.Empty;

    public int AccountId { get; set; }

    [Required, StringLength(32)]
    public string Status { get; set; } = string.Empty;

    public DateTime CompletedAtUtc { get; set; }
}
