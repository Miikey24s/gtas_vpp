using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using gtas_vpp_be.Model.Library;

namespace gtas_vpp_be.Model.AI;

[Table("AI_VPPEmbedding")]
public class AI_VPPEmbedding
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public Guid VPPId { get; set; }

    [ForeignKey(nameof(VPPId))]
    public L04_VPP VPP { get; set; } = null!;

    [Required]
    public string EmbeddingText { get; set; } = string.Empty;

    [Required]
    public byte[] EmbeddingVector { get; set; } = [];

    [Required]
    [MaxLength(100)]
    public string ModelName { get; set; } = string.Empty;

    public int VectorDimension { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
