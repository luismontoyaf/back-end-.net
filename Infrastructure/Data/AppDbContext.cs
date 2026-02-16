using Core.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public required DbSet<Client> Clientes { get; set; }
        public required DbSet<Employe> Usuarios { get; set; }
        public required DbSet<Product> Productos { get; set; }
        public required DbSet<Sale> Ventas { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public required DbSet<Variant> Variants { get; set; }
        public required DbSet<ImagenProducto> ImagenesProducto { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>(entity =>
            {
                entity.ToTable("clientes");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.nombre).HasColumnName("nombre");
                entity.Property(e => e.apellidos).HasColumnName("apellidos");
                entity.Property(e => e.tipoDocumento).HasColumnName("tipo_documento");
                entity.Property(e => e.numDocumento).HasColumnName("num_documento");
                entity.Property(e => e.correo).HasColumnName("correo");
                entity.Property(e => e.fechaNacimiento).HasColumnName("fecha_nacimiento").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
                entity.Property(e => e.celular).HasColumnName("celular");
                entity.Property(e => e.direccion).HasColumnName("direccion");
                entity.Property(e => e.genero).HasColumnName("genero");
                entity.Property(e => e.estado).HasColumnName("estado");
            });

            modelBuilder.Entity<Employe>(entity =>
            {
                entity.ToTable("usuarios");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.nombre).HasColumnName("nombre");
                entity.Property(e => e.apellidos).HasColumnName("apellidos");
                entity.Property(e => e.tipoDocumento).HasColumnName("tipo_documento");
                entity.Property(e => e.numDocumento).HasColumnName("num_documento");
                entity.Property(e => e.correo).HasColumnName("correo");
                entity.Property(e => e.fechaNacimiento).HasColumnName("fecha_nacimiento").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
                entity.Property(e => e.fechaIngreso).HasColumnName("fecha_ingreso").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
                entity.Property(e => e.rol).HasColumnName("rol");
                entity.Property(e => e.estado).HasColumnName("estado");
                entity.Property(e => e.contrasena).HasColumnName("contrasena");
                entity.Property(e => e.celular).HasColumnName("celular");
                entity.Property(e => e.direccion).HasColumnName("direccion");
                entity.Property(e => e.genero).HasColumnName("genero");
                entity.Property(e => e.clienteId).HasColumnName("cliente_id");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("productos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.nombreProducto).HasColumnName("nombre_producto");
                entity.Property(e => e.descripcion).HasColumnName("descripcion");
                entity.Property(e => e.stock).HasColumnName("stock");
                entity.Property(e => e.precio).HasColumnName("precio");
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.ToTable("ventas");

                entity.HasKey(e => e.IdFactura);

                entity.Property(e => e.IdFactura).HasColumnName("id_factura");
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.NumeroFactura).HasColumnName("numero_factura");
                entity.Property(e => e.JsonFactura).HasColumnName("json_factura");
                entity.Property(e => e.FormaPago).HasColumnName("forma_pago");
                entity.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<UserRefreshToken>(entity =>
            {
                entity.ToTable("user_refresh_tokens");

                entity.HasKey(e => e.IdRefreshToken);

                entity.Property(e => e.IdRefreshToken).HasColumnName("id_refresh_token");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Token).HasColumnName("token");
                entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            });

            modelBuilder.Entity<Variant>(entity =>
            {
                entity.ToTable("variantes");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id_variante");
                entity.Property(e => e.Name).HasColumnName("nombre_variante");
                entity.Property(e => e.JsonValues).HasColumnName("json_variante");
                entity.Property(e => e.State).HasColumnName("estado");
            });
        }
    }
}
