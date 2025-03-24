using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Core.Models;
using Core.Interfaces;

namespace BackendApp.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Product> GetAllProducts()
        {
            var products = new List<Product>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                SELECT p.Id, p.NombreProducto, p.Descripcion, p.Strock, p.Precio, i.Imagen
                FROM Productos p
                LEFT JOIN ImagenesProducto i ON p.Id = i.ProductoId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            byte[]? buffer = null;

                            if (!reader.IsDBNull(5))
                            {
                                long length = reader.GetBytes(5, 0, null, 0, 0);
                                buffer = new byte[length];
                                reader.GetBytes(5, 0, buffer, 0, (int)length);
                            }

                            // Convertimos el byte[] en un FormFile
                            IFormFile? imageFile = null;
                            if (buffer != null)
                            {
                                var stream = new MemoryStream(buffer);
                                imageFile = new FormFile(stream, 0, buffer.Length, "imagen", "imagen.jpg");
                            }

                            products.Add(new Product
                            {
                                Id = reader.GetInt32(0),
                                nombreProducto = reader.GetString(1),
                                descripcion = reader.GetString(2),
                                stock = reader.GetInt32(3),
                                precio = reader.GetDecimal(4),
                                imagen = imageFile
                            });
                        }
                    }
                }
            }

            return products;
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
                        if (product.imagen != null && product.imagen.Length > 0)
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                // Leer el archivo de la imagen en un stream de memoria
                                product.imagen.CopyTo(memoryStream);
                                byte[] imageBytes = memoryStream.ToArray();

                                // Insertar la imagen en la tabla ImagenesProducto
                                string insertImageQuery = @"
                                    INSERT INTO ImagenesProducto (ProductoId, NombreImagen, Imagen)
                                    VALUES (@ProductoId, @NombreImagen, @Imagen);";

                                using (var command = new SqlCommand(insertImageQuery, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@ProductoId", productId);
                                    command.Parameters.AddWithValue("@NombreImagen", product.imagen.FileName);
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

    }
}
