using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Core.Models;
using Core.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace BackendApp.Services
{
    public class InfoRepository : IInfoRepository
    {
        private readonly string _connectionString;

        public InfoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public string GetParameter(Info info)
        {
            var image = "";

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                SELECT ValorParametro
                FROM Parametros WHERE NombreParametro = @NombreParametro";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Agregar el parámetro antes de abrir la conexión
                    command.Parameters.AddWithValue("@NombreParametro", info.nombreParametro);

                    connection.Open();

                    // Ejecutar la consulta correctamente
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        image = result.ToString(); // Convertir a string si el resultado no es nulo
                    }
                }
            }

            return image;
        }

    }
}
