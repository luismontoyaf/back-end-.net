using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Core.Models;
using Core.Interfaces;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Services
{
    public class InfoRepository : IInfoRepository
    {
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public InfoRepository(string connectionString, AppDbContext context)
        {
            _connectionString = connectionString;
            _context = context;
        }

        public Client GetUserInfoByDocument(string cedula)
        {
            var client = _context.Clientes
                .Where(u => u.numDocumento == cedula)
                .Select(u => new Client
                {
                    Id = u.Id,
                    nombre = u.nombre,
                    apellidos = u.apellidos,
                    tipoDocumento = u.tipoDocumento,
                    numDocumento = u.numDocumento,
                    fechaNacimiento = (DateTime)u.fechaNacimiento,
                    direccion = u.direccion,
                    correo = u.correo,
                    contrasena = u.contrasena,
                    celular = u.celular
                })
                .FirstOrDefault();

            return client;
        }

        public string GetParameter(Info info)
        {
            var image = "";

            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
                        SELECT ValorParametro
                        FROM Parametros WHERE NombreParametro = @NombreParametro";

                using (var command = new SqlCommand(query, connection))
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

        public string GetParameterByName(string nameParameter)
        {
            var parameterValue = "";

            using (var connection = new SqlConnection(_connectionString))
            {
                string query = @"
                        SELECT ValorParametro
                        FROM Parametros WHERE NombreParametro = @NombreParametro";

                using (var command = new SqlCommand(query, connection))
                {
                    // Agregar el parámetro antes de abrir la conexión
                    command.Parameters.AddWithValue("@NombreParametro", nameParameter);

                    connection.Open();

                    // Ejecutar la consulta correctamente
                    var result = command.ExecuteScalar();
                    if (result != null)
                    {
                        parameterValue = result.ToString(); // Convertir a string si el resultado no es nulo
                    }
                }
            }

            return parameterValue;
        }

    }
}
