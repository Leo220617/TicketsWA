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

                if (bytes.Length > 18L * 1024 * 1024)
                {
                    throw new Exception(
                        "Una captura pegada supera el límite permitido de 18 MB."
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
        private static string ObtenerTipoContenido(string extension)
        {
            switch ((extension ?? "").TrimStart('.').ToLowerInvariant())
            {
                case "png":
                    return "image/png";

                case "jpg":
                case "jpeg":
                    return "image/jpeg";

                case "pdf":
                    return "application/pdf";

                case "xls":
                    return "application/vnd.ms-excel";

                case "xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                case "doc":
                    return "application/msword";

                case "docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

                case "csv":
                    return "text/csv";

                default:
                    return "application/octet-stream";
            }
        }
        private void GuardarAdjuntosDelCorreo(
          MailMessage mensaje,
          int idTicket)
        {
            if (mensaje?.Attachments == null ||
                mensaje.Attachments.Count == 0)
            {
                return;
            }

            const long limiteTotal =
                18L * 1024 * 1024;

            const int maximoArchivos = 5;

            long tamanoAcumulado = 0;
            int cantidadGuardada = 0;

            string[] extensionesPermitidas =
            {
        "png", "jpg", "jpeg",
        "pdf",
        "xls", "xlsx",
        "doc", "docx",
        "csv"
    };

            foreach (Attachment archivo in mensaje.Attachments)
            {
                try
                {
                    // No guardar imágenes internas de firmas.
                    if (archivo.ContentDisposition != null &&
                        archivo.ContentDisposition.Inline)
                    {
                        continue;
                    }

                    if (cantidadGuardada >= maximoArchivos)
                    {
                        break;
                    }

                    var nombreArchivo =
                        string.IsNullOrWhiteSpace(archivo.Name)
                            ? "archivo"
                            : Path.GetFileName(archivo.Name);

                    var extension = Path
                        .GetExtension(nombreArchivo)
                        .TrimStart('.')
                        .ToLowerInvariant();

                    if (!extensionesPermitidas.Contains(
                        extension))
                    {
                        continue;
                    }

                    byte[] contenido;

                    using (var memoria = new MemoryStream())
                    {
                        if (archivo.ContentStream.CanSeek)
                        {
                            archivo.ContentStream.Position = 0;
                        }

                        archivo.ContentStream.CopyTo(memoria);
                        contenido = memoria.ToArray();
                    }

                    if (contenido.Length == 0)
                    {
                        continue;
                    }

                    if (contenido.Length > limiteTotal)
                    {
                        throw new Exception(
                            "El archivo " + nombreArchivo +
                            " supera el límite de 18 MB."
                        );
                    }

                    if (tamanoAcumulado + contenido.Length >
                        limiteTotal)
                    {
                        throw new Exception(
                            "Los archivos recibidos superan " +
                            "el límite total de 18 MB."
                        );
                    }

                    var tipoContenido =
                        archivo.ContentType?.MediaType;

                    if (string.IsNullOrWhiteSpace(
                        tipoContenido))
                    {
                        tipoContenido =
                            ObtenerTipoContenido(extension);
                    }

                    var adjuntoDataUrl =
                        "data:" + tipoContenido +
                        ";name=" +
                        Uri.EscapeDataString(nombreArchivo) +
                        ";base64," +
                        Convert.ToBase64String(contenido);

                    db.Adjuntos.Add(new Adjuntos
                    {
                        idTicket = idTicket,
                        Adjunto = adjuntoDataUrl
                    });

                    tamanoAcumulado += contenido.Length;
                    cantidadGuardada++;
                }
                catch (Exception ex)
                {
                    db.BitacoraErrores.Add(
                        new BitacoraErrores
                        {
                            Descripcion =
                                "No se pudo guardar el archivo recibido '" +
                                archivo.Name + "': " +
                                ex.Message,

                            StackTrace = ex.StackTrace,
                            Fecha = DateTime.Now,
                            JSON =
                                JsonConvert.SerializeObject(ex)
                        }
                    );
                }
            }
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




                            if (message.Attachments != null &&
          message.Attachments.Count > 0)
                            {
                                try
                                {
                                    var attachment = message.Attachments
                                        .Cast<System.Net.Mail.Attachment>()
                                        .FirstOrDefault(a =>
                                            a.ContentDisposition == null ||
                                            !a.ContentDisposition.Inline
                                        );

                                    if (attachment != null)
                                    {
                                        using (var memoria = new MemoryStream())
                                        {
                                            if (attachment.ContentStream.CanSeek)
                                            {
                                                attachment.ContentStream.Position = 0;
                                            }

                                            attachment.ContentStream.CopyTo(memoria);
                                            ByteArrayPDF = memoria.ToArray();
                                        }

                                        TipoAdjunto = Path
                                            .GetExtension(attachment.Name ?? "")
                                            .TrimStart('.')
                                            .ToLowerInvariant();

                                        string[] extensionesPermitidas =
                                        {
                "png", "jpg", "jpeg",
                "pdf",
                "xls", "xlsx",
                "doc", "docx",
                "csv"
            };

                                        if (!extensionesPermitidas.Contains(TipoAdjunto))
                                        {
                                            ByteArrayPDF = new byte[0];
                                            TipoAdjunto = "";
                                        }

                                        // Máximo 18 MB.
                                        if (ByteArrayPDF.Length > 18L * 1024 * 1024)
                                        {
                                            ByteArrayPDF = new byte[0];
                                            TipoAdjunto = "";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    var bt = new BitacoraErrores
                                    {
                                        Descripcion =
                                            "Error leyendo el archivo recibido: " +
                                            ex.Message,
                                        StackTrace = ex.StackTrace,
                                        Fecha = DateTime.Now,
                                        JSON = JsonConvert.SerializeObject(ex)
                                    };

                                    db.BitacoraErrores.Add(bt);
                                    db.SaveChanges();

                                    ByteArrayPDF = new byte[0];
                                    TipoAdjunto = "";
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

                            if (ticketRelacionado != null &&
       ticketRelacionado.TicketPrincipalId.HasValue)
                            {
                                var idPrincipal =
                                    ticketRelacionado.TicketPrincipalId.Value;

                                ticketRelacionado = db.Tickets.FirstOrDefault(
                                    t => t.id == idPrincipal
                                );
                            }

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

                                    var tieneAdjuntos = message.Attachments != null &&
                      message.Attachments
                          .Cast<Attachment>()
                          .Any(a =>
                              a.ContentDisposition == null ||
                              !a.ContentDisposition.Inline
                          );

                                    if (!string.IsNullOrWhiteSpace(textoRespuesta) ||
                                        tieneAdjuntos)
                                    {
                                        var nuevaRespuesta = new Respuestas
                                        {
                                            idTicket = ticketRelacionado.id,
                                            idUsuario = 0,

                                            Respuesta =
                       "<div>" +
    (
        string.IsNullOrWhiteSpace(textoRespuesta)
            ? "El cliente adjuntó uno o más archivos."
            : HttpUtility.HtmlEncode(textoRespuesta)
                .Replace("\r\n", "<br>")
                .Replace("\n", "<br>")
    ) +
                               "</div><!-- correo:" +
    HttpUtility.HtmlEncode(messageId ?? "") +
    " -->",

                                            EsNotaInterna = false,
                                            FechaCreacion = DateTime.Now
                                        };

                                        db.Respuestas.Add(nuevaRespuesta);

                                        // Guardar los archivos enviados en la respuesta del cliente.
                                        GuardarAdjuntosDelCorreo(
                                            message,
                                            ticketRelacionado.id
                                        );

                                        // Registrar la fecha únicamente si estaba cerrado.
                                        if (ticketRelacionado.Status == "C")
                                        {
                                            ticketRelacionado.FechaReapertura =
                                                DateTime.Now;
                                        }

                                        // Si el cliente respondió, abrir nuevamente el ticket.
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

                    // Guardar el archivo recibido en la tabla de adjuntos.
                    if (item.Adjuntos != null &&
                        item.Adjuntos.Length > 0)
                    {
                        var extension = (item.TipoAdjunto ?? "")
                            .Trim()
                            .TrimStart('.')
                            .ToLowerInvariant();

                        var tipoContenido = ObtenerTipoContenido(extension);

                        var nombreArchivo = string.IsNullOrWhiteSpace(extension)
                            ? "archivo"
                            : "archivo-recibido." + extension;

                        var adjuntoDataUrl =
                            "data:" + tipoContenido +
                            ";name=" + Uri.EscapeDataString(nombreArchivo) +
                            ";base64," +
                            Convert.ToBase64String(item.Adjuntos);

                        db.Adjuntos.Add(new Adjuntos
                        {
                            idTicket = ti.id,
                            Adjunto = adjuntoDataUrl
                        });

                        db.SaveChanges();
                    }

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


        public HttpResponseMessage Get(
        [FromUri] Filtros filtro)
        {
            try
            {
                if (filtro == null)
                {
                    filtro = new Filtros();
                }

                var consulta = db.Tickets
                    .AsNoTracking()
                    .AsQueryable();

                var fechaVacia = DateTime.MinValue;

                // Aplicar fechas únicamente cuando sean válidas.
                if (filtro.FechaInicial != fechaVacia)
                {
                    var fechaInicial =
                        filtro.FechaInicial.Date;

                    consulta = consulta.Where(ticket =>
                        ticket.FechaTicket >= fechaInicial
                    );
                }

                if (filtro.FechaFinal != fechaVacia)
                {
                    // Incluye todo el día final.
                    var fechaFinalExclusiva =
                        filtro.FechaFinal.Date.AddDays(1);

                    consulta = consulta.Where(ticket =>
                        ticket.FechaTicket <
                        fechaFinalExclusiva
                    );
                }

                if (!string.IsNullOrWhiteSpace(filtro.Texto))
                {
                    var texto = filtro.Texto.Trim();

                    consulta = consulta.Where(ticket =>
                        (ticket.Asunto != null &&
                         ticket.Asunto.Contains(texto))
                        ||
                        (ticket.Mensaje != null &&
                         ticket.Mensaje.Contains(texto))
                    );
                }

                if (filtro.Codigo1 > 0)
                {
                    consulta = consulta.Where(ticket =>
                        ticket.idLoginAsignado ==
                        filtro.Codigo1
                    );
                }

                if (filtro.Codigo2 > 0)
                {
                    consulta = consulta.Where(ticket =>
                        ticket.idEmpresa ==
                        filtro.Codigo2
                    );
                }

                if (!string.IsNullOrWhiteSpace(filtro.Texto2) &&
                    filtro.Texto2 != "N")
                {
                    if (filtro.Texto2 == "AV")
                    {
                        consulta = consulta.Where(ticket =>
                            ticket.Status == "A" ||
                            ticket.Status == "V"
                        );
                    }
                    else
                    {
                        var estado = filtro.Texto2;

                        consulta = consulta.Where(ticket =>
                            ticket.Status == estado
                        );
                    }
                }
                if (!string.IsNullOrWhiteSpace(filtro.Texto3) &&
    filtro.Texto3 != "N")
                {
                    var tipo = filtro.Texto3.Trim();

                    consulta = consulta.Where(ticket =>
                        ticket.Tipo == tipo
                    );
                }

                var tiquetes = consulta
                    .OrderByDescending(ticket =>
                        ticket.FechaTicket
                    )
                .Select(ticket => new
                {
                    ticket.id,
                    ticket.FechaTicket,
                    FechaReapertura = (DateTime?)ticket.FechaReapertura,
                    ticket.Asunto,
                    ticket.idLoginAsignado,
                    ticket.Duracion,
                    ticket.DuracionEstimada,
                    ticket.idEmpresa,
                    ticket.Status,
                    ticket.PersonaTicket,
                    ticket.Tipo
                })
                    .ToList();

                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    tiquetes
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
                var ticket = db.Tickets.FirstOrDefault(
                    a => a.id == t.id
                );

                if (ticket == null)
                {
                    throw new Exception("El tiquete no existe.");
                }

                var estadoAnterior = ticket.Status;

                var seEstaCerrando = estadoAnterior != "C" && t.Status == "C";

                // Enviar correo de asignación únicamente si no se está cerrando.
                if (estadoAnterior == "E" && t.Status != "C")
                {
                    try
                    {
                        var usuario = db.Login.FirstOrDefault(
                            a => a.id == t.idLoginAsignado
                        );

                        if (usuario == null)
                        {
                            throw new Exception("El usuario asignado no existe.");
                        }

                        var correo = db.CorreosRecepcion.FirstOrDefault();

                        if (correo == null)
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
                            "<body style='font-family:Arial,sans-serif;'>" +
                            "<div style='max-width:700px;margin:auto;'>" +
                            "<p>Estimado usuario, se le ha asignado un nuevo tiquete:</p>" +
                            "<p><strong>ID:</strong> @ID</p>" +
                            "<p><strong>Asunto:</strong> @ASUNTO</p>" +
                            "<div style='margin-top:20px;'>@MENSAJE</div>" +
                            "</div>" +
                            "</body>" +
                            "</html>";

                        html = html.Replace(
                            "@ID",
                            ticket.id.ToString()
                        );

                        html = html.Replace(
                            "@ASUNTO",
                            HttpUtility.HtmlEncode(
                                ticket.Asunto ?? ""
                            )
                        );

                        html = html.Replace(
                            "@MENSAJE",
                            ticket.Mensaje ?? ""
                        );

                        var imagenesInline =
                            PrepararImagenesInlineTicket(ref html);

                        var servicioCorreo = new G();

                        var enviado = servicioCorreo.SendV2(
                            usuario.Email,
                            "",
                            "",
                            correo.RecepcionEmail,
                            "TICKET",
                            "NUEVO TIQUETE ASIGNADO",
                            html,
                            correo.RecepcionHostName,
                            587,
                            correo.RecepcionUseSSL.Value,
                            correo.RecepcionEmail,
                            correo.RecepcionPassword,
                            imagenesInline
                        );

                        if (!enviado)
                        {
                            throw new Exception(
                                "No se pudo enviar el correo de asignación."
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        var bitacoraAsignacion = new BitacoraErrores
                        {
                            Descripcion = ex.Message,
                            StackTrace = ex.StackTrace,
                            Fecha = DateTime.Now,
                            JSON = JsonConvert.SerializeObject(ex)
                        };

                        db.BitacoraErrores.Add(bitacoraAsignacion);
                        db.SaveChanges();
                    }
                }

                if (seEstaCerrando)
                {
                    // Si falla el correo, se genera la excepción
                    // y el tiquete no se cierra.
                    EnviarNotificacionCierre(ticket);
                }

                db.Entry(ticket).State = EntityState.Modified;

                ticket.Duracion = t.Duracion;
                ticket.idLoginAsignado = t.idLoginAsignado;
                ticket.Comentarios = t.Comentarios;
                ticket.idEmpresa = t.idEmpresa;
                ticket.DuracionEstimada = t.DuracionEstimada;
                ticket.Status = t.Status;
                ticket.Tipo = t.Tipo;

                if (seEstaCerrando)
                {
                    ticket.FechaCierre = DateTime.Now;
                }

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, ticket);
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

                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex);
            }
        }
        private void EnviarNotificacionCierre(Tickets ticket)
        {
            if (ticket == null)
            {
                throw new Exception(
                    "El tiquete no existe."
                );
            }

            if (string.IsNullOrWhiteSpace(ticket.PersonaTicket))
            {
                throw new Exception(
                    "El tiquete no tiene un correo registrado."
                );
            }

            var correoEnvio = db.CorreoEnvio.FirstOrDefault();

            if (correoEnvio == null)
            {
                throw new Exception(
                    "No existe una configuración para enviar correos."
                );
            }

            if (correoEnvio.EnvioPort == null ||
                correoEnvio.RecepcionUseSSL == null)
            {
                throw new Exception(
                    "La configuración del correo está incompleta."
                );
            }

            var asuntoTicket = HttpUtility.HtmlEncode(
                ticket.Asunto ?? ""
            );

            var html =
                "<!DOCTYPE html>" +
                "<html lang='es'>" +
                "<head>" +
                "<meta charset='UTF-8'>" +
                "<meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
                "</head>" +
                "<body style='margin:0;padding:0;background:#f2f3f3;font-family:Arial,sans-serif;color:#161e2d;'>" +
                "<table width='100%' cellspacing='0' cellpadding='0' style='background:#f2f3f3;padding:30px 15px;'>" +
                "<tr><td align='center'>" +
                "<table width='650' cellspacing='0' cellpadding='0' style='max-width:650px;width:100%;background:#ffffff;border:1px solid #d5dbdb;'>" +

                "<tr>" +
                "<td style='background:#131e29;border-bottom:4px solid #ff9900;padding:22px 28px;color:#ffffff;'>" +
                "<div style='font-size:12px;color:#aab7b8;'>SOPORTE</div>" +
                "<div style='font-size:22px;font-weight:bold;margin-top:5px;'>Tiquete cerrado</div>" +
                "</td>" +
                "</tr>" +

                "<tr>" +
                "<td style='padding:28px;'>" +
                "<p style='margin-top:0;'>Hola,</p>" +
                "<p>Le informamos que su solicitud de soporte fue cerrada.</p>" +

                "<div style='background:#f7f8f8;border-left:4px solid #ff9900;padding:18px;margin:22px 0;line-height:1.6;'>" +
                "<strong>Tiquete:</strong> #" + ticket.id + "<br>" +
                "<strong>Asunto:</strong> " + asuntoTicket +
                "</div>" +

                "<p>Si necesita información adicional, puede responder a este correo y el tiquete será abierto nuevamente.</p>" +

                "<p style='font-size:13px;color:#687078;margin-bottom:0;margin-top:25px;'>" +
                "Equipo de soporte" +
                "</p>" +
                "</td>" +
                "</tr>" +

                "</table>" +
                "</td></tr>" +
                "</table>" +
                "</body>" +
                "</html>";

            var asuntoCorreo = ticket.Asunto == null
                ? ""
                : ticket.Asunto.Trim();

            if (!asuntoCorreo.StartsWith(
                "RE:",
                StringComparison.OrdinalIgnoreCase))
            {
                asuntoCorreo = "Re: " + asuntoCorreo;
            }

            var servicioCorreo = new G();

            var enviado = servicioCorreo.SendV2(
                ticket.PersonaTicket,
                "",
                "",
                correoEnvio.RecepcionEmail,
                "TICKETS",
                asuntoCorreo,
                html,
                correoEnvio.RecepcionHostName,
                correoEnvio.EnvioPort,
                correoEnvio.RecepcionUseSSL,
                correoEnvio.RecepcionEmail,
                correoEnvio.RecepcionPassword,
                null,
                ticket.idCorreo
            );

            if (!enviado)
            {
                throw new Exception(
                    "No se pudo enviar la notificación de cierre a " +
                    ticket.PersonaTicket +
                    "."
                );
            }
        }
        [HttpDelete]
        [Route("api/Tiquetes/Eliminar")]
        public HttpResponseMessage Delete([FromUri] int id)
        {
            try
            {
                var ticket = db.Tickets.FirstOrDefault(
                    x => x.id == id
                );

                if (ticket == null)
                {
                    throw new Exception(
                        "El tiquete no existe."
                    );
                }

                db.Entry(ticket).State =
                    EntityState.Modified;

                if (ticket.Status == "C")
                {
                    ticket.Status = "A";
                    ticket.FechaReapertura = DateTime.Now;

                    db.SaveChanges();

                    return Request.CreateResponse(
                        HttpStatusCode.OK,
                        new
                        {
                            cerrado = false,
                            fechaReapertura = ticket.FechaReapertura,
                            mensaje = "El tiquete fue abierto correctamente."
                        }
                    );
                }


                EnviarNotificacionCierre(ticket);

                ticket.Status = "C";
                ticket.FechaCierre = DateTime.Now;

                db.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK, new { cerrado = true, mensaje = "El tiquete fue cerrado y el cliente fue notificado." });
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
                    ex.Message
                );
            }
        }
        [HttpGet]
        [Route("api/Tiquetes/LeerRespuestasTicket")]
        public HttpResponseMessage GetLeerRespuestasTicket([FromUri] int id)
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
                if (ticket != null && ticket.TicketPrincipalId.HasValue)
                {
                    var idPrincipal = ticket.TicketPrincipalId.Value;

                    ticket = db.Tickets.FirstOrDefault(
                        t => t.id == idPrincipal
                    );
                }
                if (ticket == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "El ticket no existe.");
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

                            var textoRespuesta =
     ObtenerUltimaRespuesta(mensaje.Body);

                            var tieneAdjuntos =
                                mensaje.Attachments != null &&
                                mensaje.Attachments
                                    .Cast<Attachment>()
                                    .Any(a =>
                                        a.ContentDisposition == null ||
                                        !a.ContentDisposition.Inline
                                    );

                            // Ignorar solamente si no tiene texto ni archivos.
                            if (string.IsNullOrWhiteSpace(textoRespuesta) &&
                                !tieneAdjuntos)
                            {
                                client.GetMessage(uid, true);
                                continue;
                            }

                            var contenidoRespuesta =
                                string.IsNullOrWhiteSpace(textoRespuesta)
                                    ? "El cliente adjuntó uno o más archivos."
                                    : HttpUtility.HtmlEncode(textoRespuesta)
                                        .Replace("\r\n", "<br>")
                                        .Replace("\n", "<br>");

                            var nuevaRespuesta = new Respuestas
                            {
                                idTicket = ticket.id,
                                idUsuario = 0,

                                Respuesta =
                                    "<div>" +
                                    contenidoRespuesta +
                                    "</div><!-- correo:" +
                                    HttpUtility.HtmlEncode(messageId ?? "") +
                                    " -->",

                                EsNotaInterna = false,
                                FechaCreacion = DateTime.Now
                            };

                            db.Respuestas.Add(nuevaRespuesta);

                            // Guardar PDF, Excel, Word, imágenes y demás archivos.
                            GuardarAdjuntosDelCorreo(
                                mensaje,
                                ticket.id
                            );

                            // Registrar la fecha únicamente si estaba cerrado.
                            if (ticket.Status == "C")
                            {
                                ticket.FechaReapertura = DateTime.Now;
                            }

                            // Reabrir el ticket cuando responde el cliente.
                            ticket.Status = "A";

                            db.Entry(ticket).State =
                                EntityState.Modified;

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
        [HttpPost]
        [Route("api/Tiquetes/Unificar")]
        public HttpResponseMessage UnificarTiquetes(
    [FromBody] UnificarTiquetesRequest solicitud)
        {
            if (solicitud == null)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    "Debe indicar los tiquetes que desea unificar."
                );
            }

            if (solicitud.TicketPrincipalId <= 0 ||
                solicitud.TicketSecundarioId <= 0)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    "Los números de tiquete no son válidos."
                );
            }

            if (solicitud.TicketPrincipalId ==
                solicitud.TicketSecundarioId)
            {
                return Request.CreateResponse(
                    HttpStatusCode.BadRequest,
                    "No puede unificar un tiquete consigo mismo."
                );
            }

            using (var transaccion = db.Database.BeginTransaction())
            {
                try
                {
                    var principal = db.Tickets.FirstOrDefault(
                        t => t.id == solicitud.TicketPrincipalId
                    );

                    var secundario = db.Tickets.FirstOrDefault(
                        t => t.id == solicitud.TicketSecundarioId
                    );

                    if (principal == null)
                    {
                        return Request.CreateResponse(
                            HttpStatusCode.NotFound,
                            "No se encontró el tiquete principal."
                        );
                    }

                    if (secundario == null)
                    {
                        return Request.CreateResponse(
                            HttpStatusCode.NotFound,
                            "No se encontró el tiquete secundario."
                        );
                    }

                    if (principal.TicketPrincipalId.HasValue)
                    {
                        return Request.CreateResponse(
                            HttpStatusCode.BadRequest,
                            "El tiquete seleccionado como principal ya fue unificado con otro."
                        );
                    }

                    if (secundario.TicketPrincipalId.HasValue)
                    {
                        return Request.CreateResponse(
                            HttpStatusCode.BadRequest,
                            "El tiquete secundario ya fue unificado anteriormente."
                        );
                    }

                    /*
                     * Guardar el mensaje original del tiquete secundario
                     * dentro del historial del principal.
                     */
                    var mensajeOriginal = new Respuestas
                    {
                        idTicket = principal.id,
                        idUsuario = 0,
                        Respuesta =
                            "<div>" +
                            "<strong>Mensaje original del tiquete #" +
                            secundario.id +
                            ":</strong><br><br>" +
                            (secundario.Mensaje ?? "") +
                            "</div>",
                        EsNotaInterna = true,
                        FechaCreacion =
                            secundario.FechaTicket ?? DateTime.Now
                    };

                    db.Respuestas.Add(mensajeOriginal);

                    /*
                     * Mover las respuestas del tiquete secundario
                     * hacia el principal.
                     */
                    var respuestasSecundarias = db.Respuestas
                        .Where(r => r.idTicket == secundario.id)
                        .ToList();

                    foreach (var respuesta in respuestasSecundarias)
                    {
                        respuesta.idTicket = principal.id;
                    }

                    /*
                     * Mover todos los adjuntos hacia el principal.
                     */
                    var adjuntosSecundarios = db.Adjuntos
                        .Where(a => a.idTicket == secundario.id)
                        .ToList();

                    foreach (var adjunto in adjuntosSecundarios)
                    {
                        adjunto.idTicket = principal.id;
                    }

                    /*
                     * Registrar la unificación en el historial.
                     */
                    db.Respuestas.Add(new Respuestas
                    {
                        idTicket = principal.id,
                        idUsuario = 0,
                        Respuesta =
                            "<div><strong>Tiquete unificado:</strong> " +
                            "el tiquete #" +
                            secundario.id +
                            " fue unificado con este tiquete.</div>",
                        EsNotaInterna = true,
                        FechaCreacion = DateTime.Now
                    });

                    /*
                     * Cerrar y vincular el tiquete secundario.
                     */
                    secundario.TicketPrincipalId = principal.id;
                    secundario.Status = "C";
                    secundario.FechaCierre = DateTime.Now;

                    db.SaveChanges();
                    transaccion.Commit();

                    return Request.CreateResponse(
                        HttpStatusCode.OK,
                        new
                        {
                            correcto = true,
                            ticketPrincipalId = principal.id,
                            ticketSecundarioId = secundario.id,
                            respuestasMovidas = respuestasSecundarias.Count,
                            adjuntosMovidos = adjuntosSecundarios.Count,
                            mensaje =
                                "Los tiquetes fueron unificados correctamente."
                        }
                    );
                }
                catch (Exception ex)
                {
                    transaccion.Rollback();

                    var bitacora = new BitacoraErrores
                    {
                        Descripcion = ex.Message,
                        StackTrace = ex.StackTrace,
                        Fecha = DateTime.Now,
                        JSON = JsonConvert.SerializeObject(ex)
                    };

                    using (var dbBitacora = new ModelCliente())
                    {
                        dbBitacora.BitacoraErrores.Add(bitacora);
                        dbBitacora.SaveChanges();
                    }

                    return Request.CreateResponse(
                        HttpStatusCode.InternalServerError,
                        "No fue posible unificar los tiquetes."
                    );
                }
            }
        }
      
    }
    public class UnificarTiquetesRequest
    {
        public int TicketPrincipalId { get; set; }

        public int TicketSecundarioId { get; set; }
    }
}