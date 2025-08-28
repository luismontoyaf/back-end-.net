using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Microsoft.AspNetCore.JsonPatch;
using Infrastructure.Data;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportController : Controller
    {
        private readonly ProductRepository _repository;

        private readonly ReportService _reportService;



        public ReportController(ReportService reportService, AppDbContext context)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _reportService = reportService;
            _repository = new ProductRepository(connectionString, context);
        }

        [HttpGet("getReport")]
        public async Task<IActionResult> GetReport(int id, string startDate = null, string endDate = null)
        {
            var reports = await _reportService.GetReport(id, startDate, endDate);
            if (reports == null)
            {
                return NotFound(new { Message = "No se encontraron reportes" });
            }
            return Ok(reports); // Devuelve los productos en JSON
        }
    }
}
