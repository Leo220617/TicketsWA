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
    public class RolesController : ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
               
                var Roles = db.Roles.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Roles = Roles.Where(a => a.NombreRol.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }


                
                return Request.CreateResponse(HttpStatusCode.OK, Roles);

            }
            catch (Exception ex)
            {
              
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [Route("api/Roles/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {
                


                var Rol = db.Roles.Where(a => a.idRol == id).FirstOrDefault();


                if (Rol == null)
                {
                    throw new Exception("Este rol no se encuentra registrado");
                }
               
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
               
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPost]
        public HttpResponseMessage Post([FromBody] Roles rol)
        {
            try
            {
               

                var Rol = db.Roles.Where(a => a.idRol == rol.idRol).FirstOrDefault();

                if (Rol == null)
                {
                    Rol = new Roles();
                    Rol.NombreRol = rol.NombreRol;



                    db.Roles.Add(Rol);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Este rol  YA existe");
                }

               
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
               
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpPut]
        [Route("api/Roles/Actualizar")]
        public HttpResponseMessage Put([FromBody] Roles rol)
        {
            try
            {
               

                var Rol = db.Roles.Where(a => a.idRol == rol.idRol).FirstOrDefault();

                if (Rol != null)
                {
                    db.Entry(Rol).State = EntityState.Modified;
                    Rol.NombreRol = rol.NombreRol;

                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Rol no existe");
                }
               
                return Request.CreateResponse(HttpStatusCode.OK, Rol);
            }
            catch (Exception ex)
            {
               
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }

        [HttpDelete]
        [Route("api/Roles/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
               

                var Rol = db.Roles.Where(a => a.idRol == id).FirstOrDefault();

                if (Rol != null)
                {


                    db.Roles.Remove(Rol);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Rol no existe");
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