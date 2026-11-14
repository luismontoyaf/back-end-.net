using Core.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Core.Interfaces
{
    public interface IProductRepository
    {
        List<Product> GetAllProducts();
        bool AddProduct(Product product);
        void EditProduct(Product product, JsonPatchDocument<Product> patchDoc);
        bool RemoveProduct(int id);
        Product GetProductById(int id);
        Task<Product> GetProductByName(string nameProduct);
        Task<bool> UpdateStockAsync(int productId, int amountStock);
        void Update(Product product);
        Task UpdateImageAsync(int productId, string nombreArchivo, byte[] imagenBytes);
        Task<HashSet<string>> GetExistingProductCodesAsync(IEnumerable<string> codes);
        bool ProductCodeExists(string code, int? excludedProductId = null);
        Task<HashSet<string>> GetExistingProductNamesAsync(IEnumerable<string> names);
        Task<int> AddProductsAsync(IEnumerable<Product> products, string imageName, byte[] imageBytes);
    }
}