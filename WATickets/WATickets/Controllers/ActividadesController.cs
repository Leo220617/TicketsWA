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
        public HttpResponseMessage Get([FromUri] Filtros filtro)
        {
            try
            {
                filtro = filtro ?? new Filtros();

                var consulta = db.Actividades
                    .AsNoTracking()
                    .AsQueryable();

                if (filtro.FechaInicial != DateTime.MinValue)
                {
                    var fechaInicial = filtro.FechaInicial.Date;

                    consulta = consulta.Where(a =>
                        a.fechaAgendada >= fechaInicial
                    );
                }

                if (filtro.FechaFinal != DateTime.MinValue)
                {
                    var fechaFinal =
                        filtro.FechaFinal.Date.AddDays(1);

                    consulta = consulta.Where(a =>
                        a.fechaAgendada < fechaFinal
                    );
                }

                if (filtro.Codigo1 > 0)
                {
                    consulta = consulta.Where(a =>
                        a.idUsuario == filtro.Codigo1
                    );
                }

                if (filtro.Codigo2 > 0)
                {
                    consulta = consulta.Where(a =>
                        a.idTipoActividad == filtro.Codigo2
                    );
                }

                var actividades = consulta
                    .OrderBy(a => a.fechaAgendada)
                    .Select(a => new
                    {
                        a.id,
                        a.idUsuario,
                        a.idEmpresa,
                        a.idTipoActividad,
                        a.titulo,
                        a.fechaAgendada,
                        a.fechaCreacion,
                        a.estado,
                        a.comentario,
                        a.horas,

                        NomUsuario = db.Login
                            .Where(u => u.id == a.idUsuario)
                            .Select(u => u.Nombre)
                            .FirstOrDefault(),

                        NomEmpresa = db.Empresas
                            .Where(e => e.id == a.idEmpresa)
                            .Select(e => e.Nombre)
                            .FirstOrDefault(),

                        NomActividad = db.TiposActividad
                            .Where(t => t.id == a.idTipoActividad)
                            .Select(t => t.nombre)
                            .FirstOrDefault()
                    })
                    .ToList()
                    .Select(a => new
                    {
                        a.id,
                        a.idUsuario,
                        a.idEmpresa,
                        a.idTipoActividad,
                        a.titulo,
                        a.fechaAgendada,
                        a.fechaCreacion,
                        a.estado,
                        a.comentario,

                        NomUsuario = a.NomUsuario ?? "Sin asignar",
                        NomEmpresa = a.NomEmpresa ?? "Sin empresa",
                        NomActividad = a.NomActividad ?? "Actividad",
                        horas = a.horas,
                        start = a.fechaAgendada,

                        title =
                            (a.NomUsuario ?? "Sin asignar") +
                            " · " +
                            (a.titulo ?? a.NomActividad ?? "Actividad"),

                        color = a.estado == "Realizado"
                            ? "#1f7a45"
                            : a.estado == "Cancelado"
                                ? "#b12704"
                                : "#0073bb",

                        extendedProps = new
                        {
                            idUsuario = a.idUsuario,
                            tipo = a.idTipoActividad,
                            estado = a.estado ?? "Pendiente",
                            comentario = a.comentario ?? "",
                            usuario = a.NomUsuario ?? "Sin asignar",
                            empresa = a.NomEmpresa ?? "Sin empresa",
                            actividad = a.NomActividad ?? "Actividad",
                            horas = a.horas
                        }
                    })
                    .ToList();

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    actividades
                );
            }
            catch (Exception ex)
            {
                var bitacora = new BitacoraErrores
                {
                    Descripcion = ex.Message,
                    StackTrace = ex.StackTrace,
                    Fecha = DateTime.Now,
                    JSON = JsonConvert.SerializeObject(ex)
                };

                db.BitacoraErrores.Add(bitacora);
                db.SaveChanges();

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    ex
                );
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
                    act.horas = t.horas;
                    db.Actividades.Add(act);
                    db.SaveChanges();



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
        public HttpResponseMessage Put(
            [FromBody] ActividadesViewModel modelo)
        {
            try
            {
                if (modelo == null || modelo.id <= 0)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        "La actividad no es válida."
                    );
                }

                var actividad = db.Actividades.FirstOrDefault(
                    a => a.id == modelo.id
                );

                if (actividad == null)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.NotFound,
                        "La actividad no existe."
                    );
                }

                if (!string.IsNullOrWhiteSpace(modelo.estado))
                {
                    actividad.estado = modelo.estado;
                }

                if (modelo.idUsuario > 0)
                {
                    actividad.idUsuario = modelo.idUsuario;
                }

                if (modelo.fechaAgendada != DateTime.MinValue)
                {
                    actividad.fechaAgendada =
                        modelo.fechaAgendada;
                }

                if (modelo.idTipoActividad > 0)
                {
                    actividad.idTipoActividad =
                        modelo.idTipoActividad;
                }

                if (!string.IsNullOrWhiteSpace(modelo.titulo))
                {
                    actividad.titulo = modelo.titulo.Trim();
                }

                if (modelo.comentario != null)
                {
                    actividad.comentario = modelo.comentario;
                }

                if(modelo.horas != actividad.horas && modelo.horas > 0)
                {
                    actividad.horas = modelo.horas;
                }

                db.SaveChanges();

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    actividad
                );
            }
            catch (Exception ex)
            {
                var bitacora = new BitacoraErrores
                {
                    Descripcion = ex.Message,
                    StackTrace = ex.StackTrace,
                    Fecha = DateTime.Now,
                    JSON = JsonConvert.SerializeObject(ex)
                };

                db.BitacoraErrores.Add(bitacora);
                db.SaveChanges();

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    ex
                );
            }
        }
    }
}