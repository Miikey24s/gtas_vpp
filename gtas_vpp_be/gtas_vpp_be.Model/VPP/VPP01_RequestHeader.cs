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
    [Table("VPP01_RequestHeader")]
    public class VPP01_RequestHeader : BaseModel
    {
        public string? VPPCode { get; set; }
        public int Y { get; set; }
        public int M { get; set; }

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

        public bool IsAdditionalOrder { get; set; } = false;

        public DateTime? SettledAt { get; set; }
        public int? SettledByUserId { get; set; }
        public Guid? SettledByPriceListId { get; set; }
        public L07_PriceList? SettledByPriceList { get; set; }

        [System.ComponentModel.DataAnnotations.Timestamp]
        public byte[]? RowVersion { get; set; }

        public virtual ICollection<VPP02_RequestDetail> VPP02_RequestDetails { get; set; } = new List<VPP02_RequestDetail>();
        public virtual ICollection<VPP03_Log> VPP03_Logs { get; set; } = new List<VPP03_Log>();
        public VPP01_RequestHeader() { }
    }
}
