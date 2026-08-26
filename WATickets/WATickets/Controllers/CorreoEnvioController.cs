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
    public class CorreoEnvioController : ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {

                var Correos = db.CorreoEnvio.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Correos = Correos.Where(a => a.RecepcionEmail.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }



                return Request.CreateResponse(HttpStatusCode.OK, Correos);

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

        [Route("api/CorreoEnvio/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {



                var Correo = db.CorreoEnvio.Where(a => a.id == id).FirstOrDefault();


                if (Correo == null)
                {
                    throw new Exception("Este correo no se encuentra registrado");
                }

                return Request.CreateResponse(HttpStatusCode.OK, Correo);
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
        public HttpResponseMessage Post([FromBody] CorreoEnvio correo)
        {
            try
            {


                var Correo = db.CorreoEnvio.Where(a => a.id == correo.id).FirstOrDefault();

                if (Correo == null)
                {
                    Correo = new CorreoEnvio();
                    Correo.RecepcionEmail = correo.RecepcionEmail;
                    Correo.RecepcionPassword = correo.RecepcionPassword;
                    Correo.RecepcionHostName = correo.RecepcionHostName;
                    Correo.RecepcionUseSSL = correo.RecepcionUseSSL;
                    Correo.EnvioPort = correo.EnvioPort;

                    db.CorreoEnvio.Add(Correo);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Este correo  YA existe");
                }


                return Request.CreateResponse(HttpStatusCode.OK, Correo);
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
        [Route("api/CorreoEnvio/Actualizar")]
        public HttpResponseMessage Put([FromBody] CorreoEnvio correo)
        {
            try
            {


                var Correo = db.CorreoEnvio.Where(a => a.id == correo.id).FirstOrDefault();

                if (Correo != null)
                {
                    db.Entry(Correo).State = EntityState.Modified;
                    Correo.RecepcionEmail = correo.RecepcionEmail;
                    Correo.RecepcionPassword = correo.RecepcionPassword;
                    Correo.RecepcionHostName = correo.RecepcionHostName;
                    Correo.RecepcionUseSSL = correo.RecepcionUseSSL;
                    Correo.EnvioPort = correo.EnvioPort;
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Correo no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, Correo);
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

        [HttpDelete]
        [Route("api/CorreoEnvio/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {


                var Correo = db.CorreoEnvio.Where(a => a.id == id).FirstOrDefault();

                if (Correo != null)
                {


                    db.CorreoEnvio.Remove(Correo);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Correo no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK);
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