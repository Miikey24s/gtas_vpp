using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.VPP
{
    [StructLayout(LayoutKind.Auto)]
    [Table("Requests")]
    public class VppRequest : BaseModel
    {
        public string? VppCode { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }

        // Aggregate identity and immutable revision lineage. PeriodId remains
        // nullable only for legacy rows that could not be backfilled safely.
        public Guid? PeriodId { get; set; }
        public VppPeriod? Period { get; set; }
        public Guid RequestSeriesId { get; set; }
        public int RevisionNumber { get; set; } = 1;
        public bool IsCurrentRevision { get; set; } = true;
        public Guid? SupersedesRequestId { get; set; }
        public Guid? SupersededByRequestId { get; set; }

        // Supplement provenance and policy counters. BaseRequestSeriesId is
        // the stable quota key while BaseRequestId captures the exact regular
        // revision used when the supplement was submitted.
        public Guid? BaseRequestId { get; set; }
        public Guid? BaseRequestSeriesId { get; set; }
        public int? SupplementSequence { get; set; }
        public int? SupplementAttemptNumber { get; set; }
        public string? SupplementReason { get; set; }

        // Status & Workflow
        public int Status { get; set; } = (int)VPPStatus.Submitted;
        public string? DepartmentCode { get; set; }
        public string? MemberCompanyCode { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public int? ApprovedById { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? RejectedById { get; set; }
        public DateTime? RejectedAt { get; set; }
        public string? RejectReason { get; set; }

        public int? CancelledById { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }

        // Optional command idempotency contract. Old clients may omit it;
        // current clients send one key per create/update/cancel command.
        public string? IdempotencyKey { get; set; }
        public string? CommandPayloadHash { get; set; }

        public bool IsAdditionalOrder { get; set; } = false;

        public DateTime? SettledAt { get; set; }
        public int? SettledByUserId { get; set; }
        public Guid? SettledByPriceListId { get; set; }
        public PriceList? SettledByPriceList { get; set; }

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        public virtual ICollection<VppRequestDetail> RequestDetails { get; set; } = new List<VppRequestDetail>();
        public virtual ICollection<RequestLog> RequestLogs { get; set; } = new List<RequestLog>();
        public VppRequest() { }
    }
}
