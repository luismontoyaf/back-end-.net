using Core.Models;
using Core.Interfaces;

namespace Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public bool AddProduct(Product producto)
        {
            // Validación de reglas de negocio, como contraseñas que coincidan
            if (string.IsNullOrEmpty(producto.nombreProducto) || string.IsNullOrEmpty(producto.nombreProducto))
            {
                throw new ArgumentException("Datos inválidos");
            }

            // Hash de la contraseña
            producto.nombreProducto = producto.nombreProducto;

            // Llama al repositorio para guardar el usuario
            return _productRepository.AddProduct(producto);
        }
    }
}
