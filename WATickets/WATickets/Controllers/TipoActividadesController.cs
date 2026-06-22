using Newtonsoft.Json;
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
    public class TipoActividadesController : ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {

                var TipoActividad = db.TiposActividad.ToList();

                 



                return Request.CreateResponse(HttpStatusCode.OK, TipoActividad);

            }
            catch (Exception ex)
            {
                BitacoraErrores bt = new BitacoraErrores();
                bt.Descripcion = ex.Message;
                bt.StackTrace = ex.StackTrace;
                bt.Fecha = DateTime.Now;
                bt.JSON = JsonConvert.SerializeObject(ex);
                db.BitacoraErrores.Add(bt);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/TipoActividades/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {



                var TipoActividad = db.TiposActividad.Where(a => a.id == id).FirstOrDefault();


                if (TipoActividad == null)
                {
                    throw new Exception("Este TipoActividad no se encuentra registrado");
                }

                return Request.CreateResponse(HttpStatusCode.OK, TipoActividad);
            }
            catch (Exception ex)
            {
                BitacoraErrores bt = new BitacoraErrores();
                bt.Descripcion = ex.Message;
                bt.StackTrace = ex.StackTrace;
                bt.Fecha = DateTime.Now;
                bt.JSON = JsonConvert.SerializeObject(ex);
                db.BitacoraErrores.Add(bt);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] TiposActividad ta)
        {
            try
            {


                var TA = db.TiposActividad.Where(a => a.id == ta.id).FirstOrDefault();

                if (TA == null)
                {
                    TA = new TiposActividad();
                    TA.nombre = ta.nombre; 
                     
                    db.TiposActividad.Add(TA);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Este TipoActividad  YA existe");
                }


                return Request.CreateResponse(HttpStatusCode.OK, TA);
            }
            catch (Exception ex)
            {
                BitacoraErrores bt = new BitacoraErrores();
                bt.Descripcion = ex.Message;
                bt.StackTrace = ex.StackTrace;
                bt.Fecha = DateTime.Now;
                bt.JSON = JsonConvert.SerializeObject(ex);
                db.BitacoraErrores.Add(bt);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/TipoActividades/Actualizar")]
        public HttpResponseMessage Put([FromBody] TiposActividad ta)
        {
            try
            {


                var TA = db.TiposActividad.Where(a => a.id == ta.id).FirstOrDefault();

                if (TA != null)
                {
                    db.Entry(TA).State = EntityState.Modified;
                    TA.nombre = ta.nombre; 
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("TipoActividad no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, TA);
            }
            catch (Exception ex)
            {
                BitacoraErrores bt = new BitacoraErrores();
                bt.Descripcion = ex.Message;
                bt.StackTrace = ex.StackTrace;
                bt.Fecha = DateTime.Now;
                bt.JSON = JsonConvert.SerializeObject(ex);
                db.BitacoraErrores.Add(bt);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}