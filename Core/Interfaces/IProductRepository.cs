using Core.Models;

namespace Core.Interfaces
{
    public interface IProductRepository
    {
        bool AddProduct(Product product);
    }
}