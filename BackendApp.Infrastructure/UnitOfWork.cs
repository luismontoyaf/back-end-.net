using Core.Interfaces;
using Data;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        private readonly ITenantProvider _tenantProvider;

        string connectionString;

        public UnitOfWork(AppDbContext context, ITenantProvider tenantProvider, IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
            _context = context;
            Ventas = new SaleRepository(context, tenantProvider);
            Clientes = new UserRepository(connectionString, context);
            Usuarios = new UserRepository(connectionString, context);
            Variants = new VariantRepository(context, tenantProvider);
            Expenses = new ExpensesRepository(tenantProvider, context);
        }

        public ISaleRepository Ventas { get; }
        public IUserRepository Clientes { get; }
        public IUserRepository Usuarios { get; }
        public IVariantRepository Variants { get; }
        public IExpensesRepository Expenses { get; }


        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}




