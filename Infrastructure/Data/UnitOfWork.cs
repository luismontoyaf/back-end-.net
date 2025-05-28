using Core.Interfaces;
using Infrastructure.Services;

namespace Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;

        string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Ventas = new SaleRepository(context);
            Clientes = new UserRepository(connectionString, context);
            Usuarios = new UserRepository(connectionString, context);
        }

        public ISaleRepository Ventas { get; }
        public IUserRepository Clientes { get; }
        public IUserRepository Usuarios { get; }
        public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
