using Core.Models;

namespace Core.Interfaces
{
    public interface IInvoiceRepository
    {
        //bool AddProduct(Product product);

        Task<List<Sale>> GetAllInvoices();
    }
}