 

namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Actividades")]
    public class Actividades
    {

        public int id { get; set; }

        public int idUsuario { get; set; }
        public int idEmpresa { get; set; }
        public int idTipoActividad { get; set; }

        public string titulo { get; set; }
        public DateTime fechaAgendada { get; set; }
        public string comentario { get; set; }

        public DateTime fechaCreacion { get; set; }  

        public string estado { get; set; }
    }
}