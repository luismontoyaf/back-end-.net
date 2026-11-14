using Core.Models;

namespace Core.Interfaces
{
    public interface IUnitOfWork
    {
        ISaleRepository Ventas { get; }
        IUserRepository Clientes { get; }
        IUserRepository Usuarios { get; }
        IVariantRepository Variants { get; }
        Task<int> SaveChangesAsync();
    }
}