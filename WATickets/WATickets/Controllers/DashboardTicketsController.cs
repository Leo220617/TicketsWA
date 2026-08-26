using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WATickets.Models;
using WATickets.Models.Cliente;
using WATickets.Models.Dashboard;

namespace WATickets.Controllers
{
    [Authorize]
    public class DashboardTicketsController : ApiController
    {
        private readonly ModelCliente db = new ModelCliente();

        [HttpGet]
        public async Task<HttpResponseMessage> Get([FromUri] Filtros filtro)
        {
            try
            {
                filtro = filtro ?? new Filtros();

                var hoy = DateTime.Today;
                var fechaInicial = filtro.FechaInicial == DateTime.MinValue
                    ? new DateTime(hoy.Year, 1, 1)
                    : filtro.FechaInicial.Date;

                var fechaFinal = filtro.FechaFinal == DateTime.MinValue
                    ? hoy
                    : filtro.FechaFinal.Date;

                if (fechaFinal < fechaInicial)
                {
                    var temporal = fechaInicial;
                    fechaInicial = fechaFinal;
                    fechaFinal = temporal;
                }

                var respuesta = new DashboardTicketsRespuesta();
                var conexion = db.Database.Connection;

                if (conexion.State != ConnectionState.Open)
                {
                    await conexion.OpenAsync();
                }

                using (var comando = conexion.CreateCommand())
                {
                    comando.CommandText = "dbo.SP_DASHBOARD_TICKETS";
                    comando.CommandType = CommandType.StoredProcedure;
                    comando.CommandTimeout = 120;

                    AgregarParametro(
                        comando,
                        "@FechaInicial",
                        DbType.Date,
                        fechaInicial
                    );

                    AgregarParametro(
                        comando,
                        "@FechaFinal",
                        DbType.Date,
                        fechaFinal
                    );

                    AgregarParametro(
                        comando,
                        "@IdUsuario",
                        DbType.Int32,
                        filtro.Codigo1
                    );

                    AgregarParametro(
                        comando,
                        "@Tipo",
                        DbType.String,
                        string.IsNullOrWhiteSpace(filtro.Texto3)
                            ? "N"
                            : filtro.Texto3.Trim()
                    );

                    using (var reader = await comando.ExecuteReaderAsync())
                    {
                        // Resultado 1: indicadores.
                        if (await reader.ReadAsync())
                        {
                            respuesta.Total = IntValue(reader, "Total");
                            respuesta.Abiertos = IntValue(reader, "Abiertos");
                            respuesta.Validacion = IntValue(reader, "Validacion");
                            respuesta.Espera = IntValue(reader, "Espera");
                            respuesta.Cerrados = IntValue(reader, "Cerrados");
                            respuesta.SinAsignar = IntValue(reader, "SinAsignar");
                            respuesta.HorasInvertidas = DecimalValue(reader, "HorasInvertidas");
                            respuesta.PromedioHoras = DecimalValue(reader, "PromedioHoras");
                            respuesta.TasaResolucion = DecimalValue(reader, "TasaResolucion");
                            respuesta.CumplimientoEstimado = DecimalValue(reader, "CumplimientoEstimado");
                            respuesta.EdadPromedioActivos = DecimalValue(reader, "EdadPromedioActivos");
                        }

                        // Resultado 2: estados.
                        await SiguienteResultado(reader, "estados");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Estados.Add(new DashboardCategoria
                            {
                                Etiqueta = StringValue(reader, "Etiqueta"),
                                Valor = IntValue(reader, "Valor")
                            });
                        }

                        // Resultado 3: tipos.
                        await SiguienteResultado(reader, "tipos");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Tipos.Add(new DashboardCategoria
                            {
                                Etiqueta = StringValue(reader, "Etiqueta"),
                                Valor = IntValue(reader, "Valor")
                            });
                        }

                        // Resultado 4: antigüedad.
                        await SiguienteResultado(reader, "antigüedad");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Antiguedad.Add(new DashboardCategoria
                            {
                                Etiqueta = StringValue(reader, "Etiqueta"),
                                Valor = IntValue(reader, "Valor")
                            });
                        }

                        // Resultado 5: evolución mensual.
                        await SiguienteResultado(reader, "tendencia mensual");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Tendencia.Add(new DashboardTendencia
                            {
                                Anno = IntValue(reader, "Anno"),
                                Mes = IntValue(reader, "Mes"),
                                Etiqueta = StringValue(reader, "Etiqueta"),
                                Creados = IntValue(reader, "Creados"),
                                Cerrados = IntValue(reader, "Cerrados")
                            });
                        }

                        // Resultado 6: consultores.
                        await SiguienteResultado(reader, "consultores");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Consultores.Add(new DashboardConsultor
                            {
                                Id = IntValue(reader, "Id"),
                                Nombre = StringValue(reader, "Nombre"),
                                Total = IntValue(reader, "Total"),
                                Activos = IntValue(reader, "Activos"),
                                Cerrados = IntValue(reader, "Cerrados"),
                                Horas = DecimalValue(reader, "Horas")
                            });
                        }

