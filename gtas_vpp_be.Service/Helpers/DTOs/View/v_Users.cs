using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace gtas_vpp_be.Service.Helpers.DTOs.Res
{
    //[NotMapped]
    [Keyless]
    public class v_Users
    {
        public int UserID { get; set; }
        public string? UserLogin { get; set; }
        public string? PasswordChar { get; set; }
        public string? FullName { get; set; }
        public string? EmailAddress1 { get; set; }
        public string? EmailAddress2 { get; set; }
        public string? GoogleEmail { get; set; }
        public string? PhoneNo1 { get; set; }
        public string? PhoneNo2 { get; set; }
        public v_Users()
        {
            //UserID = 4519;
            //UserLogin = "google";
            //PasswordChar = "wiSEc6nf/dK/Vu0E738j8Q==";
            //FullName = "Google";
            //EmailAddress1 = "google@ppj-international.com";
        }
    }
}
