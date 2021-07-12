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
using WATickets.Models.Cliente;
namespace WATickets.Controllers
{
    [Authorize]
    public class SeguridadModulosController : ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                
                var modulos = db.SeguridadModulos.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    modulos = modulos.Where(a => a.Descripcion.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }



               
                return Request.CreateResponse(HttpStatusCode.OK, modulos);

            }
            catch (Exception ex)
            {
               
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/SeguridadModulos/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                

                var Rol = db.SeguridadModulos.Where(a => a.CodModulo == id).FirstOrDefault();

               
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
               
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }


    }
}