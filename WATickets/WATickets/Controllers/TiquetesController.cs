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
using System.Text.RegularExpressions;
using System.Net.Mime;

namespace WATickets.Controllers
{
    [Authorize]
    public class TiquetesController : ApiController
    {
        ModelCliente db = new ModelCliente();
        private static List<Attachment> PrepararImagenesInlineTicket(
    ref string html)
        {
            var imagenes = new List<Attachment>();

            var regex = new Regex(
                @"data:(image\/(?:png|jpeg|jpg|gif|webp));base64,([A-Za-z0-9+\/=\s]+)",
                RegexOptions.IgnoreCase
            );

            html = regex.Replace(html ?? "", match =>
            {
                var contentType = match.Groups[1].Value.ToLowerInvariant();

                var base64 = Regex.Replace(
                    match.Groups[2].Value,
                    @"\s+",
                    ""
                );

                var bytes = Convert.FromBase64String(base64);

                if (bytes.Length > 5 * 1024 * 1024)
                {
                    throw new Exception(
                        "Una captura pegada supera el límite permitido de 5 MB."
                    );
                }

                var contentId = "ticket-" + Guid.NewGuid().ToString("N");

                var extension = contentType.Contains("png")
                    ? ".png"
                    : contentType.Contains("gif")
                        ? ".gif"
                        : contentType.Contains("webp")
                            ? ".webp"
                            : ".jpg";

                var stream = new MemoryStream(bytes);

                var imagen = new Attachment(
                    stream,
                    contentId + extension,
                    contentType
                );

                imagen.ContentId = contentId;
                imagen.ContentDisposition.Inline = true;
                imagen.ContentDisposition.DispositionType =
                    DispositionTypeNames.Inline;

                imagenes.Add(imagen);

                // Reemplaza el Base64 por la referencia interna del correo.
                return "cid:" + contentId;
            });

            return imagenes;
        }
        private static string ObtenerUltimaRespuesta(string cuerpo)
        {
            if (string.IsNullOrWhiteSpace(cuerpo))
                return string.Empty;

            var texto = cuerpo;

            // Elimina la conversación citada por Gmail cuando viene como HTML.
            var bloqueGmail = Regex.Match(
                texto,
                @"<div[^>]*class\s*=\s*[""'][^""']*gmail_quote[^""']*[""'][^>]*>",
                RegexOptions.IgnoreCase
            );

            if (bloqueGmail.Success)
                texto = texto.Substring(0, bloqueGmail.Index);

            // Convierte saltos HTML en saltos de texto.
            texto = Regex.Replace(
                texto,
                @"<br\s*/?>",
                "\n",
                RegexOptions.IgnoreCase
            );

            texto = Regex.Replace(
                texto,
                @"</(?:p|div)\s*>",
                "\n",
                RegexOptions.IgnoreCase
            );

            // Elimina las etiquetas HTML restantes.
            texto = Regex.Replace(texto, @"<[^>]+>", "");

            texto = HttpUtility.HtmlDecode(texto)
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");

            var separadores = new[]
            {
        // Gmail en español.
        @"(?im)^\s*El\s+.+\s+escribió:\s*$",

        // Gmail en inglés.
        @"(?im)^\s*On\s+.+\s+wrote:\s*$",

        // Otros clientes de correo.
        @"(?im)^\s*-{2,}\s*Mensaje original\s*-{2,}\s*$",
        @"(?im)^\s*-{2,}\s*Original Message\s*-{2,}\s*$",
        @"(?im)^\s*De:\s+.+$",
        @"(?im)^\s*From:\s+.+$",

        // Primera línea citada.
        @"(?m)^\s*>"
    };

            var posicionCorte = texto.Length;

            foreach (var patron in separadores)
            {
                var coincidencia = Regex.Match(texto, patron);

                if (coincidencia.Success &&
                    coincidencia.Index < posicionCorte)
                {
                    posicionCorte = coincidencia.Index;
                }
            }

            texto = texto.Substring(0, posicionCorte);

            // Elimina exceso de líneas vacías.
            texto = Regex.Replace(texto, @"\n{3,}", "\n\n");

            return texto.Trim();
        }

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
                        IEnumerable<uint> uids = client.Search(SearchCondition.Unseen()).ToList();

