using Core.Models;

namespace Core.Interfaces
{
    public interface IUnitOfWork
    {
        ISaleRepository Ventas { get; }
        IUserRepository Clientes { get; }
        IUserRepository Usuarios { get; }
        Task<int> SaveChangesAsync();
    }
}