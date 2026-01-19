using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Microsoft.AspNetCore.JsonPatch;
using Infrastructure.Data;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly ProductRepository _repository;

        private readonly ProductService _productService;



        public ProductController(ProductService productService, AppDbContext context)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _repository = new ProductRepository(connectionString, context);
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
            var result = _productService.AddProduct(producto);
            if (result)
                return Ok(new { Message = "Producto agregado exitosamente" });

            return BadRequest(new { Message = "No se pudo agregar el producto" });
        }

        [HttpPatch("{id}")]
        public IActionResult EditProduct(int id, [FromBody] JsonPatchDocument<Product> patchDoc)
        {
            if (patchDoc == null)
            {
                return BadRequest("Datos de actualización inválidos.");
            }

            var product = _repository.GetProductById(id);
            if (product == null)
            {
                return NotFound("Producto no encontrado.");
            }

            patchDoc.ApplyTo(product, error => ModelState.AddModelError("", error.ErrorMessage));

            //if (product.ImagenFile != null)
            //{
            //    // Procesar la imagen
            //    var filePath = Path.Combine("Uploads", product.ImagenFile.FileName);
            //    using (var stream = new FileStream(filePath, FileMode.Create))
            //    {
            //        product.ImagenFile.CopyTo(stream);
            //    }
            //    using (var memoryStream = new MemoryStream())
            //    {
            //        // Leer el archivo de la imagen en un stream de memoria
            //        product.ImagenFile.CopyTo(memoryStream);
            //        byte[] imageBytes = memoryStream.ToArray();

            //        product.ImagenBase64 = imageBytes.ToString();

            //    }
            //}

            // Verifica si el modelo es válido después de la actualización
            //if (!TryValidateModel(product))
            //{
            //    return BadRequest(ModelState);
            //}

            // Guarda los cambios en la BD
            _repository.EditProduct(product, patchDoc);

            return Ok(product);
        }

        [HttpDelete("{id}")]
        public IActionResult RemoveProduct(int id)
        {
            var result = _repository.RemoveProduct(id);

            if (result)
                return Ok(new { Message = "Producto eliminado correctamente" });

            return BadRequest(new { Message = "No se pudo eliminar el producto" });
        }

        [HttpGet("GetProductById/{id}")]
        public IActionResult GetProductById(int id) {
            var product = _repository.GetProductById(id);

            if (product == null)
            {
                return NotFound("Producto no encontrado.");
            }

            return Ok(product);
        }

        [HttpPut("{id}/image")]
        public async Task<IActionResult> UpdateProductImage(int id, IFormFile imagen)
        {
            if (imagen == null || imagen.Length == 0)
                return BadRequest("No se envió imagen");

            var product = _repository.GetProductById(id);
            if (product == null) return NotFound();

            await _productService.UpdateProductImageAsync(id, imagen);

            return Ok(product);
        }
    }
}
