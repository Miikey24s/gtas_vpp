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
    [Table("PermissionComponents")]
    public class PermissionComponent : BaseModel
    {
        [StringLength(150)]
        public required string ComponentName { get; set; }
        [StringLength(50)]
        public required string ComponentCode { get; set; }
        public virtual ICollection<PageComponentMapping>? PageComponentMappings { get; set; }

        [NotMapped]
        public string? CreatedByUserName { get; set; }
        [NotMapped]
        public string? UpdatedByUserName { get; set; }
        public PermissionComponent() { }
    }
}
