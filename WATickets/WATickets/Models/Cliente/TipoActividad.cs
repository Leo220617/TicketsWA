 
namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("TiposActividad")]
    public class TiposActividad
    {
        public int id { get; set; }

        public string nombre { get; set; }
    }
}