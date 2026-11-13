using Application.Services;
using Core.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : Controller
    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
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
            try
            {
                var result = _productService.AddProduct(producto);

                if (result)
                    return Ok(new { Message = "Producto agregado exitosamente" });

                return BadRequest(new { Message = "No se pudo agregar el producto" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        [HttpGet("bulk/template")]
        public IActionResult DownloadBulkTemplate()
        {
            return File(_productService.GetProductImportTemplate(), "application/vnd.ms-excel", "plantilla_productos.xls");
        }

        [HttpPost("bulk/validate")]
        public async Task<IActionResult> ValidateBulkProducts(IFormFile archivo)
        {
            var result = await _productService.ValidateProductImportAsync(archivo);
            return Ok(result);
        }

        [HttpPost("bulk/save")]
        public async Task<IActionResult> SaveBulkProducts(IFormFile archivo)
        {
            try
            {
                var total = await _productService.AddProductsFromImportAsync(archivo);
                return Ok(new { Message = "Productos guardados correctamente.", Total = total });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                return Conflict(new { Message = "Uno de los códigos de producto ya existe. Valide el archivo nuevamente." });
            }
        }

        [HttpPatch("{id}")]
        public IActionResult EditProduct(int id, [FromBody] JsonPatchDocument<Product> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest("Datos invÃ¡lidos");

            try
            {
                var result = _productService.EditProduct(id, patchDoc);

                if (!result)
                    return NotFound("Producto no encontrado");

                return Ok(new { Message = "Producto actualizado correctamente" });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
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

        [HttpPut("[Action]/{id}")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] int amountStock)
        {
            try
            {
                await _productService.UpdateStock(id, amountStock);
                return Ok(new { Message = "Stock actualizado correctamente" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}

