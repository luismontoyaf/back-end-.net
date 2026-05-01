using System.Data;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ReportService
    {
        private readonly TenantProvider _tenantProvider;
        private readonly AppDbContext _context;

        public ReportService(
            TenantProvider tenantProvider,
            AppDbContext context)
        {
            _tenantProvider = tenantProvider;
            _context = context;
        }

        public async Task<string> GetReport(int id, string startDate = null, string endDate = null)
        {
            var tenantId = _tenantProvider.GetTenantId();

            var sql = "SELECT sp_generate_system_report(@reportId, @tenantId, @startDate, @endDate)";

            using var cmd = _context.Database.GetDbConnection().CreateCommand();
            cmd.CommandText = sql;

            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("reportId", id));
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("tenantId", tenantId));

            DateTime? start = !string.IsNullOrEmpty(startDate) ? DateTime.Parse(startDate) : (DateTime?)null;
            DateTime? end = !string.IsNullOrEmpty(endDate) ? DateTime.Parse(endDate) : (DateTime?)null;

            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("startDate", start ?? (object)DBNull.Value));
            cmd.Parameters.Add(new Npgsql.NpgsqlParameter("endDate", end ?? (object)DBNull.Value));

            await _context.Database.OpenConnectionAsync();

            try
            {
                using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);

                if (await reader.ReadAsync())
                    return reader.GetString(0);

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al generar el reporte: {ex.Message}");
                return null;
            }
            finally
            {
                await _context.Database.CloseConnectionAsync();
            }
        }
    }
}
