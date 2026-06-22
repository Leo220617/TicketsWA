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
    public class ActividadesController: ApiController
    {
        ModelCliente db = new ModelCliente();
        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                var time = new DateTime();
                var Actividades = db.Actividades.AsEnumerable()
                    .Where(a => (filtro.FechaInicial != time ? a.fechaAgendada >= filtro.FechaInicial && a.fechaAgendada <= filtro.FechaFinal : true) &&
                    (filtro.Codigo1 > 0 ? a.idUsuario == filtro.Codigo1 : true) && (filtro.Codigo2 > 0 ? a.idTipoActividad == filtro.Codigo2 : true)
                    )
                    .Select(a => new
                    {
                        a.id,
                        a.idUsuario,
                        NomUsuario = db.Login.Where(x => x.id == a.idUsuario).FirstOrDefault() == null ? "" : db.Login.Where(x => x.id == a.idUsuario).FirstOrDefault().Nombre,
                        a.idEmpresa, 
                        NomEmpresa = db.Empresas.Where(x => x.id == a.idEmpresa).FirstOrDefault() == null ? "" : db.Empresas.Where(x => x.id == a.idEmpresa).FirstOrDefault().Nombre,
                        a.idTipoActividad, 
                        NomActividad = db.TiposActividad.Where(x => x.id == a.idTipoActividad).FirstOrDefault() == null ? "" : db.TiposActividad.Where(x => x.id == a.idTipoActividad).FirstOrDefault().nombre,
                        a.titulo,
                        a.fechaAgendada,
                        a.fechaCreacion,
                        a.estado,
                        a.comentario,
                        tieneAdjunto = db.AdjuntosActividades.Where(x => x.idActividad == a.id).Any(),
                        color = a.estado == "Realizado" ? "#28a745" :
                                 a.estado == "Pendiente" ? "#ffc107" : "#dc3545",
                        start = a.fechaAgendada,
                        title = (db.TiposActividad.Where(x => x.id == a.idTipoActividad).FirstOrDefault() == null ? "" : db.TiposActividad.Where(x => x.id == a.idTipoActividad).FirstOrDefault().nombre)  + " - "+ (db.Login.Where(x => x.id == a.idUsuario).FirstOrDefault() == null ? "" : db.Login.Where(x => x.id == a.idUsuario).FirstOrDefault().Nombre) + " - " + (db.Empresas.Where(x => x.id == a.idEmpresa).FirstOrDefault() == null ? "" : db.Empresas.Where(x => x.id == a.idEmpresa).FirstOrDefault().Nombre) ,
                        extendedProps = new
                        {
                            tipo = a.idTipoActividad,
                            estado = a.estado,
                            comentario = a.titulo,
                            tieneAdjunto =  db.AdjuntosActividades.Where(x => x.idActividad == a.id).Any(),
                            NomActividad = db.TiposActividad.Where(x => x.id == a.idTipoActividad).FirstOrDefault() == null ? "" : db.TiposActividad.Where(x => x.id == a.idTipoActividad).FirstOrDefault().nombre,
                            start = a.fechaAgendada,
                            adjuntos = db.AdjuntosActividades.Where(x => x.idActividad == a.id).ToList()
                        }


                    } 
                  
                        )
                    .ToList();

               

                return Request.CreateResponse(HttpStatusCode.OK, Actividades);

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

        [Route("api/Actividades/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {



                var Actividades = db.Actividades.Where(a => a.id == id).FirstOrDefault();


                if (Actividades == null)
                {
                    throw new Exception("Este Actividades no se encuentra registrado");
                }

                return Request.CreateResponse(HttpStatusCode.OK, Actividades);
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
        public HttpResponseMessage Post([FromBody] ActividadesViewModel t)
        {
            try
            {


                var act = db.Actividades.Where(a => a.id == t.id).FirstOrDefault();

                if (act == null)
                {
                    act = new Actividades();

                    act.idUsuario = t.idUsuario;
                    act.idEmpresa = t.idEmpresa;
                    act.idTipoActividad = t.idTipoActividad;
                    act.titulo = t.titulo;
                    act.fechaAgendada = t.fechaAgendada;
                    act.fechaCreacion = DateTime.Now;
                    act.estado = "Pendiente";
                    act.comentario = t.comentario; 
                    db.Actividades.Add(act);
                    db.SaveChanges();


                    if (t.adjuntos_actividades == null)
                    {
                        t.adjuntos_actividades = new List<AdjuntosActividades>();
                    }
                    foreach (var adjunto in t.adjuntos_actividades)
                    {
                    
                        AdjuntosActividades adj = new AdjuntosActividades();
                        adj.idActividad = act.id;
                        adj.Adjunto = adjunto.Adjunto;
                        db.AdjuntosActividades.Add(adj);
                        db.SaveChanges();

                    }
                }
                else
                {
                    throw new Exception("actividad ya existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, act);
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
        [Route("api/Actividades/Actualizar")]
        public HttpResponseMessage Put([FromBody] ActividadesViewModel t)
        {
            try
            {


                var ACT = db.Actividades.Where(a => a.id == t.id).FirstOrDefault();

                if (ACT != null)
                {
                    db.Entry(ACT).State = EntityState.Modified;
                    ACT.estado = t.estado; 
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Actividad no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, ACT);
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