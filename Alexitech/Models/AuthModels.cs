using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Alexitech.Models
{
    public class AuthModels
    {
        public class Index
        {
            [Required]
            public string State { get; set; }
        }

        public class Network
        {
            [Required]
            public string State { get; set; }
            [Required]
            public string Hostname { get; set; }
        }

        public class Login
        {
            [Required]
            public string State { get; set; }
            [Required]
            public string Hostname { get; set; }

            [Required]
            public string Username { get; set; }
            [Required]
            public string Password { get; set; }
        }
    }
}