namespace WATickets.Models.Cliente
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ModelCliente : DbContext
    {
        public ModelCliente()
            : base("name=ModelCliente")
        {
        }

        public virtual DbSet<BandejaEntrada> BandejaEntrada { get; set; }
        public virtual DbSet<CorreosRecepcion> CorreosRecepcion { get; set; }
        public virtual DbSet<Login> Login { get; set; }
        public virtual DbSet<Roles> Roles { get; set; }
        public virtual DbSet<SeguridadModulos> SeguridadModulos { get; set; }
        public virtual DbSet<SeguridadRolesModulos> SeguridadRolesModulos { get; set; }
        public virtual DbSet<Tickets> Tickets { get; set; }
        public virtual DbSet<Empresas> Empresas { get; set; }
        public virtual DbSet<Adjuntos> Adjuntos { get; set; }
        public virtual DbSet<BitacoraErrores> BitacoraErrores { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BandejaEntrada>()
                .Property(e => e.Procesado)
                .IsUnicode(false);

            modelBuilder.Entity<BandejaEntrada>()
                .Property(e => e.Mensaje)
                .IsUnicode(false);

            modelBuilder.Entity<BandejaEntrada>()
                .Property(e => e.Asunto)
                .IsUnicode(false);

            modelBuilder.Entity<BandejaEntrada>()
                .Property(e => e.Remitente)
                .IsUnicode(false);

            modelBuilder.Entity<BandejaEntrada>()
                .Property(e => e.Texto)
                .IsUnicode(false);

            modelBuilder.Entity<CorreosRecepcion>()
                .Property(e => e.RecepcionEmail)
                .IsUnicode(false);

            modelBuilder.Entity<CorreosRecepcion>()
                .Property(e => e.RecepcionPassword)
                .IsUnicode(false);

            modelBuilder.Entity<CorreosRecepcion>()
                .Property(e => e.RecepcionHostName)
                .IsUnicode(false);

            modelBuilder.Entity<Login>()
                .Property(e => e.Email)
                .IsUnicode(false);

            modelBuilder.Entity<Login>()
                .Property(e => e.Nombre)
                .IsUnicode(false);

            modelBuilder.Entity<Login>()
                .Property(e => e.Clave)
                .IsUnicode(false);

            modelBuilder.Entity<Roles>()
                .Property(e => e.NombreRol)
                .IsUnicode(false);

            modelBuilder.Entity<SeguridadModulos>()
                .Property(e => e.Descripcion)
                .IsUnicode(false);

            modelBuilder.Entity<Tickets>()
                .Property(e => e.Asunto)
                .IsUnicode(false);

            modelBuilder.Entity<Tickets>()
                .Property(e => e.Mensaje)
                .IsUnicode(false);

            modelBuilder.Entity<Tickets>()
                .Property(e => e.Comentarios)
                .IsUnicode(false);

            modelBuilder.Entity<Tickets>()
                .Property(e => e.Duracion)
                .IsUnicode(false);

            modelBuilder.Entity<Tickets>()
                .Property(e => e.PersonaTicket)
                .IsUnicode(false);
        }
    }
}
