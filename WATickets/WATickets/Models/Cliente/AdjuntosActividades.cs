using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("AdjuntosActividades")]
    public class AdjuntosActividades
    {
        public int id { get; set; }

        public int idActividad { get; set; }

        public string Adjunto { get; set; }

    }
}