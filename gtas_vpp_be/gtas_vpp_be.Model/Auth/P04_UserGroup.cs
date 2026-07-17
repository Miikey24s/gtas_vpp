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
    [Table("P04_UserGroup")]
    public class P04_UserGroup : BaseModel
    {
        public required int UserId { get; set; }
        public int? AccountId { get; set; }
        public P02_Group? P02_Group { get; set; }
        public Guid P02_GroupId { get; set; }
        public Guid LEX02_CompanyDepartmentLocationId { get; set; }
        public LEX02_CompanyDepartmentLocation LEX02_CompanyDepartmentLocation { get; set; } = default!;

        [Timestamp]
        public byte[] RowVersion { get; set; } = [];

        public P04_UserGroup() { }
    }
}
