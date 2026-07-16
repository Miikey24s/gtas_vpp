using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace gtas_vpp_be.Model.VPP
{
    [Table("VPP03_Log")]
    public class VPP03_Log
    {
        [Key]
        public Guid Id { get; set; }

        public Guid VPP01_RequestHeaderId { get; set; }

        public string? LogJS { get; set; }

        public DateTime LogDate { get; set; }

        [MaxLength(255)]
        public string? LogTitle { get; set; }

        public int? ActorUserId { get; set; }

        [MaxLength(50)]
        public string? MemberCompanyCode { get; set; }

        [MaxLength(64)]
        public string? Action { get; set; }

        public int? RevisionNumber { get; set; }

        [MaxLength(128)]
        public string? CorrelationId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [ForeignKey("VPP01_RequestHeaderId")]
        public virtual VPP01_RequestHeader VPP01_RequestHeader { get; set; } = default!;
    }
}