                        foreach (var uid in uids)
                        {
                            System.Net.Mail.MailMessage message = client.GetMessage(uid, false);
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
                            var messageId = message.Headers["Message-ID"];
                            var inReplyTo = message.Headers["In-Reply-To"] ?? "";
                            var references = message.Headers["References"] ?? "";

                            var ticketRelacionado = db.Tickets
                                .Where(t => t.idCorreo != null && t.idCorreo != "")
                                .ToList()
                                .FirstOrDefault(t =>
                                    inReplyTo.IndexOf(t.idCorreo, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    references.IndexOf(t.idCorreo, StringComparison.OrdinalIgnoreCase) >= 0
                                );

                            if (ticketRelacionado != null)
                            {
                                var respuestaExistente =
                                    !string.IsNullOrWhiteSpace(messageId) &&
                                    db.Respuestas.Any(r =>
                                        r.idTicket == ticketRelacionado.id &&
                                        r.Respuesta.Contains(messageId)
                                    );

                                if (!respuestaExistente)
                                {
                                    var textoRespuesta = ObtenerUltimaRespuesta(message.Body);

                                    if (!string.IsNullOrWhiteSpace(textoRespuesta))
                                    {
                                        var nuevaRespuesta = new Respuestas
                                        {
                                            idTicket = ticketRelacionado.id,
                                            idUsuario = 0,

                                            Respuesta =
                                                "<div>" +
                                                HttpUtility.HtmlEncode(textoRespuesta)
                                                    .Replace("\r\n", "<br>")
                                                    .Replace("\n", "<br>") +
                                                "</div><!-- correo:" +
                                                HttpUtility.HtmlEncode(messageId ?? "") +
                                                " -->",

                                            EsNotaInterna = false,
                                            FechaCreacion = DateTime.Now
                                        };

                                        db.Respuestas.Add(nuevaRespuesta);

                                        ticketRelacionado.Status = "A";
                                        db.Entry(ticketRelacionado).State =
                                            EntityState.Modified;

                                        db.SaveChanges();
                                    }
                                }
                            }
                            else
                            {
                                var bandejaExistente = db.BandejaEntrada.Any(a =>
                                    a.idCorreo == messageId
                                );

                                if (!bandejaExistente)
                                {
                                    var bandeja = new BandejaEntrada
                                    {
                                        Procesado = "0",
                                        FechaIngreso = DateTime.Now,
                                        Asunto = message.Subject,
                                        Mensaje = "",
                                        Remitente = message.From.Address,
                                        Texto = message.Body,
                                        Adjuntos = ByteArrayPDF,
                                        TipoAdjunto = TipoAdjunto,
                                        idCorreo = messageId
                                    };

                                    db.BandejaEntrada.Add(bandeja);
                                    db.SaveChanges();
                                }
                            }
                            client.GetMessage(uid, true);
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
                    ti.idCorreo = item.idCorreo;
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
                            var Usuario = db.Login
                                .FirstOrDefault(a => a.id == t.idLoginAsignado);

                            if (Usuario == null)
                            {
                                throw new Exception(
                                    "El usuario asignado no existe."
                                );
                            }

                            var Correo = db.CorreosRecepcion.FirstOrDefault();

                            if (Correo == null)
                            {
                                throw new Exception(
                                    "No existe configuración de correo."
                                );
                            }

                            var html =
                                "<!DOCTYPE html>" +
                                "<html lang='es'>" +
                                "<head>" +
                                "<meta charset='UTF-8'>" +
                                "<meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
                                "</head>" +
                                "<body style='font-family: Arial, sans-serif;'>" +
                                "<div style='max-width: 700px; margin: auto;'>" +
                                "<p>Estimado usuario, se le ha asignado un nuevo ticket:</p>" +
                                "<p><strong>ID:</strong> @ID</p>" +
                                "<p><strong>Asunto:</strong> @ASUNTO</p>" +
                                "<div style='margin-top: 20px;'>@MENSAJE</div>" +
                                "</div>" +
                                "</body>" +
                                "</html>";

                            html = html.Replace(
                                "@ID",
                                ticket.id.ToString()
                            );

                            html = html.Replace(
                                "@ASUNTO",
                                HttpUtility.HtmlEncode(ticket.Asunto ?? "")
                            );

                            // Mensaje contiene el texto y las capturas pegadas en Base64.
                            html = html.Replace(
                                "@MENSAJE",
                                ticket.Mensaje ?? ""
                            );

                            // Convierte las capturas Base64 en imágenes dentro del correo.
                            var imagenesInline =
                                PrepararImagenesInlineTicket(ref html);

                            G G = new G();

                            var resp = G.SendV2(
                                Usuario.Email,
                                "",
                                "",
                                Correo.RecepcionEmail,
                                "TICKET",
                                "NUEVO TICKET ASIGNADO",
                                html,
                                Correo.RecepcionHostName,
                                587,
                                Correo.RecepcionUseSSL.Value,
                                Correo.RecepcionEmail,
                                Correo.RecepcionPassword,
                                imagenesInline
                            );

                            if (!resp)
                            {
                                throw new Exception(
                                    "No se pudo enviar el correo del ticket."
                                );
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
                        }
                    }
                    db.Entry(ticket).State = EntityState.Modified;
                    ticket.Duracion = t.Duracion;
                    ticket.idLoginAsignado = t.idLoginAsignado;
                    ticket.Comentarios = t.Comentarios;
                    ticket.idEmpresa = t.idEmpresa;
                    ticket.DuracionEstimada = t.DuracionEstimada;
                    ticket.Status = t.Status;

                    if (t.Status == "C")
                    {
                        ticket.FechaCierre = DateTime.Now;
                    }

                    ticket.Tipo = t.Tipo;
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
        [HttpGet]
        [Route("api/Tiquetes/LeerRespuestasTicket")]
        public HttpResponseMessage GetLeerRespuestasTicket( [FromUri] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return Request.CreateResponse(
                        HttpStatusCode.BadRequest,
                        "El ticket no es válido."
                    );
                }

                var ticket = db.Tickets.FirstOrDefault(t => t.id == id);

                if (ticket == null)
                {
                    return Request.CreateResponse(  HttpStatusCode.NotFound,"El ticket no existe.");
                }

                if (string.IsNullOrWhiteSpace(ticket.idCorreo))
                {
                    return Request.CreateResponse(
                        HttpStatusCode.OK,
                        new
                        {
                            procesadas = 0,
                            mensaje = "El ticket no tiene un correo asociado."
                        }
                    );
                }

                var cantidadProcesada = 0;
                var correos = db.CorreosRecepcion.ToList();

                foreach (var configuracion in correos)
                {
                    using (var client = new ImapClient(
                        configuracion.RecepcionHostName,
                        (int)configuracion.RecepcionPort,
                        configuracion.RecepcionEmail,
                        configuracion.RecepcionPassword,
                        AuthMethod.Login,
                        (bool)configuracion.RecepcionUseSSL))
                    {
                        var uids = client
                            .Search(SearchCondition.Unseen())
                            .ToList();

                        foreach (var uid in uids)
                        {
                            // false evita marcar como leído antes de comprobarlo.
                            var mensaje = client.GetMessage(uid, false);

                            var messageId =
                                mensaje.Headers["Message-ID"] ?? "";

                            var inReplyTo =
                                mensaje.Headers["In-Reply-To"] ?? "";

                            var references =
                                mensaje.Headers["References"] ?? "";

                            var perteneceAlTicket =
                                inReplyTo.IndexOf(
                                    ticket.idCorreo,
                                    StringComparison.OrdinalIgnoreCase
                                ) >= 0
                                ||
                                references.IndexOf(
                                    ticket.idCorreo,
                                    StringComparison.OrdinalIgnoreCase
                                ) >= 0;

                            if (!perteneceAlTicket)
                            {
                                continue;
                            }

                            var yaExiste =
                                !string.IsNullOrWhiteSpace(messageId)
                                &&
                                db.Respuestas.Any(r =>
                                    r.idTicket == ticket.id &&
                                    r.Respuesta.Contains(messageId)
                                );
                            if (yaExiste)
                            {
                                // Ya fue guardado anteriormente; marcarlo como leído.
                                client.GetMessage(uid, true);
                                continue;
                            }

                            var textoRespuesta = ObtenerUltimaRespuesta(mensaje.Body);

                            if (string.IsNullOrWhiteSpace(textoRespuesta))
                            {
                                client.GetMessage(uid, true);
                                continue;
                            }

                            var nuevaRespuesta = new Respuestas
                            {
                                idTicket = ticket.id,
                                idUsuario = null,

                                Respuesta =
                                    "<div>" +
                                    HttpUtility.HtmlEncode(textoRespuesta)
                                        .Replace("\r\n", "<br>")
                                        .Replace("\n", "<br>") +
                                    "</div><!-- correo:" +
                                    HttpUtility.HtmlEncode(messageId ?? "") +
                                    " -->",

                                EsNotaInterna = false,
                                FechaCreacion = DateTime.Now
                            };

                            db.Respuestas.Add(nuevaRespuesta);

                            // Si el cliente respondió, reabrir el ticket.
                            ticket.Status = "A";
   
                            db.Entry(ticket).State = EntityState.Modified;
                            db.SaveChanges();

                            cantidadProcesada++;
                            client.GetMessage(uid, true);
                        }
                    }
                }

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    new
                    {
                        procesadas = cantidadProcesada,
                        mensaje = cantidadProcesada > 0
                            ? "Se encontraron respuestas nuevas."
                            : "No se encontraron respuestas nuevas."
                    }
                );
            }
            catch (Exception ex)
            {
                BitacoraErrores bt = new BitacoraErrores
                {
                    Descripcion = ex.Message,
                    StackTrace = ex.StackTrace,
                    Fecha = DateTime.Now,
                    JSON = JsonConvert.SerializeObject(ex)
                };

                db.BitacoraErrores.Add(bt);
                db.SaveChanges();

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    ex.Message
                );
            }
        }
    }
}