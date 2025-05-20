using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Infrastructure.Data;
using Core.Interfaces;
using Newtonsoft.Json;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/sale")]
    public class SaleController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SaleService _saleService;

        public SaleController(IUnitOfWork unitOfWork, SaleService saleService)
        {
            _unitOfWork = unitOfWork;
            _saleService = saleService;
        }

        [HttpPost("saveSale")]
        public async Task<IActionResult> SaveSale([FromBody] InvoiceRequest request)
        {
            try
            {
                await _saleService.SaveSaleAsync(request);
                return Ok(new { message = "Venta guardada correctamente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }
    }
}
