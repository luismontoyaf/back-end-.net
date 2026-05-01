using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly TenantProvider _tenantProvider;
        private readonly string _imageUploadPath;

        public ProductService(
            IProductRepository productRepository,
            TenantProvider tenantProvider,
            IConfiguration configuration)
        {
            _productRepository = productRepository;
            _tenantProvider = tenantProvider;
            _imageUploadPath = configuration["ImageUploadPath"];
        }

        public List<Product> GetAllProducts()
        {
            return _productRepository.GetAllProducts();
        }

        public Product GetProductById(int id)
        {
            return _productRepository.GetProductById(id);
        }

        public bool AddProduct(Product producto)
        {
            if (string.IsNullOrEmpty(producto.nombreProducto))
                throw new ArgumentException("Datos inválidos");

            producto.TenantId = _tenantProvider.GetTenantId();

            GuardarImagen(producto.ImagenFile);

            return _productRepository.AddProduct(producto);
        }

        public bool EditProduct(int id, JsonPatchDocument<Product> patchDoc)
        {
            var product = _productRepository.GetProductById(id);

            if (product == null)
                return false;

            patchDoc.ApplyTo(product);

            _productRepository.EditProduct(product, patchDoc);

            return true;
        }

        public bool RemoveProduct(int id)
        {
            return _productRepository.RemoveProduct(id);
        }

        public async Task UpdateProductImageAsync(int productId, IFormFile imagen)
        {
            if (imagen == null || imagen.Length == 0)
                throw new ArgumentException("Imagen inválida");

            using var ms = new MemoryStream();
            await imagen.CopyToAsync(ms);
            var imagenBytes = ms.ToArray();

            await _productRepository.UpdateImageAsync(productId, imagen.FileName, imagenBytes);
        }

        private void GuardarImagen(IFormFile imagen)
        {
            if (imagen != null)
            {
                if (!Directory.Exists(_imageUploadPath))
                    Directory.CreateDirectory(_imageUploadPath);

                var uniqueName = $"{Guid.NewGuid()}_{imagen.FileName}";
                var filePath = Path.Combine(_imageUploadPath, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imagen.CopyTo(stream);
                }
            }
        }
    }
}
