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
    [Table("P01_Page")]
    public class P01_Page : BaseModel
    {
        [StringLength(50)]
        public required string PageCode { get; set; }
        [StringLength(250)]
        public string? PageName { get; set; }
        [StringLength(50)]
        public required string Type { get; set; }
        public virtual ICollection<P05_PageComponentMapping>? P05_PageComponentMappings { get; set; }

        public P01_Page() { }
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
