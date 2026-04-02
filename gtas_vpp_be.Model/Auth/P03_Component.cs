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
    [Table("P03_Component")]
    public class P03_Component : BaseModel
    {
        [StringLength(150)]
        public required string ComponentName { get; set; }
        [StringLength(50)]
        public required string ComponentCode { get; set; }
        public virtual ICollection<P05_PageComponentMapping>? P05_PageComponentMappings { get; set; }

        [NotMapped]
        public string? CreateUserName { get; set; }
        [NotMapped]
        public string? UpdateUserName { get; set; }
        public P03_Component() { }
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
