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
    [EnableCors("*", "*", "*")]
    public class LoginController : ApiController
    {
        ModelCliente db = new ModelCliente();


        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
              
                var Login = db.Login.ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Login = Login.Where(a => a.Nombre.ToUpper().Contains(filtro.Texto.ToUpper()) || a.Email.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }


               
                return Request.CreateResponse(HttpStatusCode.OK, Login);

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

        [Route("api/Login/Consultar")]
        public async Task<HttpResponseMessage> GetOne([FromUri] int id)
        {
            try
            {
              
                var Login = db.Login.Where(a => a.id == id).FirstOrDefault();

                if (Login == null)
                {
                    throw new Exception("Usuario no existe");
                }


                
                return Request.CreateResponse(HttpStatusCode.OK, Login);

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

        [Route("api/Login/Conectar")]
        public async Task<HttpResponseMessage> GetLoginAsync([FromUri] string email, string clave)
        {
            try
            {
                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(clave))
                {
                    var Usuario = db.Login.Where(a => a.Email.ToUpper().Contains(email.ToUpper())).FirstOrDefault();

                    if (Usuario == null)
                    {
                        throw new Exception("Usuario o clave incorrecta");
                    }

                    if (!BCrypt.Net.BCrypt.Verify(clave, Usuario.Clave))
                    {
                        throw new Exception("Clave o Usuario incorrectos");
                    }
                    if(!Usuario.Activo.Value)
                    {
                        throw new Exception("Usuario desactivado");
                    }
                    var token = TokenGenerator.GenerateTokenJwt(Usuario.Nombre, Usuario.id.ToString());
                    var SeguridadModulos = db.SeguridadRolesModulos.Where(a => a.CodRol == Usuario.idRol).ToList();


                    DevolcionLogin de = new DevolcionLogin();
                    de.id = Usuario.id;
                    de.Nombre = Usuario.Nombre;
                    de.idRol = Usuario.idRol;
                    de.Clave = "";
                    de.Activo = Usuario.Activo;
                    de.Email = Usuario.Email;
                    de.token = token;
                    de.Seguridad = SeguridadModulos;

                    return Request.CreateResponse(HttpStatusCode.OK, de);

                }
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Debe incluir usuario y clave");
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
        public HttpResponseMessage Post([FromBody] Login usuario)
        {
            try
            {
                var Usuario = db.Login.Where(a => a.Email.ToUpper().Contains(usuario.Email.ToUpper())).FirstOrDefault();

                if(Usuario != null)
                {
                    throw new Exception("Usuario ya existe");

                }

                Usuario = new Login();
                Usuario.Nombre = usuario.Nombre;
                Usuario.Email = usuario.Email;
                Usuario.Activo = true;
                Usuario.idRol = usuario.idRol;
                Usuario.Clave = BCrypt.Net.BCrypt.HashPassword(usuario.Clave);

                db.Login.Add(Usuario);
                db.SaveChanges();
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

        //Actualiza la contraseña del usuario 
        [HttpPut]
        [Route("api/Login/Actualizar")]
        public HttpResponseMessage Put([FromBody] Login usuario)
        {
            try
            {
                var Usuario = db.Login.Where(a => a.id == usuario.id).FirstOrDefault();

                if(Usuario == null)
                {
                    throw new Exception("El usuario no existe");
                }

                db.Entry(Usuario).State = System.Data.Entity.EntityState.Modified;

                if(!string.IsNullOrEmpty(usuario.Nombre))
                {
                    Usuario.Nombre = usuario.Nombre;
                }

                if(!string.IsNullOrEmpty(usuario.Clave))
                {
                    Usuario.Clave = BCrypt.Net.BCrypt.HashPassword(usuario.Clave);
                }
                
                if(Usuario.idRol != usuario.idRol && usuario.idRol != null)
                {
                    Usuario.idRol = usuario.idRol;
                }

                if(Usuario.Activo != usuario.Activo && usuario.Activo != null)
                {
                    Usuario.Activo = usuario.Activo;
                }

                if(!string.IsNullOrEmpty(usuario.Email))
                {
                    Usuario.Email = usuario.Email;
                }
                
                db.SaveChanges();

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


        [HttpDelete]
        [Route("api/Login/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                

                var User = db.Login.Where(a => a.id == id).FirstOrDefault();
                 


                if ( User != null)
                {

                    db.Entry(User).State = EntityState.Modified;
                   

                    if (User.Activo.Value)
                    {
                       
                        User.Activo = false;

                    }
                    else
                    {

                        User.Activo = true;
                    }



                     
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Usuario no existe");
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


   

    public class DevolcionLogin
    {
        public int id { get; set; }

        public int? idRol { get; set; }

        
        public string Email { get; set; }

        
        public string Nombre { get; set; }

        public bool? Activo { get; set; }

        
        public string Clave { get; set; }
        public string token { get; set; }
        public List<SeguridadRolesModulos> Seguridad { get; set; }
    }
}