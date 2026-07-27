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
    [Table("PermissionPages")]
    public class PermissionPage : BaseModel
    {
        [StringLength(50)]
        public required string PageCode { get; set; }
        [StringLength(250)]
        public string? PageName { get; set; }
        [StringLength(50)]
        public required string Type { get; set; }
        public virtual ICollection<PageComponentMapping>? PageComponentMappings { get; set; }

        public PermissionPage() { }
    }
}
