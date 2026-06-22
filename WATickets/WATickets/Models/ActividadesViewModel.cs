using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using WATickets.Models.Cliente;

namespace WATickets.Models
{
    public class ActividadesViewModel
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
        public List<AdjuntosActividades> adjuntos_actividades { get; set; }

    }
}