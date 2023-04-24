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
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Mail;

namespace WATickets.Controllers
{
    [Authorize]
    public class TiquetesController : ApiController
    {
        ModelCliente db = new ModelCliente();

        [Route("api/Tiquetes/RealizarLecturaEmail")]

        public async Task<HttpResponseMessage> GetRealizarLecturaEmailsAsync()
        {
            try
            {
                var Correos = db.CorreosRecepcion.ToList();

                foreach (var item in Correos)
                {
                    using (ImapClient client = new ImapClient(item.RecepcionHostName, (int)(item.RecepcionPort),
                          item.RecepcionEmail, item.RecepcionPassword, AuthMethod.Login, (bool)(item.RecepcionUseSSL)))
                    {
                        IEnumerable<uint> uids = client.Search(SearchCondition.Unseen());

                        DateTime recepcionUltimaLecturaImap = DateTime.Now;
                        if (item.RecepcionUltimaLecturaImap != null)
                            recepcionUltimaLecturaImap = item.RecepcionUltimaLecturaImap.Value;

                        uids.Concat(client.Search(SearchCondition.SentSince(recepcionUltimaLecturaImap)));

                        foreach (var uid in uids)
                        {
                            System.Net.Mail.MailMessage message = client.GetMessage(uid);
                            byte[] ByteArrayPDF = new byte[0];
                            var TipoAdjunto = "";
                            //try
                            //{


                            //    BinaryFormatter bf = new BinaryFormatter();
                            //    using (MemoryStream ms = new MemoryStream())
                            //    {
                            //        bf.Serialize(ms, message);
                            //       ByteArrayPDF = ms.ToArray();
                            //    }
                            //}
                            //catch (Exception ex)
                            //{
                            //    BitacoraErrores bt = new BitacoraErrores();
                            //    bt.Descripcion = ex.Message;
                            //    bt.StackTrace = ex.StackTrace;
                            //    bt.Fecha = DateTime.Now;
                            //    bt.JSON = JsonConvert.SerializeObject(ex);
                            //    db.BitacoraErrores.Add(bt);
                            //    db.SaveChanges();
                            //}




                            if (message.Attachments.Count > 0)
                            {
                                try
                                {
                                    var attachment = message.Attachments.Where(a => !a.ContentId.ToUpper().Contains("@")).FirstOrDefault();
                                    System.IO.StreamReader sr = new System.IO.StreamReader(attachment.ContentStream);
                                    string texto = sr.ReadToEnd();
                                    ByteArrayPDF = ((MemoryStream)attachment.ContentStream).ToArray();
                                    TipoAdjunto = attachment.Name.Split('.')[1];

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
                                }
                            }
                            var bandeja2 = db.BandejaEntrada.Where(a => a.Asunto == message.Subject && a.Remitente == message.From.Address).FirstOrDefault();
                            if (!message.Subject.ToUpper().Contains("RE:".ToUpper()) && bandeja2 == null)
                            {
                                BandejaEntrada bandeja = new BandejaEntrada();
                                bandeja.Procesado = "0";
                                bandeja.FechaIngreso = DateTime.Now;
                                bandeja.Asunto = message.Subject;
                                bandeja.Mensaje = "";
                                bandeja.Remitente = message.From.Address;
                                bandeja.Texto = message.Body;
                                bandeja.Adjuntos = ByteArrayPDF;
                                bandeja.TipoAdjunto = TipoAdjunto;
                                
                                db.BandejaEntrada.Add(bandeja);
                                db.SaveChanges();
                            }
                        }
                    }
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

        [Route("api/Tiquetes/LeerBandejaEntrada")]
        public async Task<HttpResponseMessage> GetLeerBandejaEntradaAsync()
        {
            try
            {
                var Lista = db.BandejaEntrada.Where(a => a.Procesado == "0" && string.IsNullOrEmpty(a.Mensaje)).ToList();

                foreach (var item in Lista)
                {
                    Tickets ti = new Tickets();
                    ti.FechaTicket = item.FechaIngreso;
                    ti.Asunto = item.Asunto;
                    ti.Mensaje = item.Texto;
                    ti.Comentarios = "";
                    ti.idLoginAsignado = 0;
                    ti.Duracion = "00:00:00";
                    ti.PersonaTicket = item.Remitente;
                    ti.Status = "E";
                    ti.DuracionEstimada = "00:00:00";
                    ti.idEmpresa = (db.Empresas.Where(a => item.Remitente.ToUpper().Contains(a.Dominio.ToUpper())).FirstOrDefault() == null ? 0 : db.Empresas.Where(a => item.Remitente.ToUpper().Contains(a.Dominio.ToUpper())).FirstOrDefault().id);
                    ti.Adjuntos = item.Adjuntos;
                    ti.TipoAdjunto = item.TipoAdjunto;
                    ti.FechaCierre = DateTime.Now;
                    db.Tickets.Add(ti);
                    db.SaveChanges();

                    db.Entry(item).State = EntityState.Modified;
                    item.FechaProcesado = DateTime.Now;
                    item.Procesado = "1";
                    item.Mensaje = "";
                    db.SaveChanges();

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


        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                var time = new DateTime();
                var Tiquetes = db.Tickets.Where(a => (filtro.FechaInicial != time ? a.FechaTicket >= filtro.FechaInicial && a.FechaTicket <= filtro.FechaFinal : true)).ToList();

                if (!string.IsNullOrEmpty(filtro.Texto))
                {
                    Tiquetes = Tiquetes.Where(a => a.Asunto.ToUpper().Contains(filtro.Texto.ToUpper()) || a.Mensaje.ToUpper().Contains(filtro.Texto.ToUpper())).ToList();
                }

                if (filtro.Codigo1 > 0)
                {
                    Tiquetes = Tiquetes.Where(a => a.idLoginAsignado == filtro.Codigo1).ToList();
                }

                if (!string.IsNullOrEmpty(filtro.Texto2) && filtro.Texto2 != "N")
                {
                    Tiquetes = Tiquetes.Where(a => a.Status == filtro.Texto2).ToList();
                }

                if (filtro.Codigo2 > 0)
                {
                    Tiquetes = Tiquetes.Where(a => a.idEmpresa == filtro.Codigo2).ToList();
                }

                return Request.CreateResponse(HttpStatusCode.OK, Tiquetes);

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

        [Route("api/Tiquetes/Consultar")]
        public HttpResponseMessage GetOne([FromUri]int id)
        {
            try
            {



                var Tiquetes = db.Tickets.Where(a => a.id == id).FirstOrDefault();


                if (Tiquetes == null)
                {
                    throw new Exception("Este ticket no se encuentra registrado");
                }

                return Request.CreateResponse(HttpStatusCode.OK, Tiquetes);
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
        public HttpResponseMessage Post([FromBody] Tickets t)
        {
            try
            {


                var ticket = db.Tickets.Where(a => a.id == t.id).FirstOrDefault();

                if (ticket == null)
                {
                    ticket = new Tickets();

                    ticket.FechaTicket = t.FechaTicket;
                    ticket.Asunto = t.Asunto;
                    ticket.Mensaje = t.Mensaje;
                    ticket.Comentarios = t.Comentarios;
                    ticket.idLoginAsignado = t.idLoginAsignado;
                    ticket.Duracion = "00:00:00";
                    ticket.PersonaTicket = t.PersonaTicket;
                    ticket.Status = "E";
                    ticket.idEmpresa = t.idEmpresa;
                    ticket.DuracionEstimada = t.DuracionEstimada;
                    ticket.FechaCierre = DateTime.Now;
                    db.Tickets.Add(ticket);
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("ticket ya existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, ticket);
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
        [Route("api/Tiquetes/Actualizar")]
        public HttpResponseMessage Put([FromBody] Tickets t)
        {
            try
            {


                var ticket = db.Tickets.Where(a => a.id == t.id).FirstOrDefault();

                if (ticket != null)
                {
                    if (ticket.Status == "E")
                    {
                        try
                        {
                            List<Attachment> adjuntos = new List<Attachment>();


                            var html = "<!DOCTYPE html> <html lang='es'> <head> <meta charset='UTF-8'> <meta http-equiv='X-UA-Compatible' content='IE=edge'> <meta name='viewport' content='width=device-width, initial-scale=1.0'> <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/css/bootstrap.min.css' /> </head> <body> <div class='row'> <div class='col-sm-3'></div> <div class='col-sm-6' style='text-justify: center;'> <p>Estimado usuario se le ha asignado un nuevo ticket, abajo encontrará información mas detallada: </p> <ul> <li>ID: @ID</li> <li>Resumen: @Resumen</li> </ul> </div> <div class='col-sm-3'></div></div> </body> </html>";

                            html = html.Replace("@ID", ticket.id.ToString());
                            html = html.Replace("@Resumen", ticket.Asunto + " <br> " + ticket.Mensaje);
                            var Usuario = db.Login.Where(a => a.id == t.idLoginAsignado).FirstOrDefault();
                            var Correo = db.CorreosRecepcion.FirstOrDefault();
                            G G = new G();

                            Attachment att = new Attachment(new MemoryStream(ticket.Adjuntos), "Adjunto." + ticket.TipoAdjunto);
                            adjuntos.Add(att);



                            var resp = G.SendV2(Usuario.Email, "", "", Correo.RecepcionEmail, "TICKET", "NUEVO TICKET ASIGNADO", html, Correo.RecepcionHostName, 587, Correo.RecepcionUseSSL.Value, Correo.RecepcionEmail, Correo.RecepcionPassword, adjuntos);
                            if (!resp)
                            {
                                BitacoraErrores bt = new BitacoraErrores();
                                bt.Descripcion = "Enviar correo";
                                bt.StackTrace = "";
                                bt.Fecha = DateTime.Now;
                                bt.JSON = JsonConvert.SerializeObject(resp);
                                db.BitacoraErrores.Add(bt);
                                db.SaveChanges();
                            }
                        }
                        catch (Exception ex )
                        {

                            BitacoraErrores bt = new BitacoraErrores();
                            bt.Descripcion = ex.Message;
                            bt.StackTrace = ex.StackTrace;
                            bt.Fecha = DateTime.Now;
                            bt.JSON = JsonConvert.SerializeObject(ex);
                            db.BitacoraErrores.Add(bt);
                            db.SaveChanges();
                        }
                      





                    }
                    db.Entry(ticket).State = EntityState.Modified;
                    ticket.Duracion = t.Duracion;
                    ticket.idLoginAsignado = t.idLoginAsignado;
                    ticket.Comentarios = t.Comentarios;
                    ticket.idEmpresa = t.idEmpresa;
                    ticket.DuracionEstimada = t.DuracionEstimada;
                    ticket.Status = "A";
                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("ticket no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, ticket);
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
        [Route("api/Tiquetes/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {


                var ticket = db.Tickets.Where(a => a.id == id).FirstOrDefault();

                if (ticket != null)
                {

                    db.Entry(ticket).State = EntityState.Modified;
                    if (ticket.Status == "A")
                    {
                        ticket.Status = "C";
                        ticket.FechaCierre = DateTime.Now;

                    }
                    else
                    {
                        ticket.Status = "A";
                    }

                    db.SaveChanges();

                }
                else
                {
                    throw new Exception("Tiquete no existe");
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