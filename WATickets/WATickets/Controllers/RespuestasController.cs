using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using WATickets.Models;
using WATickets.Models.Cliente;

namespace WATickets.Controllers
{
    [Authorize]
    public class RespuestasController : ApiController
    {
        ModelCliente db = new ModelCliente();

        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                if (filtro != null)
                {
                    if (filtro.Codigo1 != 0)
                    {
                        var Respuestas = db.Respuestas.Where(a => a.idTicket == filtro.Codigo1).OrderBy(a => a.FechaCreacion).ThenBy(a => a.id).ToList();

                        return Request.CreateResponse(HttpStatusCode.OK, Respuestas);
                    }
                    else
                    {
                        throw new Exception("No vienen parámetros correctos");
                    }
                }
                else
                {
                    throw new Exception("No vienen parámetros correctos");
                }
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

        [Route("api/Respuestas/Consultar")]
        public HttpResponseMessage GetOne([FromUri] int id)
        {
            try
            {
                var respuesta = db.Respuestas.Where(a => a.id == id).FirstOrDefault();

                if (respuesta == null)
                {
                    throw new Exception("La respuesta no se encuentra registrada");
                }

                return Request.CreateResponse(HttpStatusCode.OK, respuesta);
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
        public HttpResponseMessage Post([FromBody] Respuestas r)
        {
            try
            {
                var ticket = db.Tickets.Where(a => a.id == r.idTicket).FirstOrDefault();

                if (ticket == null)
                {
                    throw new Exception("El ticket no se encuentra registrado");
                }

                if (string.IsNullOrEmpty(r.Respuesta))
                {
                    throw new Exception("Debe ingresar una respuesta");
                }

                Respuestas respuesta = new Respuestas();
                respuesta.idTicket = r.idTicket;
                respuesta.idUsuario = r.idUsuario;
                respuesta.Respuesta = SanitizarContenidoRespuesta(r.Respuesta);
                respuesta.EsNotaInterna = r.EsNotaInterna;
                respuesta.FechaCreacion = DateTime.Now;
                db.Respuestas.Add(respuesta);
                db.SaveChanges();


                return Request.CreateResponse(HttpStatusCode.OK, respuesta);
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

        //Enviar respuesta al correo de la persona que creó el ticket
        [HttpGet]
        [Route("api/Respuestas/EnviarCorreo")]
        public HttpResponseMessage GetEnviarCorreo([FromUri] int id)
        {
            try
            {
                var respuesta = db.Respuestas.Where(a => a.id == id).FirstOrDefault();

                if (respuesta == null)
                {
                    throw new Exception("La respuesta no se encuentra registrada");
                }

                if (respuesta.EsNotaInterna)
                {
                    throw new Exception("Las notas internas no se pueden enviar al cliente");
                }

                var ticket = db.Tickets.Where(a => a.id == respuesta.idTicket).FirstOrDefault();

                if (ticket == null)
                {
                    throw new Exception("El ticket no se encuentra registrado");
                }

                if (string.IsNullOrEmpty(ticket.PersonaTicket))
                {
                    throw new Exception("El ticket no tiene un correo registrado");
                }

                var CorreoEnvio = db.CorreoEnvio.FirstOrDefault();

                if (CorreoEnvio == null)
                {
                    throw new Exception("No existe una configuración para el envío de correos");
                }

                if (CorreoEnvio.EnvioPort == null || CorreoEnvio.RecepcionUseSSL == null)
                {
                    throw new Exception("La configuración del correo está incompleta");
                }

                var textoRespuesta = SanitizarContenidoRespuesta(respuesta.Respuesta);
                var asuntoTicket = HttpUtility.HtmlEncode(ticket.Asunto);
                var html = "<!DOCTYPE html> <html lang='es'> <head> <meta charset='UTF-8'> <meta name='viewport' content='width=device-width, initial-scale=1.0'> </head> <body style='margin: 0; padding: 0; background-color: #f2f3f3; font-family: Arial, sans-serif; color: #161e2d;'> <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='background-color: #f2f3f3; padding: 30px 15px;'> <tr> <td align='center'> <table role='presentation' width='650' cellspacing='0' cellpadding='0' style='max-width: 650px; width: 100%; background-color: #ffffff; border: 1px solid #d5dbdb;'> <tr> <td style='background-color: #131e29; border-bottom: 4px solid #ff9900; padding: 22px 28px; color: #ffffff;'> <div style='font-size: 12px; color: #aab7b8;'>SOPORTE</div> <div style='font-size: 22px; font-weight: bold; margin-top: 5px;'>Respuesta al ticket #@ID</div> </td> </tr> <tr> <td style='padding: 28px;'> <p style='margin-top: 0;'>Hola,</p> <p>El equipo de soporte ha respondido su solicitud:</p> <div style='background-color: #f7f8f8; border-left: 4px solid #ff9900; padding: 18px; margin: 22px 0; line-height: 1.6;'>@RESPUESTA</div> <table role='presentation' width='100%' cellspacing='0' cellpadding='0' style='border-top: 1px solid #eaeded; margin-top: 25px; padding-top: 18px;'> <tr> <td style='font-size: 13px; color: #687078;'> <b>Ticket:</b> #@ID<br> <b>Asunto:</b> @ASUNTO </td> </tr> </table> <p style='font-size: 13px; color: #687078; margin-bottom: 0; margin-top: 25px;'>Puede responder a este correo si necesita información adicional.</p> </td> </tr> </table> </td> </tr> </table> </body> </html>";
                html = html.Replace("@ID", ticket.id.ToString());
                html = html.Replace("@ASUNTO", asuntoTicket);
                html = html.Replace("@RESPUESTA", textoRespuesta);

                var imagenesInline = PrepararImagenesInline(ref html);

                var asuntoCorreo = ticket.Asunto == null ? "" : ticket.Asunto.Trim();

                if (!asuntoCorreo.StartsWith("RE:", StringComparison.OrdinalIgnoreCase))
                {
                    asuntoCorreo = "Re: " + asuntoCorreo;
                }

                G G = new G();

                var resp = G.SendV2(ticket.PersonaTicket, "", "", CorreoEnvio.RecepcionEmail, "TICKETS", asuntoCorreo, html, CorreoEnvio.RecepcionHostName, CorreoEnvio.EnvioPort, CorreoEnvio.RecepcionUseSSL, CorreoEnvio.RecepcionEmail, CorreoEnvio.RecepcionPassword, imagenesInline, ticket.idCorreo);
                if (!resp)
                {
                    throw new Exception("No se ha podido enviar el correo a " + ticket.PersonaTicket);
                }

                return Request.CreateResponse(HttpStatusCode.OK, respuesta);
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

        private static string SanitizarContenidoRespuesta(string contenido)
        {
            if (string.IsNullOrWhiteSpace(contenido))
                return "";

            if (!contenido.Contains("<"))
            {
                return HttpUtility.HtmlEncode(contenido)
                    .Replace("\r\n", "<br>")
                    .Replace("\n", "<br>");
            }

            var limpio = Regex.Replace(
                contenido,
                @"<(?!\/?(?:div|p|br|strong|b|em|i|u|ul|ol|li|img)\b)[^>]*>",
                "",
                RegexOptions.IgnoreCase
            );

            limpio = Regex.Replace(
                limpio,
                @"\s+on[a-z]+\s*=\s*(['""])[\s\S]*?\1",
                "",
                RegexOptions.IgnoreCase
            );

            limpio = Regex.Replace(
                limpio,
                @"\s+(?:href|formaction)\s*=\s*(['""])[\s\S]*?\1",
                "",
                RegexOptions.IgnoreCase
            );

            return Regex.Replace(
                limpio,
                @"javascript\s*:",
                "",
                RegexOptions.IgnoreCase
            );
        }

        private static List<Attachment> PrepararImagenesInline(ref string html)
        {
            var imagenes = new List<Attachment>();
            var regex = new Regex(
                @"data:(image\/(?:png|jpeg|jpg|gif|webp));base64,([A-Za-z0-9+\/=]+)",
                RegexOptions.IgnoreCase
            );

            html = regex.Replace(html ?? "", match =>
            {
                var contentType = match.Groups[1].Value.ToLowerInvariant();
                var bytes = Convert.FromBase64String(match.Groups[2].Value);

                if (bytes.Length > 5 * 1024 * 1024)
                    throw new Exception("Una imagen pegada supera el límite permitido de 5 MB.");

                var contentId = "ticket-" + Guid.NewGuid().ToString("N");
                var extension = contentType.Contains("png") ? ".png"
                    : contentType.Contains("gif") ? ".gif"
                    : contentType.Contains("webp") ? ".webp"
                    : ".jpg";

                var stream = new MemoryStream(bytes);
                var imagen = new Attachment(stream, contentId + extension, contentType)
                {
                    ContentId = contentId
                };

                imagen.ContentDisposition.Inline = true;
                imagen.ContentDisposition.DispositionType =
                    DispositionTypeNames.Inline;

                imagenes.Add(imagen);
                return "cid:" + contentId;
            });

            return imagenes;
        }

        [HttpPut]
        [Route("api/Respuestas/Actualizar")]
        public HttpResponseMessage Put([FromBody] Respuestas r)
        {
            try
            {
                var respuesta = db.Respuestas.Where(a => a.id == r.id).FirstOrDefault();

                if (respuesta != null)
                {
                    db.Entry(respuesta).State = EntityState.Modified;
                    respuesta.Respuesta = SanitizarContenidoRespuesta(r.Respuesta);
                    respuesta.EsNotaInterna = r.EsNotaInterna;
                    db.SaveChanges();
                }
                else
                {
                    throw new Exception("La respuesta no existe");
                }

                return Request.CreateResponse(HttpStatusCode.OK, respuesta);
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
        [Route("api/Respuestas/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                var respuesta = db.Respuestas.Where(a => a.id == id).FirstOrDefault();

                if (respuesta != null)
                {
                    db.Respuestas.Remove(respuesta);
                    db.SaveChanges();
                }
                else
                {
                    throw new Exception("La respuesta no existe");
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

