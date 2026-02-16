using Core.Models;
using Core.Interfaces;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    public class ReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly AppDbContext _context;


        public ReportService(IReportRepository reportRepository, AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _reportRepository = reportRepository;
        }

        public async Task<string> GetReport(int id, string startDate = null, string endDate= null)
        {

                var sql = "SELECT sp_generate_system_report(@reportId, @startDate, @endDate)";

                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = sql;

                cmd.Parameters.Add(new Npgsql.NpgsqlParameter("reportId", id));

                DateTime? start = !string.IsNullOrEmpty(startDate) ? DateTime.Parse(startDate) : (DateTime?)null;
                DateTime? end = !string.IsNullOrEmpty(endDate) ? DateTime.Parse(endDate) : (DateTime?)null;

                cmd.Parameters.Add(new Npgsql.NpgsqlParameter("startDate", start ?? (object)DBNull.Value));
                cmd.Parameters.Add(new Npgsql.NpgsqlParameter("endDate", end ?? (object)DBNull.Value));

                await _context.Database.OpenConnectionAsync();
                try
                {
                    using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection); 
                    string jsonResult = null; 

                    if (await reader.ReadAsync()) { 
                        jsonResult = reader.GetString(0); 
                    }
                    
                    return jsonResult;
                }
                catch(Exception ex)
                {
                    _context.Database.CloseConnectionAsync();
                    // Manejo de excepciones
                    Console.WriteLine($"Error al generar el reporte: {ex.Message}");
                    return ex.Message;
                }
                finally
                {
                    await _context.Database.CloseConnectionAsync();
                }
            }
    }
}
