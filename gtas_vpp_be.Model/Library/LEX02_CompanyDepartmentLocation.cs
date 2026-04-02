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
    [Table("LEX02_CompanyDepartmentLocation")]
    public class LEX02_CompanyDepartmentLocation : BaseModel
    {
        public string? LEX02Code { get; set; }
        public string? LEX02Name { get; set; }
        public string LEX02Type { get; set; }
        public Guid? ParentId { get; set; }
        public virtual ICollection<P04_UserGroup>? P04_UserGroups { get; set; }
        [NotMapped]
        private bool _isDisposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_isDisposed)
                {
                    //Do your unmanaged disposing here
                    _isDisposed = true;
                }
            }
        }
    }
}
