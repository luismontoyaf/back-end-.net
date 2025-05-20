using Core.Models;

namespace Core.Interfaces
{
    public interface IUnitOfWork
    {
        ISaleRepository Ventas { get; }
        IUserRepository Clientes { get; }
        Task<int> SaveChangesAsync();
    }
}