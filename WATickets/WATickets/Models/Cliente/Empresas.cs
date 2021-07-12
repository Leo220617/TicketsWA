 

namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Empresas")]
    public class Empresas
    {
        public int id { get; set; }
        public string Nombre { get; set; }
        public string Dominio { get; set; }

    }
}