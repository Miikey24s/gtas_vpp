using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text;

namespace gtas_vpp_be.Model.Auth
{
    [StructLayout(LayoutKind.Auto)]
    [Table("P06_GroupPageComponentMapping")]
    public class P06_GroupPageComponentMapping
    {
        public P05_PageComponentMapping? P05_PageComponentMapping { get; set; }
        public Guid P05_PageComponentMappingId { get; set; }
        public P02_Group? P02_Group { get; set; }
        public Guid P02_GroupId { get; set; }
        public int CreateUserId { get; set; }
        public DateTime CreateDate { get; set; }
        public int UpdateUserId { get; set; }
        public DateTime UpdateDate { get; set; }
        public bool IsEnable { get; set; }
        public bool IsVisible { get; set; }
        public long MemberCompanyCode { get; set; } = 77500;
        public P06_GroupPageComponentMapping() { }
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
