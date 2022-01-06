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
    public class AdjuntosController : ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                if(filtro != null)
                {
                    if(filtro.Codigo1 != 0)
                    {
                        var Adjuntos = db.Adjuntos.Where(a => a.idTicket == filtro.Codigo1).ToList();



                        return Request.CreateResponse(HttpStatusCode.OK, Adjuntos);
                    }
                    else
                    {
                        throw new Exception("No vienen parametros correctos");
                    }
                }else
                {
                    throw new Exception("No vienen parametros correctos");
                }
                
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


        [HttpPost]
        public HttpResponseMessage Post([FromBody] Adjuntos[] t)
        {
            var tr = db.Database.BeginTransaction();
            try
            {
                var idTicket = t.FirstOrDefault().idTicket;
                var Adjuntos = db.Adjuntos.Where(a => a.idTicket == idTicket).ToList();

                foreach(var item in Adjuntos)
                {
                    db.Adjuntos.Remove(item);
                    db.SaveChanges();
                }

                foreach( var item in t)
                {
                    if(!string.IsNullOrEmpty(item.Adjunto))
                    {
                        db.Adjuntos.Add(item);
                        db.SaveChanges();
                    }
                    
                }

                tr.Commit();
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                tr.Rollback();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}