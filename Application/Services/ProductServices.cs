using Core.Models;
using Core.Interfaces;

namespace Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;

        private readonly string _imageUploadPath;


        public ProductService(IProductRepository productRepository, IConfiguration configuration)
        {
            _productRepository = productRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
        }

        public bool AddProduct(Product producto)
        {
            // Validación de reglas de negocio, como contraseñas que coincidan
            if (string.IsNullOrEmpty(producto.nombreProducto) || string.IsNullOrEmpty(producto.nombreProducto))
            {
                throw new ArgumentException("Datos inválidos");
            }

            GuardarImagen(producto.ImagenFile);
            // Hash de la contraseña
            producto.nombreProducto = producto.nombreProducto;

            // Llama al repositorio para guardar el usuario
            return _productRepository.AddProduct(producto);
        }

        //public bool EditProduct(Product producto)
        //{
        //    // Validación de reglas de negocio, como contraseñas que coincidan
        //    if (string.IsNullOrEmpty(producto.nombreProducto) || string.IsNullOrEmpty(producto.nombreProducto))
        //    {
        //        throw new ArgumentException("Datos inválidos");
        //    }

        //    GuardarImagen(producto.ImagenFile);
        //    // Hash de la contraseña
        //    producto.nombreProducto = producto.nombreProducto;

        //    // Llama al repositorio para guardar el usuario
        //    return _productRepository.EditProduct(producto);
        //}

        public void GuardarImagen(IFormFile imagen)
        {
            if (imagen != null)
            {
                // Asegurar que la carpeta exista
                if (!Directory.Exists(_imageUploadPath))
                {
                    Directory.CreateDirectory(_imageUploadPath);
                }

                // Construir la ruta completa del archivo
                var filePath = Path.Combine(_imageUploadPath, imagen.FileName);

                // Guardar el archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imagen.CopyTo(stream);
                }
            }
        }
    }
}
