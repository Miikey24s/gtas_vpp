using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Auth
{
    [StructLayout(LayoutKind.Auto)]
    [Table("PageComponentMappings")]
    public class PageComponentMapping
    {
        public Guid Id { get; set; }
        public PermissionPage? PermissionPage { get; set; }
        public Guid PermissionPageId { get; set; }
        public PermissionComponent? PermissionComponent { get; set; }
        public Guid PermissionComponentId { get; set; }
        public virtual ICollection<GroupPageComponentMapping>? GroupPageComponentMappings { get; set; }
        public PageComponentMapping() { }
    }
}
