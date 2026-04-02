using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Service.Helpers
{
    [StructLayout(LayoutKind.Auto)]
    [Keyless]
    public class sp_ResDTO
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMess { get; set; }
        public string? ResData { get; set; }
        public sp_ResDTO() { }
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
