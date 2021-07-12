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
    public class EmpresasController: ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {

                var Empresas = db.Empresas.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Empresas = Empresas.Where(a => a.Nombre.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }



                return Request.CreateResponse(HttpStatusCode.OK, Empresas);

            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Empresas/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {



                var Empresa = db.Empresas.Where(a => a.id == id).FirstOrDefault();


                if (Empresa == null)
                {
                    throw new Exception("Esta empresa no se encuentra registrado");
                }

                return Request.CreateResponse(HttpStatusCode.OK, Empresa);
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Empresas empresa)
        {
            try
            {


                var Empresa = db.Empresas.Where(a => a.id == empresa.id).FirstOrDefault();

                if (Empresa == null)
                {
                    Empresa = new Empresas();
                    Empresa.Nombre = empresa.Nombre;
                    Empresa.Dominio = empresa.Dominio;
                   


                    db.Empresas.Add(Empresa);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Esta empresa  YA existe");
                }


                return Request.CreateResponse(HttpStatusCode.OK, Empresa);
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Empresas/Actualizar")]
        public HttpResponseMessage Put([FromBody] Empresas empresa)
        {
            try
            {


                var Empresa = db.Empresas.Where(a => a.id == empresa.id).FirstOrDefault();

                if (Empresa != null)
                {
                    db.Entry(Empresa).State = EntityState.Modified;
                    Empresa.Nombre = empresa.Nombre;
                    Empresa.Dominio = empresa.Dominio;

                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Empresa no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, Empresa);
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpDelete]
        [Route("api/Empresas/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {


                var Empresa = db.Empresas.Where(a => a.id == id).FirstOrDefault();

                if (Empresa != null)
                {


                    db.Empresas.Remove(Empresa);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Empresa no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
    }
}