using gtas_vpp_be.Model.Helpers;
using gtas_vpp_be.Model.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Auth
{
    [StructLayout(LayoutKind.Auto)]
    [Table("UserGroupMemberships")]
    public class UserGroupMembership : BaseModel
    {
        public required int UserId { get; set; }
        public int? AccountId { get; set; }
        public PermissionGroup? PermissionGroup { get; set; }
        public Guid PermissionGroupId { get; set; }
        public Guid DepartmentId { get; set; }
        public Department Department { get; set; } = default!;

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        public UserGroupMembership() { }
    }
}
