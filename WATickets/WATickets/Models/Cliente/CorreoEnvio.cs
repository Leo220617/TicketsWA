using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WATickets.Models.Cliente
{
    [Table("CorreoEnvio")]
    public class CorreoEnvio
    {
        public int id { get; set; }
        public string RecepcionHostName { get; set; }
        public int EnvioPort { get; set; }
        public bool RecepcionUseSSL { get; set; }
        public string RecepcionEmail { get; set; }
        public string RecepcionPassword { get; set; }
    }
}