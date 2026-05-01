using System.Data;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Infrastructure.Services
{
    public class InfoRepository : IInfoRepository
    {
        private readonly TenantProvider _tenantProvider;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public InfoRepository(TenantProvider tenantProvider, string connectionString, AppDbContext context)
        {
            _tenantProvider = tenantProvider;
            _connectionString = connectionString;
            _context = context;
        }

        public Client GetUserInfoByDocument(string cedula, int tenantId)
        {
            var client = _context.Clientes
                .Where(u => u.numDocumento == cedula && u.TenantId == tenantId)
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

        public string GetParameter(string nombreParametro, int tenantId)
        {
            var value = "";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
            SELECT valor
            FROM parametros 
            WHERE nombre = @NombreParametro
            AND tenant_id = @TenantId";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NombreParametro", nombreParametro);
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();

                    var result = command.ExecuteScalar();

                    if (result != null)
                        value = result.ToString();
                }
            }

            return value;
        }

        public string GetParameterByName(string nameParameter, int tenantId)
        {
            var value = "";

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                string query = @"
            SELECT valor
            FROM parametros 
            WHERE nombre = @NombreParametro
            AND tenant_id = @TenantId";

                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NombreParametro", nameParameter);
                    command.Parameters.AddWithValue("@TenantId", tenantId);

                    connection.Open();

                    var result = command.ExecuteScalar();

                    if (result != null)
                        value = result.ToString();
                }
            }

            return value;
        }

        public bool ValidateTenant(string tenantId)
        {
            return _context.Tenants.Any(t => t.identificador == tenantId);
        }

    }
}