                        // Resultado 7: empresas.
                        await SiguienteResultado(reader, "empresas");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Empresas.Add(new DashboardEmpresa
                            {
                                Id = IntValue(reader, "Id"),
                                Nombre = StringValue(reader, "Nombre"),
                                Total = IntValue(reader, "Total"),
                                Activos = IntValue(reader, "Activos"),
                                Cerrados = IntValue(reader, "Cerrados")
                            });
                        }

                        // Resultado 8: tiquetes recientes.
                        await SiguienteResultado(reader, "tiquetes recientes");
                        while (await reader.ReadAsync())
                        {
                            respuesta.Recientes.Add(new DashboardTicketReciente
                            {
                                Id = IntValue(reader, "Id"),
                                Fecha = NullableDateTimeValue(reader, "Fecha"),
                                Asunto = StringValue(reader, "Asunto"),
                                Estado = StringValue(reader, "Estado"),
                                EstadoCodigo = StringValue(reader, "EstadoCodigo"),
                                Tipo = StringValue(reader, "Tipo"),
                                Usuario = StringValue(reader, "Usuario"),
                                Empresa = StringValue(reader, "Empresa")
                            });
                        }
                    }
                }

                // ICrudApi.ObtenerLista espera una colección.
                return Request.CreateResponse(
                    HttpStatusCode.OK,
                    new[] { respuesta }
                );
            }
            catch (Exception ex)
            {
                GuardarError(ex);

                return Request.CreateResponse(
                    HttpStatusCode.InternalServerError,
                    ex.Message
                );
            }
            finally
            {
                db.Dispose();
            }
        }

        private static void AgregarParametro(
            DbCommand comando,
            string nombre,
            DbType tipo,
            object valor)
        {
            var parametro = comando.CreateParameter();
            parametro.ParameterName = nombre;
            parametro.DbType = tipo;
            parametro.Value = valor ?? DBNull.Value;
            comando.Parameters.Add(parametro);
        }

        private static async Task SiguienteResultado(
            DbDataReader reader,
            string nombre)
        {
            if (!await reader.NextResultAsync())
            {
                throw new Exception(
                    "SP_DASHBOARD_TICKETS no devolvió el resultado de " + nombre + "."
                );
            }
        }

        private static int IntValue(IDataRecord reader, string columna)
        {
            var posicion = reader.GetOrdinal(columna);
            return reader.IsDBNull(posicion)
                ? 0
                : Convert.ToInt32(reader.GetValue(posicion));
        }

        private static decimal DecimalValue(IDataRecord reader, string columna)
        {
            var posicion = reader.GetOrdinal(columna);
            return reader.IsDBNull(posicion)
                ? 0
                : Convert.ToDecimal(reader.GetValue(posicion));
        }

        private static string StringValue(IDataRecord reader, string columna)
        {
            var posicion = reader.GetOrdinal(columna);
            return reader.IsDBNull(posicion)
                ? string.Empty
                : Convert.ToString(reader.GetValue(posicion));
        }

        private static DateTime? NullableDateTimeValue(
            IDataRecord reader,
            string columna)
        {
            var posicion = reader.GetOrdinal(columna);
            return reader.IsDBNull(posicion)
                ? (DateTime?)null
                : Convert.ToDateTime(reader.GetValue(posicion));
        }

        private static void GuardarError(Exception ex)
        {
            try
            {
                using (var errores = new ModelCliente())
                {
                    errores.BitacoraErrores.Add(new BitacoraErrores
                    {
                        Descripcion = ex.Message,
                        StackTrace = ex.StackTrace,
                        Fecha = DateTime.Now,
                        JSON = JsonConvert.SerializeObject(ex)
                    });

                    errores.SaveChanges();
                }
            }
            catch
            {
                // No ocultar el error original si falla la bitácora.
            }
        }
    }
}

namespace WATickets.Models.Dashboard
{
    using System;
    using System.Collections.Generic;

    public class DashboardTicketsRespuesta
    {
        public int Total { get; set; }
        public int Abiertos { get; set; }
        public int Validacion { get; set; }
        public int Espera { get; set; }
        public int Cerrados { get; set; }
        public int SinAsignar { get; set; }
        public decimal HorasInvertidas { get; set; }
        public decimal PromedioHoras { get; set; }
        public decimal TasaResolucion { get; set; }
        public decimal CumplimientoEstimado { get; set; }
        public decimal EdadPromedioActivos { get; set; }
        public List<DashboardCategoria> Estados { get; set; } = new List<DashboardCategoria>();
        public List<DashboardCategoria> Tipos { get; set; } = new List<DashboardCategoria>();
        public List<DashboardCategoria> Antiguedad { get; set; } = new List<DashboardCategoria>();
        public List<DashboardTendencia> Tendencia { get; set; } = new List<DashboardTendencia>();
        public List<DashboardConsultor> Consultores { get; set; } = new List<DashboardConsultor>();
        public List<DashboardEmpresa> Empresas { get; set; } = new List<DashboardEmpresa>();
        public List<DashboardTicketReciente> Recientes { get; set; } = new List<DashboardTicketReciente>();
    }

    public class DashboardCategoria
    {
        public string Etiqueta { get; set; }
        public int Valor { get; set; }
    }

    public class DashboardTendencia
    {
        public int Anno { get; set; }
        public int Mes { get; set; }
        public string Etiqueta { get; set; }
        public int Creados { get; set; }
        public int Cerrados { get; set; }
    }

    public class DashboardConsultor
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Total { get; set; }
        public int Activos { get; set; }
        public int Cerrados { get; set; }
        public decimal Horas { get; set; }
    }

    public class DashboardEmpresa
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Total { get; set; }
        public int Activos { get; set; }
        public int Cerrados { get; set; }
    }

    public class DashboardTicketReciente
    {
        public int Id { get; set; }
        public DateTime? Fecha { get; set; }
        public string Asunto { get; set; }
        public string Estado { get; set; }
        public string EstadoCodigo { get; set; }
        public string Tipo { get; set; }
        public string Usuario { get; set; }
        public string Empresa { get; set; }
    }
}
