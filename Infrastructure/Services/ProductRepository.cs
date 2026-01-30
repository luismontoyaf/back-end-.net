using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Diagnostics;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

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

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                SELECT p.Id, p.NombreProducto, p.Descripcion, p.Stock, p.Precio, i.Imagen
                FROM Productos p
                LEFT JOIN ImagenesProducto i ON p.Id = i.ProductoId
                WHERE p.Activo = 1";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? imageBase64 = null;

                            if (!reader.IsDBNull(5))
                            {
                                byte[] buffer = (byte[])reader["Imagen"];
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT P.Id, P.NombreProducto, P.Descripcion, P.Precio, P.Stock, P.Activo, I.Imagen " +
                            "FROM Productos P INNER JOIN ImagenesProducto I ON P.Id = I.ProductoId WHERE P.Id = @Id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            byte[] imagenBytes = (byte[])reader["Imagen"];

                            return new Product
                            {
                                Id = reader.GetInt32(0),
                                nombreProducto = reader.GetString(1),
                                descripcion = reader.GetString(2),
                                precio = reader.GetDecimal(3),
                                stock = reader.GetInt32(4),
                                activo = reader.GetBoolean(5) ? 1 : 0,
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, NombreProducto, Descripcion, Precio, Stock, Activo " +
                            "FROM Productos WHERE NombreProducto = @NombreProducto";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NombreProducto", productName);
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
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
                                activo = reader.GetBoolean(5) ? 1 : 0
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Iniciar una transacción para asegurar que ambos inserts sean atómicos
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insertar producto en la tabla Productos
                        string insertProductQuery = @"
                            INSERT INTO Productos (NombreProducto, Descripcion, Stock, Precio)
                            OUTPUT INSERTED.Id
                            VALUES (@NombreProducto, @Descripcion, @Stock, @Precio);";

                        int productId;

                        using (var command = new SqlCommand(insertProductQuery, connection, transaction))
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
                                    INSERT INTO ImagenesProducto (ProductoId, NombreImagen, Imagen)
                                    VALUES (@ProductoId, @NombreImagen, @Imagen);";

                                using (var command = new SqlCommand(insertImageQuery, connection, transaction))
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
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteImagesQuery = @"
                            DELETE FROM ImagenesProducto
                            WHERE ProductoId = @ProductoId";

                        using (var command = new SqlCommand(deleteImagesQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ProductoId", id);
                            command.ExecuteNonQuery();
                        }

                        string deleteProductQuery = @"
                            DELETE FROM Productos
                            WHERE Id = @Id";

                        using (var command = new SqlCommand(deleteProductQuery, connection, transaction))
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
