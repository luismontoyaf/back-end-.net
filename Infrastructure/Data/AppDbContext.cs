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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.ToTable("Ventas");

                entity.HasKey(e => e.IdFactura);

                entity.Property(e => e.IdFactura).HasColumnName("ID_FACTURA");
                entity.Property(e => e.IdCliente).HasColumnName("ID_CLIENTE");
                entity.Property(e => e.NumeroFactura).HasColumnName("NUMERO_FACTURA");
                entity.Property(e => e.JsonFactura).HasColumnName("JSON_FACTURA");
                entity.Property(e => e.FormaPago).HasColumnName("FORMA_PAGO");
                entity.Property(e => e.FechaCreacion).HasColumnName("FECHA_CREACION").HasDefaultValueSql("GETDATE()").ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<UserRefreshToken>(entity =>
            {
                entity.ToTable("UserRefreshTokens");

                entity.HasKey(e => e.IdRefreshToken);

                entity.Property(e => e.IdRefreshToken).HasColumnName("ID_REFRESH_TOKEN");
                entity.Property(e => e.UserId).HasColumnName("USER_ID");
                entity.Property(e => e.Token).HasColumnName("TOKEN");
                entity.Property(e => e.ExpiryDate).HasColumnName("EXPIRY_DATE");
            });

            modelBuilder.Entity<Variant>(entity =>
            {
                entity.ToTable("VARIANTES");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("ID_VARIANTE");
                entity.Property(e => e.Name).HasColumnName("NOMBRE_VARIANTE");
                entity.Property(e => e.JsonValues).HasColumnName("JSON_VARIANTE");
                entity.Property(e => e.State).HasColumnName("ESTADO");
            });
        }
    }
}
