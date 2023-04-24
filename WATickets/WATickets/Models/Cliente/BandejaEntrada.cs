namespace WATickets.Models.Cliente
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("BandejaEntrada")]
    public partial class BandejaEntrada
    {
        public int Id { get; set; }

        public DateTime FechaIngreso { get; set; }

        [StringLength(1)]
        public string Procesado { get; set; }

        public DateTime? FechaProcesado { get; set; }

        public string Mensaje { get; set; }

        public string Asunto { get; set; }

        public string Remitente { get; set; }

        public string Texto { get; set; }
        public byte[] Adjuntos { get; set; }
        public string TipoAdjunto { get; set; } 
    }
}
