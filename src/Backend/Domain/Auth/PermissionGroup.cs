using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Auth
{
    [StructLayout(LayoutKind.Auto)]
    [Table("PermissionGroups")]
    public class PermissionGroup : BaseModel
    {
        [Required]
        [StringLength(50)]
        public string GroupCode { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string? GroupName { get; set; }
        public Guid? ParentGroupId { get; set; }

        public virtual ICollection<UserGroupMembership>? UserGroupMemberships { get; set; }
        public virtual ICollection<GroupPageComponentMapping>? GroupPageComponentMappings { get; set; }

        [NotMapped]
        public string? CreatedByUserName { get; set; }
        [NotMapped]
        public string? UpdatedByUserName { get; set; }
        public PermissionGroup() { }
    }
}
