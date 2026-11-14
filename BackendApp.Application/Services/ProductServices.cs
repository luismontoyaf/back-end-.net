using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Core.Interfaces;
using Core.Models;
using Microsoft.AspNetCore.JsonPatch;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace Application.Services
{
    public class ProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Microsoft.Extensions.Hosting.IHostEnvironment _hostEnvironment;
        private readonly string _imageUploadPath;

        public ProductService(
            IProductRepository productRepository,
            ITenantProvider tenantProvider,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            Microsoft.Extensions.Hosting.IHostEnvironment hostEnvironment)
        {
            _productRepository = productRepository;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _hostEnvironment = hostEnvironment;
            _imageUploadPath = configuration["ImageUploadPath"];
        }

        private static readonly string[] ImportHeaders =
        {
            "codigo_producto", "nombre_producto", "descripcion", "stock", "precio"
        };

        public byte[] GetProductImportTemplate()
        {
            using var workbook = new HSSFWorkbook();
            var sheet = workbook.CreateSheet("Productos");
            var header = sheet.CreateRow(0);
            var headers = ImportHeaders;
            for (var index = 0; index < headers.Length; index++)
            {
                header.CreateCell(index).SetCellValue(headers[index]);
                sheet.SetColumnWidth(index, 20 * 256);
            }

            using var stream = new MemoryStream();
            workbook.Write(stream, leaveOpen: false);
            return stream.ToArray();
        }

        public async Task<ProductImportValidationResult> ValidateProductImportAsync(IFormFile file)
        {
            var parsed = await ParseProductImportAsync(file);
            if (parsed.Result.Errores.Count > 0)
                return parsed.Result;

            var codes = parsed.Rows
                .Where(x => !string.IsNullOrWhiteSpace(x.CodigoProducto))
                .Select(x => x.CodigoProducto!)
                .ToList();
            var duplicateRows = parsed.Rows
                .Select((row, index) => new { row, index })
                .Where(x => !string.IsNullOrWhiteSpace(x.row.CodigoProducto))
                .GroupBy(x => x.row.CodigoProducto!, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (var duplicate in duplicateRows)
                foreach (var item in duplicate)
                    parsed.Result.Errores.Add(new ProductImportError { Fila = item.index + 2, Campo = "codigo_producto", Mensaje = $"El código {duplicate.Key} está repetido en el archivo." });

            var existing = await _productRepository.GetExistingProductCodesAsync(codes);
            foreach (var item in parsed.Rows.Select((row, index) => new { row, index }))
                if (!string.IsNullOrWhiteSpace(item.row.CodigoProducto) && existing.Contains(item.row.CodigoProducto))
                    parsed.Result.Errores.Add(new ProductImportError { Fila = item.index + 2, Campo = "codigo_producto", Mensaje = $"El código {item.row.CodigoProducto} ya existe." });

            var duplicateNames = parsed.Rows
                .Select((row, index) => new { row, index })
                .GroupBy(x => x.row.NombreProducto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1);

            foreach (var duplicate in duplicateNames)
                foreach (var item in duplicate)
                    parsed.Result.Errores.Add(new ProductImportError { Fila = item.index + 2, Campo = "nombre_producto", Mensaje = $"El nombre {duplicate.Key} está repetido en el archivo." });

            var names = parsed.Rows
                .Where(row => !string.IsNullOrWhiteSpace(row.NombreProducto))
                .Select(row => row.NombreProducto!.Trim())
                .ToList();
            var existingNames = await _productRepository.GetExistingProductNamesAsync(names);
            foreach (var item in parsed.Rows.Select((row, index) => new { row, index }))
                if (!string.IsNullOrWhiteSpace(item.row.NombreProducto)
                    && existingNames.Contains(item.row.NombreProducto.Trim()))
                    parsed.Result.Errores.Add(new ProductImportError { Fila = item.index + 2, Campo = "nombre_producto", Mensaje = $"El nombre {item.row.NombreProducto} ya existe." });

            return parsed.Result;
        }

        public async Task<int> AddProductsFromImportAsync(IFormFile file)
        {
            var validation = await ValidateProductImportAsync(file);
            if (!validation.Valido)
                throw new ArgumentException("El archivo contiene errores y no puede guardarse.");

            var parsed = await ParseProductImportAsync(file);
            var products = parsed.Rows.Select(row => new Product
            {
                Id = 0,
                TenantId = _tenantProvider.GetTenantId(),
                idUsuarioCreador = GetCurrentUserId(),
                codigoProducto = row.CodigoProducto,
                nombreProducto = row.NombreProducto!,
                descripcion = row.Descripcion ?? string.Empty,
                stock = row.Stock!.Value,
                precio = row.Precio!.Value,
                activo = true
            });

            var defaultImagePath = Path.Combine(_hostEnvironment.ContentRootPath, "Uploads", "noProduct.png");
            if (!File.Exists(defaultImagePath))
                throw new FileNotFoundException("No se encontró la imagen por defecto de los productos.", defaultImagePath);

            var defaultImageBytes = await File.ReadAllBytesAsync(defaultImagePath);
            return await _productRepository.AddProductsAsync(products, "noProduct.png", defaultImageBytes);
        }

        private int GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue("userId");
            if (!int.TryParse(userId, out var parsedUserId) || parsedUserId <= 0)
                throw new UnauthorizedAccessException("No se pudo identificar al usuario autenticado.");

            return parsedUserId;
        }

        private async Task<(List<ProductImportRow> Rows, ProductImportValidationResult Result)> ParseProductImportAsync(IFormFile file)
        {
            var result = new ProductImportValidationResult();
            var rows = new List<ProductImportRow>();
            if (file == null || file.Length == 0)
            {
                result.Errores.Add(new ProductImportError { Fila = 1, Campo = "archivo", Mensaje = "Debe seleccionar un archivo Excel .xls o CSV no vacío." });
                return (rows, result);
            }

            if (Path.GetExtension(file.FileName).Equals(".xls", StringComparison.OrdinalIgnoreCase))
                return ParseExcelProductImport(file);

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, true);
            var header = await reader.ReadLineAsync();
            if (header == null || !header.Split(';').Select(x => x.Trim()).SequenceEqual(ImportHeaders, StringComparer.OrdinalIgnoreCase))
            {
                result.Errores.Add(new ProductImportError { Fila = 1, Campo = "encabezado", Mensaje = "El encabezado debe ser: codigo_producto;nombre_producto;descripcion;stock;precio." });
                return (rows, result);
            }

            var lineNumber = 1;
            while (await reader.ReadLineAsync() is { } line)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                result.TotalFilas++;
                var values = line.Split(';');
                if (values.Length != ImportHeaders.Length)
                {
                    result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "fila", Mensaje = "La fila debe contener exactamente 5 columnas separadas por punto y coma." });
                    continue;
                }

                var row = new ProductImportRow { NombreProducto = values[1].Trim(), Descripcion = values[2].Trim() };
                var code = values[0].Trim();
                if (!string.IsNullOrEmpty(code) && (code.Length > 100 || code.Any(character => !char.IsLetterOrDigit(character))))
                    result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "codigo_producto", Mensaje = "Debe contener solo caracteres alfanuméricos y no superar 100 caracteres." });
                else
                    row.CodigoProducto = string.IsNullOrEmpty(code) ? null : code;
                if (string.IsNullOrWhiteSpace(row.NombreProducto) || row.NombreProducto.Length > 255)
                    result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "nombre_producto", Mensaje = "Es obligatorio y no puede superar 255 caracteres." });
                if (!int.TryParse(values[3].Trim(), out var stock) || stock < 0)
                    result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "stock", Mensaje = "Debe ser un entero mayor o igual a cero." });
                else row.Stock = stock;
                if (!decimal.TryParse(values[4].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0 || decimal.Round(price, 2) != price || price > 99999999.99m)
                    result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "precio", Mensaje = "Debe ser un número mayor o igual a cero con máximo 2 decimales." });
                else row.Precio = price;
                rows.Add(row);
            }

            if (result.TotalFilas == 0)
                result.Errores.Add(new ProductImportError { Fila = 1, Campo = "archivo", Mensaje = "El archivo no contiene productos." });
            return (rows, result);
        }

        private static (List<ProductImportRow> Rows, ProductImportValidationResult Result) ParseExcelProductImport(IFormFile file)
        {
            var result = new ProductImportValidationResult();
            var rows = new List<ProductImportRow>();
            using var stream = file.OpenReadStream();
            var workbook = new HSSFWorkbook(stream);
            var sheet = workbook.GetSheetAt(0);
            var formatter = new DataFormatter(CultureInfo.InvariantCulture);
            var header = sheet.GetRow(0);
            var expectedHeaders = ImportHeaders;

            if (header == null || expectedHeaders.Select((expected, index) => new { expected, index }).Any(item => !string.Equals(formatter.FormatCellValue(header.GetCell(item.index)).Trim(), item.expected, StringComparison.OrdinalIgnoreCase)))
            {
                result.Errores.Add(new ProductImportError { Fila = 1, Campo = "encabezado", Mensaje = "El encabezado debe contener las columnas esperadas de productos." });
                return (rows, result);
            }

            for (var rowIndex = 1; rowIndex <= sheet.LastRowNum; rowIndex++)
            {
                var excelRow = sheet.GetRow(rowIndex);
                if (excelRow == null || excelRow.Cells.All(cell => string.IsNullOrWhiteSpace(formatter.FormatCellValue(cell))))
                    continue;

                result.TotalFilas++;
                var values = expectedHeaders.Select((_, index) => formatter.FormatCellValue(excelRow.GetCell(index))?.Trim() ?? string.Empty).ToArray();
                var row = new ProductImportRow { NombreProducto = values[1], Descripcion = values[2] };
                ValidateExcelRow(values, row, rowIndex + 1, result);
                rows.Add(row);
            }

            if (result.TotalFilas == 0)
                result.Errores.Add(new ProductImportError { Fila = 1, Campo = "archivo", Mensaje = "El archivo no contiene productos." });
            return (rows, result);
        }

        private static void ValidateExcelRow(string[] values, ProductImportRow row, int lineNumber, ProductImportValidationResult result)
        {
            var code = values[0].Trim();
            if (!string.IsNullOrEmpty(code) && (code.Length > 100 || code.Any(character => !char.IsLetterOrDigit(character))))
                result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "codigo_producto", Mensaje = "Debe contener solo caracteres alfanuméricos y no superar 100 caracteres." });
            else
                row.CodigoProducto = string.IsNullOrEmpty(code) ? null : code;
            if (string.IsNullOrWhiteSpace(row.NombreProducto) || row.NombreProducto.Length > 255)
                result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "nombre_producto", Mensaje = "Es obligatorio y no puede superar 255 caracteres." });
            if (!int.TryParse(values[3], out var stock) || stock < 0)
                result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "stock", Mensaje = "Debe ser un entero mayor o igual a cero." });
            else row.Stock = stock;
            if (!decimal.TryParse(values[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0 || decimal.Round(price, 2) != price || price > 99999999.99m)
                result.Errores.Add(new ProductImportError { Fila = lineNumber, Campo = "precio", Mensaje = "Debe ser un número mayor o igual a cero con máximo 2 decimales." });
            else row.Precio = price;
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
                throw new ArgumentException("Datos invÃ¡lidos");

            if (!string.IsNullOrWhiteSpace(producto.codigoProducto)
                && _productRepository.ProductCodeExists(producto.codigoProducto))
                throw new InvalidOperationException($"El código de producto '{producto.codigoProducto}' ya existe en el sistema.");

            producto.TenantId = _tenantProvider.GetTenantId();
            producto.idUsuarioCreador = GetCurrentUserId();

            //Se desactiva metodo para no escribir archivos directamente en server.
            //GuardarImagen(producto.ImagenFile);

            return _productRepository.AddProduct(producto);
        }

        public bool EditProduct(int id, JsonPatchDocument<Product> patchDoc)
        {
            var product = _productRepository.GetProductById(id);

            if (product == null)
                return false;

            patchDoc.ApplyTo(product);

            if (!string.IsNullOrWhiteSpace(product.codigoProducto)
                && _productRepository.ProductCodeExists(product.codigoProducto, id))
                throw new InvalidOperationException($"El código de producto '{product.codigoProducto}' ya existe en el sistema.");

            _productRepository.EditProduct(product, patchDoc);

            return true;
        }

        public bool RemoveProduct(int id)
        {
            return _productRepository.RemoveProduct(id);
        }

        public async Task<bool> UpdateStock(int id, int amountStock)
        {
            if (amountStock <= 0)
            {
                throw new Exception("La cantidad debe ser mayor a cero");
            }
            var updated = await _productRepository.UpdateStockAsync(id, amountStock);

            if (!updated)
                throw new KeyNotFoundException("Producto no encontrado o el stock no puede ser negativo");

            return true;
        }

        public async Task UpdateProductImageAsync(int productId, IFormFile imagen)
        {
            if (imagen == null || imagen.Length == 0)
                throw new ArgumentException("Imagen invÃ¡lida");

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

