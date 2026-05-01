using Application.Services;
using Core.Interfaces;
using Infrastructure.Services;

namespace Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        private readonly TenantProvider _tenantProvider;

        string connectionString;

        public UnitOfWork(AppDbContext context, TenantProvider tenantProvider, IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _context = context;
            Ventas = new SaleRepository(context, tenantProvider);
            Clientes = new UserRepository(connectionString, context);
            Usuarios = new UserRepository(connectionString, context);
            Variants = new VariantRepository(context, tenantProvider);
        }

        public ISaleRepository Ventas { get; }
        public IUserRepository Clientes { get; }
        public IUserRepository Usuarios { get; }
        public IVariantRepository Variants { get; }
        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
