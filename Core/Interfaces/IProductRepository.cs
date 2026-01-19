using Core.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Core.Interfaces
{
    public interface IProductRepository
    {
        bool AddProduct(Product product);
        void EditProduct(Product product, JsonPatchDocument<Product> patchDoc);
        bool RemoveProduct(int id);
        Product GetProductById(int id);
        Task UpdateImageAsync(int productId, string nombreArchivo, byte[] imagenBytes);
    }
}