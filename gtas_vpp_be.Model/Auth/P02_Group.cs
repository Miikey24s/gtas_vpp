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
    [Table("P02_Group")]
    public class P02_Group : BaseModel
    {
        [Required]
        [StringLength(50)]
        public string? GroupName { get; set; }
        public Guid? ParentGroupId { get; set; }

        public virtual ICollection<P04_UserGroup>? P04_UserGroups { get; set; }
        public virtual ICollection<P06_GroupPageComponentMapping>? P06_GroupPageComponentMapping { get; set; }

        [NotMapped]
        public string? CreateUserName { get; set; }
        [NotMapped]
        public string? UpdateUserName { get; set; }
        public P02_Group() { }
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
