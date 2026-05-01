using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;
        private readonly AppDbContext _context;
        private readonly TenantProvider _tenantProvider;

        public ProductRepository(string connectionString, AppDbContext context, TenantProvider tenantProvider)
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
                SELECT p.id, p.nombre_producto, p.descripcion, p.stock, p.precio, i.imagen
                FROM productos p
                LEFT JOIN imagenes_producto i ON p.id = i.producto_id
                WHERE p.activo = true
                AND p.tenant_id = @TenantId"; 

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? imageBase64 = null;

                            if (!reader.IsDBNull(5))
                            {
                                byte[] buffer = (byte[])reader["imagen"];
                                imageBase64 = Convert.ToBase64String(buffer);
                            }

                            products.Add(new Product
                            {
                                Id = reader.GetInt32(0),
                                nombreProducto = reader.GetString(1),
                                descripcion = reader.GetString(2),
                                stock = reader.GetInt32(3),
                                precio = reader.GetDecimal(4),
                                ImagenBase64 = imageBase64
                            });
                        }
                    }
                }
            }

            return products;
        }

        public Product GetProductById(int id)
        {
            var tenantId = _tenantProvider.GetTenantId();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                    SELECT P.id, P.tenant_id, P.nombre_producto, P.descripcion, P.precio, P.stock, P.activo, I.imagen 
                    FROM productos P 
                    INNER JOIN imagenes_producto I ON P.id = I.producto_id 
                    WHERE P.id = @Id AND P.tenant_id = @TenantId";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            byte[] imagenBytes = (byte[])reader["imagen"];

                            return new Product
                            {
                                Id = reader.GetInt32(0),
                                TenantId = reader.GetInt32(1),
                                nombreProducto = reader.GetString(2),
                                descripcion = reader.GetString(3),
                                precio = reader.GetDecimal(4),
                                stock = reader.GetInt32(5),
                                activo = reader.GetBoolean(6),
                                ImagenBase64 = Convert.ToBase64String(imagenBytes)
                            };
                        }
                    }
                }
            }

            return null;
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
                    INSERT INTO productos (nombre_producto, descripcion, stock, precio, tenant_id)
                    VALUES (@NombreProducto, @Descripcion, @Stock, @Precio, @TenantId)
                    RETURNING id;";

                        int productId;

                        using (var command = new NpgsqlCommand(insertProductQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@NombreProducto", product.nombreProducto);
                            command.Parameters.AddWithValue("@Descripcion", product.descripcion);
                            command.Parameters.AddWithValue("@Stock", product.stock);
                            command.Parameters.AddWithValue("@Precio", product.precio);
                            command.Parameters.AddWithValue("@TenantId", tenantId); 

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
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
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
