using System.Data;
using Application.Services;
using Core.Interfaces;
using Core.Models;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Services
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly TenantProvider _tenantProvider;
        private readonly string _connectionString;
        private readonly AppDbContext _context;

        public DashboardRepository(TenantProvider tenantProvider, string connectionString, AppDbContext context)
        {
            _tenantProvider = tenantProvider;
            _connectionString = connectionString;
            _context = context;
        }

        public async Task<string> GetDashboardInfo(int tenantId)
        {
            var sql = "SELECT get_dashboard_data(@tenantId)";

            await using var connection = _context.Database.GetDbConnection();

            await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();

                command.CommandText = sql;

                var parameter = command.CreateParameter();
                parameter.ParameterName = "@tenantId";
                parameter.Value = tenantId;

                command.Parameters.Add(parameter);

                var result = await command.ExecuteScalarAsync();

                return result?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error obteniendo dashboard: {ex.Message}");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

    }
}
