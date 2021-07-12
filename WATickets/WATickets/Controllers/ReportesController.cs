using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using WATickets.Models;
using S22.Imap;
using WATickets.Models.Cliente;

namespace WATickets.Controllers
{
    [Authorize]
    public class ReportesController: ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                var time = new DateTime();
                var Tiquetes = db.Tickets.Where(a => (filtro.FechaInicial != time ? a.FechaTicket >= filtro.FechaInicial && a.FechaTicket <= filtro.FechaFinal : true)).ToList();

               
                if (filtro.Codigo1 > 0)
                {
                    Tiquetes = Tiquetes.Where(a => a.idLoginAsignado == filtro.Codigo1).ToList();
                }


                return Request.CreateResponse(HttpStatusCode.OK, Tiquetes);

            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}