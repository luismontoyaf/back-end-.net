using Core.Interfaces;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public required DbSet<Client> Clientes { get; set; }
        public required DbSet<Employe> Usuarios { get; set; }
        public required DbSet<Product> Productos { get; set; }
        public required DbSet<Sale> Ventas { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public required DbSet<Variant> Variants { get; set; }
        public required DbSet<ImagenProducto> ImagenesProducto { get; set; }
        public required DbSet<Tenant> Tenants { get; set; }
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<CompanyPlanUpgradeRequest> CompanyPlanUpgradeRequests => Set<CompanyPlanUpgradeRequest>();
        public required DbSet<TenantSubscriptionPeriod> TenantSubscriptionPeriods { get; set; }
        public required DbSet<TenantSubscriptionReminderDelivery> TenantSubscriptionReminderDeliveries { get; set; }
        public required DbSet<Egreso> Egresos { get; set; }
        public required DbSet<TipoEgreso> TipoEgresos { get; set; }

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
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
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
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("productos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.idUsuarioCreador).HasColumnName("id_usuario_creador");
                entity.Property(e => e.nombreProducto).HasColumnName("nombre_producto");
                entity.Property(e => e.codigoProducto).HasColumnName("codigo_producto").HasMaxLength(100);
                entity.Property(e => e.descripcion).HasColumnName("descripcion");
                entity.Property(e => e.stock).HasColumnName("stock");
                entity.Property(e => e.precio).HasColumnName("precio");
                entity.Property(e => e.fechaCreacion).HasColumnName("fecha_creacion");
                entity.HasIndex(e => e.codigoProducto)
                    .IsUnique()
                    .HasDatabaseName("ux_productos_codigo_producto");

                entity.HasOne(p => p.Imagen)
                  .WithOne(i => i.Producto)
                  .HasForeignKey<ImagenProducto>(i => i.ProductoId);
            });

            modelBuilder.Entity<ImagenProducto>(entity =>
            {
                entity.ToTable("imagenes_producto");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductoId).HasColumnName("producto_id");
                entity.Property(e => e.tenantId).HasColumnName("tenant_id");
                entity.Property(e => e.NombreImagen).HasColumnName("nombre_imagen");
                entity.Property(e => e.Imagen).HasColumnName("imagen");
            });

            modelBuilder.Entity<Sale>(entity =>
            {
                entity.ToTable("ventas");

                entity.HasKey(e => e.IdFactura);

                entity.Property(e => e.IdFactura).HasColumnName("id_factura");
                entity.Property(e => e.IdCliente).HasColumnName("id_cliente");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.IdVendedor).HasColumnName("id_vendedor");
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
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.ActiveTenantId).HasColumnName("active_tenant_id");
                entity.Property(e => e.Token).HasColumnName("token");
                entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.ToTable("password_reset_tokens");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
                entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.HasIndex(e => e.TokenHash).IsUnique();
            });

            modelBuilder.Entity<Variant>(entity =>
            {
                entity.ToTable("variantes");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id_variante");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.Name).HasColumnName("nombre_variante");
                entity.Property(e => e.JsonValues).HasColumnName("json_variante");
                entity.Property(e => e.State).HasColumnName("estado");
            });

            modelBuilder.Entity<Tenant>(entity =>
            {
                entity.ToTable("tenants");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.nombre).HasColumnName("nombre");
                entity.Property(e => e.identificador).HasColumnName("identificador");
                entity.HasIndex(e => e.identificador).IsUnique();
                entity.Property(e => e.nit).HasColumnName("nit");
                entity.Property(e => e.direccion).HasColumnName("direccion");
                entity.Property(e => e.celular).HasColumnName("celular");
                entity.Property(e => e.correo).HasColumnName("correo");
                entity.Property(e => e.estado).HasColumnName("estado").HasDefaultValue(true);
                entity.Property(e => e.fechaCreacion).HasColumnName("fecha_creacion").HasDefaultValueSql("NOW()");
                entity.Property(e => e.fechaInicioSuscripcion).HasColumnName("fecha_inicio_suscripcion").HasColumnType("date");
                entity.Property(e => e.bloqueadoPorMora).HasColumnName("bloqueado_por_mora").HasDefaultValue(false);
                entity.Property(e => e.CompanyId).HasColumnName("company_id");
                entity.Property(e => e.IsHeadquarters).HasColumnName("is_headquarters").HasDefaultValue(false);
                entity.HasOne(e => e.Company)
                    .WithMany(company => company.Branches)
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.CompanyId);
            });

            modelBuilder.Entity<Company>(entity =>
            {
                entity.ToTable("companies");
                entity.HasKey(company => company.Id);
                entity.Property(company => company.Id).HasColumnName("id");
                entity.Property(company => company.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
                entity.Property(company => company.Nit).HasColumnName("nit").HasMaxLength(50);
                entity.Property(company => company.Email).HasColumnName("email").HasMaxLength(255);
                entity.Property(company => company.Phone).HasColumnName("phone").HasMaxLength(50);
                entity.Property(company => company.Address).HasColumnName("address").HasMaxLength(500);
                entity.Property(company => company.SubscriptionPlan)
                    .HasColumnName("subscription_plan")
                    .HasMaxLength(20)
                    .HasDefaultValue(CompanyPlan.Standard);
                entity.Property(company => company.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                entity.Property(company => company.SuspendedForOverdue).HasColumnName("suspended_for_overdue").HasDefaultValue(false);
                entity.Property(company => company.SubscriptionStartDate).HasColumnName("subscription_start_date").HasColumnType("date");
                entity.Property(company => company.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            });

            modelBuilder.Entity<CompanyPlanUpgradeRequest>(entity =>
            {
                entity.ToTable("company_plan_upgrade_requests");
                entity.HasKey(request => request.Id);
                entity.Property(request => request.Id).HasColumnName("id");
                entity.Property(request => request.CompanyId).HasColumnName("company_id");
                entity.Property(request => request.RequestedByEmail).HasColumnName("requested_by_email").HasMaxLength(255);
                entity.Property(request => request.RequestedAt)
                    .HasColumnName("requested_at")
                    .HasColumnType("timestamp with time zone");
                entity.Property(request => request.Status).HasColumnName("status").HasMaxLength(20);
                entity.Property(request => request.DecidedAt)
                    .HasColumnName("decided_at")
                    .HasColumnType("timestamp with time zone");
                entity.Property(request => request.DecidedByEmail).HasColumnName("decided_by_email").HasMaxLength(255);
                entity.HasOne(request => request.Company)
                    .WithMany()
                    .HasForeignKey(request => request.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(request => request.CompanyId)
                    .IsUnique()
                    .HasFilter("status = 'pending'")
                    .HasDatabaseName("ux_company_plan_upgrade_requests_pending");
            });

            modelBuilder.Entity<TenantSubscriptionPeriod>(entity =>
            {
                entity.ToTable("tenant_subscription_periods");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.CompanyId).HasColumnName("company_id");
                entity.Property(e => e.TenantId).HasColumnName("tenant_id");
                entity.Property(e => e.CycleNumber).HasColumnName("cycle_number");
                entity.Property(e => e.MonthNumber).HasColumnName("month_number");
                entity.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("date");
                entity.Property(e => e.PaidAt).HasColumnName("paid_at");
                entity.Property(e => e.PaymentRecordedBy).HasColumnName("payment_recorded_by");
                entity.Property(e => e.LastReminderSentAt).HasColumnName("last_reminder_sent_at");
                entity.Property(e => e.ReminderCount).HasColumnName("reminder_count").HasDefaultValue(0);
                entity.Property(e => e.ReminderClaimedUntil).HasColumnName("reminder_claimed_until");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
                entity.HasOne<Company>()
                    .WithMany()
                    .HasForeignKey(e => e.CompanyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CompanyId, e.CycleNumber, e.MonthNumber }).IsUnique();
                entity.HasIndex(e => new { e.DueDate, e.PaidAt });
            });

            modelBuilder.Entity<TenantSubscriptionReminderDelivery>(entity =>
            {
                entity.ToTable("tenant_subscription_reminder_deliveries");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.PeriodId).HasColumnName("period_id");
                entity.Property(e => e.ReminderNumber).HasColumnName("reminder_number");
                entity.Property(e => e.RecipientEmail).HasColumnName("recipient_email");
                entity.Property(e => e.SentAt).HasColumnName("sent_at");
                entity.HasOne<TenantSubscriptionPeriod>()
                    .WithMany()
                    .HasForeignKey(e => e.PeriodId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.PeriodId, e.ReminderNumber, e.RecipientEmail }).IsUnique();
            });

            modelBuilder.Entity<Egreso>(entity =>
            {
                entity.ToTable("egresos");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.TenantId)
                    .HasColumnName("tenant_id");

                entity.Property(e => e.TipoEgresoId)
                    .HasColumnName("id_tipo_egreso");

                entity.Property(e => e.Descripcion)
                    .HasColumnName("descripcion");

                entity.Property(e => e.Valor)
                    .HasColumnName("valor");

                entity.Property(e => e.Fecha)
                    .HasColumnName("fecha");

                entity.Property(e => e.Referencia)
                    .HasColumnName("referencia");

                entity.Property(e => e.CreadoPor)
                    .HasColumnName("creado_por");

                entity.HasOne(e => e.TipoEgreso)
                    .WithMany(t => t.Egresos)
                    .HasForeignKey(e => e.TipoEgresoId);

                entity.HasOne<Employe>(e => e.UsuarioRadica)
                    .WithMany()
                    .HasForeignKey(e => e.CreadoPor)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TipoEgreso>(entity =>
            {
                entity.ToTable("tipo_egreso");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Nombre)
                    .HasColumnName("nombre");

                entity.Property(e => e.Activo)
                    .HasColumnName("activo");
            });
        }
    }
}
