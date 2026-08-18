using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WATickets.Models.Cliente
{
    public class Respuestas
    {
        public int id { get; set; }
        public int idTicket { get; set; }
        public int? idUsuario { get; set; }
        public string Respuesta { get; set; }
        public bool EsNotaInterna { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}