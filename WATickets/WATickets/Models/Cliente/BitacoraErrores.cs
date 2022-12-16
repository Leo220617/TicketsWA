 

namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("BitacoraErrores")]
    public partial class BitacoraErrores
    {
        public int id { get; set; }
        public string Descripcion { get; set; }
        public string StackTrace { get; set; }
        public DateTime Fecha { get; set; }
        public string JSON { get; set; }
    }
}