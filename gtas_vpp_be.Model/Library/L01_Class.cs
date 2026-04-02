using gtas_vpp_be.Model.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Library
{
    [StructLayout(LayoutKind.Auto)]
    [Table("L01_Class")]
    public class L01_Class : BaseModel
    {
        public string? ClassCode { get; set; }
        [StringLength(200)]
        public string? ClassName { get; set; }
        [StringLength(200)]
        public string? ClassModul { get; set; }
        public virtual ICollection<L02_ClassDetail>? L02_ClassDetails { get; set; }
        public L01_Class() { }
        [NotMapped]
        public string? CreateUserName { get; set; }
        [NotMapped]
        public string? UpdateUserName { get; set; }
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
