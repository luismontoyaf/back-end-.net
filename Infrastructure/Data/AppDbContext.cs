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
        }
    }
}
