using Microsoft.AspNetCore.Mvc;
using Core.Models;
using BackendApp.Services;
using Application.Services;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly ProductRepository _repository;

        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _repository = new ProductRepository(connectionString);
            _productService = productService;
        }

        [HttpGet("getProducts")]
        public IActionResult GetProducts()
        {
            List<Product> products = _repository.GetAllProducts();
            return Ok(products); // Devuelve los productos en JSON
        }

        [HttpPost("addProduct")]
        public IActionResult AddProduct([FromForm] Product producto)
        {
            if (producto.imagen != null)
            {
                // Procesar la imagen
                var filePath = Path.Combine("Uploads", producto.imagen.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    producto.imagen.CopyTo(stream);
                }
            }

            var result = _productService.AddProduct(producto);
            if (result)
                return Ok(new { Message = "Producto agregado exitosamente" });

            return BadRequest(new { Message = "No se pudo agregar el producto" });
        }
    }
}
