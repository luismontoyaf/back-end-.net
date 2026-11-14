using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Core.Interfaces;
using Core.Models;
using Data;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly ITenantProvider _tenantProvider;

        public ProductRepository(string connectionString, AppDbContext context, ITenantProvider tenantProvider)
        {
            _connectionString = connectionString;
            _context = context;
            _tenantProvider = tenantProvider;
        }

        public List<Product> GetAllProducts()
        {
            var tenantId = _tenantProvider.GetTenantId();

            var products = new List<Product>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                SELECT p.id, p.nombre_producto, p.descripcion, p.stock, p.precio, p.fecha_creacion, i.imagen, p.codigo_producto
                FROM productos p
                LEFT JOIN imagenes_producto i ON p.id = i.producto_id
                WHERE p.activo = true
                AND p.tenant_id = @TenantId
                Order by p.nombre_producto ASC"; 

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? imageBase64 = reader["imagen"] is DBNull
                                ? null
                                : Convert.ToBase64String((byte[])reader["imagen"]);

                            products.Add(new Product
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                nombreProducto = reader.GetString(reader.GetOrdinal("nombre_producto")),
                                descripcion = reader.GetString(reader.GetOrdinal("descripcion")),
                                stock = reader.GetInt32(reader.GetOrdinal("stock")),
                                precio = reader.GetDecimal(reader.GetOrdinal("precio")),
                                fechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion")),
                                ImagenBase64 = imageBase64,
                                codigoProducto = reader["codigo_producto"] is DBNull
                                    ? null
                                    : reader.GetString(reader.GetOrdinal("codigo_producto"))
                            });
                        }
                    }
                }
            }

            return products;
        }

        public Product? GetProductById(int id)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var product = _context.Productos
                .Include(p => p.Imagen)
                .FirstOrDefault(p => p.Id == id && p.TenantId == tenantId);

            if (product?.Imagen != null)
            {
                product.ImagenBase64 = Convert.ToBase64String(product.Imagen.Imagen);
            }

            return product;
        }

        public async Task<Product> GetProductByName(string productName)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT id, nombre_producto, descripcion, precio, stock, activo " +
                            "FROM productos WHERE nombre_producto = @NombreProducto";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NombreProducto", productName);
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {

                            return new Product
                            {
                                Id = reader.GetInt32(0),
                                nombreProducto = reader.GetString(1),
                                descripcion = reader.GetString(2),
                                precio = reader.GetDecimal(3),
                                stock = reader.GetInt32(4),
                                activo = reader.GetBoolean(5)
                            };
                        }
                    }
                }
            }

            return null;
        }

        public async Task<bool> UpdateStockAsync(int productId, int amountStock)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var affectedRows = await _context.Productos
                .Where(product => product.Id == productId
                    && product.TenantId == tenantId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(product => product.stock,
                        product => product.stock + amountStock));

            return affectedRows > 0;
        }

        public bool AddProduct(Product product)
        {
            var tenantId = _tenantProvider.GetTenantId();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string insertProductQuery = @"
                    INSERT INTO productos (nombre_producto, codigo_producto, descripcion, stock, precio, tenant_id, id_usuario_creador)
                    VALUES (@NombreProducto, @CodigoProducto, @Descripcion, @Stock, @Precio, @TenantId, @IdUsuarioCreador)
                    RETURNING id;";

                        int productId;

                        using (var command = new NpgsqlCommand(insertProductQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@NombreProducto", product.nombreProducto);
                            command.Parameters.Add(new NpgsqlParameter("@CodigoProducto", NpgsqlTypes.NpgsqlDbType.Varchar)
                            {
                                Value = (object?)product.codigoProducto ?? DBNull.Value
                            });
                            command.Parameters.AddWithValue("@Descripcion", product.descripcion);
                            command.Parameters.AddWithValue("@Stock", product.stock);
                            command.Parameters.AddWithValue("@Precio", product.precio);
                            command.Parameters.AddWithValue("@TenantId", tenantId); 
                            command.Parameters.AddWithValue("@IdUsuarioCreador", product.idUsuarioCreador!.Value);

                            productId = (int)command.ExecuteScalar();
                        }

                        if (product.ImagenFile != null && product.ImagenFile.Length > 0)
                        {
                            using (var ms = new MemoryStream())
                            {
                                product.ImagenFile.CopyTo(ms);
                                byte[] imageBytes = ms.ToArray();

                                string insertImageQuery = @"
                            INSERT INTO imagenes_producto (producto_id, tenant_id, nombre_imagen, imagen)
                            VALUES (@ProductoId, @TenantId, @NombreImagen, @Imagen);";

                                using (var command = new NpgsqlCommand(insertImageQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@ProductoId", productId);
                                    command.Parameters.AddWithValue("@TenantId", tenantId);
                                    command.Parameters.AddWithValue("@NombreImagen", product.ImagenFile.FileName);
                                    command.Parameters.AddWithValue("@Imagen", imageBytes);

                                    command.ExecuteNonQuery();
                                }
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (PostgresException ex) when (
                        ex.SqlState == PostgresErrorCodes.UniqueViolation &&
                        ex.ConstraintName == "ux_productos_codigo_producto")
                    {
                        transaction.Rollback();
                        throw;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
        }

        public async Task<HashSet<string>> GetExistingProductCodesAsync(IEnumerable<string> codes)
        {
            var codeList = codes.Distinct().ToArray();
            if (codeList.Length == 0)
                return new HashSet<string>();

            return (await _context.Productos
                .Where(product => product.codigoProducto != null
                    && codeList.Contains(product.codigoProducto))
                .Select(product => product.codigoProducto!)
                .ToListAsync()).ToHashSet();
        }

        public bool ProductCodeExists(string code, int? excludedProductId = null)
        {
            var normalizedCode = code.Trim().ToLower();
            return _context.Productos.Any(product =>
                product.codigoProducto != null
                && product.codigoProducto.ToLower() == normalizedCode
                && (!excludedProductId.HasValue || product.Id != excludedProductId.Value));
        }

        public async Task<HashSet<string>> GetExistingProductNamesAsync(IEnumerable<string> names)
        {
            var nameList = names
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim().ToLower())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (nameList.Length == 0)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var tenantId = _tenantProvider.GetTenantId();
            var existingNames = await _context.Productos
                .Where(product => product.TenantId == tenantId
                    && nameList.Contains(product.nombreProducto.ToLower()))
                .Select(product => product.nombreProducto)
                .ToListAsync();

            return existingNames
                .Select(name => name.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public async Task<int> AddProductsAsync(IEnumerable<Product> products, string imageName, byte[] imageBytes)
        {
            var tenantId = _tenantProvider.GetTenantId();
            var productList = products.ToList();
            if (productList.Count == 0)
                return 0;

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            try
            {
                const string query = @"
                    INSERT INTO productos (nombre_producto, codigo_producto, descripcion, stock, precio, activo, tenant_id, id_usuario_creador)
                    VALUES (@NombreProducto, @CodigoProducto, @Descripcion, @Stock, @Precio, @Activo, @TenantId, @IdUsuarioCreador)
                    RETURNING id;";

                foreach (var product in productList)
                {
                    await using var command = new NpgsqlCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@NombreProducto", product.nombreProducto);
                    command.Parameters.Add("@CodigoProducto", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)product.codigoProducto ?? DBNull.Value;
                    command.Parameters.AddWithValue("@Descripcion", product.descripcion ?? string.Empty);
                    command.Parameters.AddWithValue("@Stock", product.stock);
                    command.Parameters.AddWithValue("@Precio", product.precio);
                    command.Parameters.AddWithValue("@Activo", product.activo ?? true);
                    command.Parameters.AddWithValue("@TenantId", tenantId);
                    command.Parameters.AddWithValue("@IdUsuarioCreador", product.idUsuarioCreador!.Value);
                    var productId = Convert.ToInt32(await command.ExecuteScalarAsync());

                    const string imageQuery = @"
                        INSERT INTO imagenes_producto (producto_id, tenant_id, nombre_imagen, imagen)
                        VALUES (@ProductoId, @TenantId, @NombreImagen, @Imagen);";

                    await using var imageCommand = new NpgsqlCommand(imageQuery, connection, transaction);
                    imageCommand.Parameters.AddWithValue("@ProductoId", productId);
                    imageCommand.Parameters.AddWithValue("@TenantId", tenantId);
                    imageCommand.Parameters.AddWithValue("@NombreImagen", imageName);
                    imageCommand.Parameters.AddWithValue("@Imagen", imageBytes);
                    await imageCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return productList.Count;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public void EditProduct(Product product, JsonPatchDocument<Product> patchDoc)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var existingProduct = _context.Productos
                .FirstOrDefault(p => p.Id == product.Id && p.TenantId == tenantId);

            if (existingProduct == null)
                throw new UnauthorizedAccessException("Producto no pertenece al tenant");

            foreach (var operation in patchDoc.Operations)
            {
                var propertyName = operation.path.TrimStart('/');

                var prop = typeof(Product).GetProperty(propertyName);
                if (prop == null) continue;

                if (Attribute.IsDefined(prop, typeof(NotMappedAttribute)))
                    continue;

                _context.Entry(existingProduct).Property(propertyName).IsModified = true;
            }

            _context.SaveChanges();
        }

        public Boolean RemoveProduct(int id)
        {
            var tenantId = _tenantProvider.GetTenantId();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteImagesQuery = @"
                        DELETE FROM imagenes_producto
                        WHERE producto_id = @ProductoId
                        AND producto_id IN (
                            SELECT id FROM productos 
                            WHERE tenant_id = @TenantId
                        )";

                        using (var command = new NpgsqlCommand(deleteImagesQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ProductoId", id);
                            command.Parameters.AddWithValue("@TenantId", tenantId);
                            command.ExecuteNonQuery();
                        }

                        string deleteProductQuery = @"
                        DELETE FROM productos
                        WHERE id = @Id AND tenant_id = @TenantId";

                        using (var command = new NpgsqlCommand(deleteProductQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            command.Parameters.AddWithValue("@TenantId", tenantId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Debug.WriteLine(ex.Message);
                        return false;
                    }
                }
            }
        }


        public void Update(Product product)
        {
            _context.Productos.Update(product);
        }

        public async Task UpdateImageAsync(int productId, string nombreArchivo, byte[] imagenBytes)
        {
            var tenantId = _tenantProvider.GetTenantId();

            // Traer la imagen existente (siempre existe)
            var imagenExistente = await _context.ImagenesProducto
            .Include(i => i.Producto)
            .FirstOrDefaultAsync(i => i.ProductoId == productId && i.Producto.TenantId == tenantId);

            // Actualizar los datos
            imagenExistente.NombreImagen = nombreArchivo;
            imagenExistente.Imagen = imagenBytes;

            // EF ya trackea el objeto, solo SaveChangesAsync
            await _context.SaveChangesAsync();
        }

    }
}

