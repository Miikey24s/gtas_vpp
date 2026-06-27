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
    [Table("LEX01_User")]
    public class LEX01_User : BaseModel
    {
        public int UserId { get; set; }
        public string? UserLogin { get; set; }
        public string? FullName { get; set; }
        public string? PasswordChar { get; set; }
        public string? Email { get; set; }
    }
}
