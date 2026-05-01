using Application.Services;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly ProductService _productService;



        public ProductController(ProductService productService, AppDbContext context, IConfiguration configuration)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = configuration.GetConnectionString("DefaultConnection");

            _productService = productService;
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = _productService.GetAllProducts();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = _productService.GetProductById(id);

            if (product == null)
                return NotFound("Producto no encontrado.");

            return Ok(product);
        }

        [HttpPost]
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
                return BadRequest("Datos inválidos");

            var result = _productService.EditProduct(id, patchDoc);

            if (!result)
                return NotFound("Producto no encontrado");

            return Ok(new { Message = "Producto actualizado correctamente" });
        }

        [HttpDelete("{id}")]
        public IActionResult RemoveProduct(int id)
        {
            var result = _productService.RemoveProduct(id);

            if (result)
                return Ok(new { Message = "Producto eliminado correctamente" });

            return BadRequest(new { Message = "No se pudo eliminar el producto" });
        }

        [HttpPut("{id}/image")]
        public async Task<IActionResult> UpdateProductImage(int id, IFormFile imagen)
        {
            try
            {
                await _productService.UpdateProductImageAsync(id, imagen);
                return Ok(new { Message = "Imagen actualizada correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
