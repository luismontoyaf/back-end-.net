using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
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

        public ProductRepository(string connectionString, AppDbContext context)
        {
            _connectionString = connectionString;
            _context = context;
        }

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
                SELECT p.id, p.nombre_producto, p.descripcion, p.stock, p.precio, i.imagen
                FROM productos p
                LEFT JOIN imagenes_producto i ON p.id = i.producto_id
                WHERE p.activo = true";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
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
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = "SELECT P.id, P.nombre_producto, P.descripcion, P.precio, P.stock, P.activo, I.imagen " +
                            "FROM productos P INNER JOIN imagenes_producto I ON P.id = I.producto_id WHERE P.id = @Id";
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    connection.Open();

                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            byte[] imagenBytes = (byte[])reader["imagen"];

                            return new Product
                            {
                                Id = reader.GetInt32(0),
                                nombreProducto = reader.GetString(1),
                                descripcion = reader.GetString(2),
                                precio = reader.GetDecimal(3),
                                stock = reader.GetInt32(4),
                                activo = reader.GetBoolean(5),
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

        public Boolean AddProduct(Product product)
        {
            // 1. Insertar el producto en la tabla 'Productos'
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                // Iniciar una transacción para asegurar que ambos inserts sean atómicos
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insertar producto en la tabla Productos
                        string insertProductQuery = @"
                            INSERT INTO productos (nombre_producto, descripcion, stock, precio)
                            VALUES (@NombreProducto, @Descripcion, @Stock, @Precio)
                            RETURNING id;";

                        int productId;
                        using (var command = new NpgsqlCommand(insertProductQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@NombreProducto", product.nombreProducto);
                            command.Parameters.AddWithValue("@Descripcion", product.descripcion);
                            command.Parameters.AddWithValue("@Stock", product.stock);
                            command.Parameters.AddWithValue("@Precio", product.precio);

                            // Ejecutar la consulta y obtener el Id del producto insertado
                            productId = (int)command.ExecuteScalar();
                        }

                        // 2. Insertar imagen en la tabla 'ImagenesProducto'
                        if (product.ImagenFile != null && product.ImagenFile.Length > 0)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                // Leer el archivo de la imagen en un stream de memoria
                                product.ImagenFile.CopyTo(memoryStream);
                                byte[] imageBytes = memoryStream.ToArray();

                                // Insertar la imagen en la tabla ImagenesProducto
                                string insertImageQuery = @"
                                    INSERT INTO imagenes_producto (producto_id, nombre_imagen, imagen)
                                    VALUES (@ProductoId, @NombreImagen, @Imagen);";

                                using (var command = new NpgsqlCommand(insertImageQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@ProductoId", productId);
                                    command.Parameters.AddWithValue("@NombreImagen", product.ImagenFile.FileName);
                                    command.Parameters.AddWithValue("@Imagen", imageBytes);

                                    command.ExecuteNonQuery();
                                }
                            }
                        }

                        // Commit de la transacción si ambos inserts fueron exitosos
                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Si algo falla, hacer rollback de la transacción
                        transaction.Rollback();
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
            }
        }

        public void EditProduct(Product product, JsonPatchDocument<Product> patchDoc)
        {
            foreach (var operation in patchDoc.Operations)
            {
                var propertyName = operation.path.TrimStart('/');

                var prop = typeof(Product).GetProperty(propertyName);
                if (prop == null) continue;

                if (Attribute.IsDefined(prop, typeof(NotMappedAttribute)))
                    continue;

                // Marca solo las propiedades modificadas
                _context.Entry(product).Property(propertyName).IsModified = true;
            }

            _context.SaveChanges();
        }

        public Boolean RemoveProduct(int id)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteImagesQuery = @"
                            DELETE FROM imagenes_producto
                            WHERE producto_id = @ProductoId";
                        using (var command = new NpgsqlCommand(deleteImagesQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ProductoId", id);
                            command.ExecuteNonQuery();
                        }

                        string deleteProductQuery = @"
                            DELETE FROM productos
                            WHERE id = @Id";

                        using (var command = new NpgsqlCommand(deleteProductQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
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
            // Traer la imagen existente (siempre existe)
            var imagenExistente = await _context.ImagenesProducto
                .FirstAsync(i => i.ProductoId == productId);

            // Actualizar los datos
            imagenExistente.NombreImagen = nombreArchivo;
            imagenExistente.Imagen = imagenBytes;

            // EF ya trackea el objeto, solo SaveChangesAsync
            await _context.SaveChangesAsync();
        }

    }
}
