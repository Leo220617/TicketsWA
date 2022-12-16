using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

using static System.Collections.Specialized.BitVector32;

using System.Web.Configuration;
using System.Security.Claims;
 
using System.Web;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Net.Mail;

namespace WATickets.Controllers
{
    public class G
    {

        public void GuardarTxt(string nombreArchivo, string texto)
        {
            try
            {
                texto = (DateTime.Now.ToString("dd/MM/yyyy HH:mm") + " " + texto + Environment.NewLine + "------------------------------------------" + Environment.NewLine);
                System.IO.File.AppendAllText(HttpContext.Current.Server.MapPath("~") + @"\Bitacora\" + nombreArchivo, texto);


            }
            catch { }
        }

        public static string ObtenerConfig(string v)
        {
            try
            {
                return WebConfigurationManager.AppSettings[v];
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Envia Email por sendgrid  
        /// </summary>
        /// <param name="para">Destinatario de Email (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="copia">Emails a los que se desea enviar copia (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="copiaOculta">Emails a los que se desea enviar copia oculta (pueden ser varios separados por punto y coma (;))</param>
        /// <param name="de">Email que se desea aparezca como remitente </param>
        /// <param name="displayName">Nombre que se desea mostrar como remitente</param>
        /// <param name="asunto">Asunto del Email</param>
        /// <param name="html">Html del body del email a enviar</param>
        /// <returns>Retorna true si el envío fué correcto o false si falla</returns>
        public bool SendV2(string para, string copia, string copiaOculta, string de, string displayName, string asunto,
            string html, string HostServer, int Puerto, bool EnableSSL, string UserName, string Password, List<Attachment> ArchivosAdjuntos = null)
        {
            try
            {

                MailMessage mail = new MailMessage();
                mail.Subject = asunto;
                mail.Body = html;
                mail.IsBodyHtml = true;

                // * mail.From = new MailAddress(WebConfigurationManager.AppSettings["UserName"], displayName);
                mail.From = new MailAddress(de, displayName);

                var paraList = para.Split(';');
                foreach (var p in paraList)
                {
                    if (p.Trim().Length > 0)
                        mail.To.Add(p.Trim());
                }
                var ccList = copia.Split(';');
                foreach (var cc in ccList)
                {
                    if (cc.Trim().Length > 0)
                        mail.CC.Add(cc.Trim());
                }
                var ccoList = copiaOculta.Split(';');
                foreach (var cco in ccoList)
                {
                    if (cco.Trim().Length > 0)
                        mail.Bcc.Add(cco.Trim());
                }



                if (ArchivosAdjuntos != null)
                {
                    foreach (var archivo in ArchivosAdjuntos)
                    {
                        //if (!string.IsNullOrEmpty(archivo))
                        mail.Attachments.Add(archivo);
                    }
                }


                SmtpClient client = new SmtpClient();
                client.Host = HostServer; // WebConfigurationManager.AppSettings["HostName"];
                client.Port = Puerto; // int.Parse(WebConfigurationManager.AppSettings["Port"].ToString());
                client.UseDefaultCredentials = false;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.EnableSsl = EnableSSL; // bool.Parse(WebConfigurationManager.AppSettings["EnableSsl"]);
                client.Credentials = new NetworkCredential(UserName, Password);

                client.Send(mail);
                client.Dispose();
                mail.Dispose();

                return true;

            }
            catch (Exception ex)
            {


                return false;
            }
        }
    }
}