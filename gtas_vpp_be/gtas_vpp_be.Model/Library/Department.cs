using gtas_vpp_be.Model.Auth;
using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("Departments")]
    public class Department : BaseModel, ITranslatableBusinessEntity
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string OriginalLanguageCode { get; set; } = "vi";
        public Guid? ParentDepartmentId { get; set; }
        public virtual Department? ParentDepartment { get; set; }
        public virtual ICollection<Department>? ChildDepartments { get; set; }
        public virtual ICollection<UserGroupMembership>? UserGroupMemberships { get; set; }
        public virtual ICollection<DepartmentTranslation> Translations { get; set; } = [];
    }
}
