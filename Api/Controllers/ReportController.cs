using Application.Services;
using Core.Models;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportController : Controller
    {
        private readonly ReportService _reportService;

        public ReportController(ReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetReport(int id, string startDate = null, string endDate = null)
        {
            var report = await _reportService.GetReport(id, startDate, endDate);

            if (report == null)
                return NotFound(new { Message = "No se encontraron reportes" });

            return Ok(report);
        }
    }
}
