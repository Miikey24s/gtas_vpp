using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Auth
{
    [StructLayout(LayoutKind.Auto)]
    [Table("GroupPageComponentMappings")]
    public class GroupPageComponentMapping
    {
        public PageComponentMapping? PageComponentMapping { get; set; }
        public Guid PageComponentMappingId { get; set; }
        public PermissionGroup? PermissionGroup { get; set; }
        public Guid PermissionGroupId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int UpdatedByUserId { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public long MemberCompanyCode { get; set; } = 77500;
        public GroupPageComponentMapping() { }
    }
}
