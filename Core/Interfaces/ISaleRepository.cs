using Core.Models;

namespace Core.Interfaces
{
    public interface ISaleRepository
    {
        Task AddAsync(Sale sale);
        Task<int> GetLastInvoiceNumber();
        Task<Sale> GetInvoiceByClientId(int idCliente);
    }
}