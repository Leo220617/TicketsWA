 

namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Adjuntos")]
    public partial class Adjuntos
    {
        public int id { get; set; }
        public int idTicket { get; set; }
       
        public string Adjunto { get; set; }
    }
}