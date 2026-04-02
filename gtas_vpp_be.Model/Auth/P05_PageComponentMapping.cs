using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Auth
{
    [StructLayout(LayoutKind.Auto)]
    [Table("P05_PageComponentMapping")]
    public class P05_PageComponentMapping
    {
        public Guid Id { get; set; }
        public P01_Page? P01_Page { get; set; }
        public Guid P01_PageId { get; set; }
        public P03_Component? P03_Component { get; set; }
        public Guid P03_ComponentId { get; set; }
        public virtual ICollection<P06_GroupPageComponentMapping>? P06_GroupPageComponentMappings { get; set; }
        public P05_PageComponentMapping() { }
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
