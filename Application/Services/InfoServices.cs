using Core.Models;
using Core.Interfaces;

namespace Application.Services
{
    public class InfoService
    {
        private readonly IInfoRepository _infoRepository;

        private readonly string _imageUploadPath;


        public InfoService(IInfoRepository infoRepository, IConfiguration configuration)
        {
            _infoRepository = infoRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
        }

        //public bool AddProduct(Product producto)
        //{
        //    // Validación de reglas de negocio, como contraseñas que coincidan
        //    if (string.IsNullOrEmpty(producto.nombreProducto) || string.IsNullOrEmpty(producto.nombreProducto))
        //    {
        //        throw new ArgumentException("Datos inválidos");
        //    }

        //    GuardarImagen(producto.imagen);
        //    // Hash de la contraseña
        //    producto.nombreProducto = producto.nombreProducto;

        //    // Llama al repositorio para guardar el usuario
        //    return _productRepository.AddProduct(producto);
        //}
    }
}
