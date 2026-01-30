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
        private readonly InvoiceService _invoiceService;

        public SaleController(IUnitOfWork unitOfWork, SaleService saleService, InvoiceService invoiceService)
        {
            _unitOfWork = unitOfWork;
            _saleService = saleService;
            _invoiceService = invoiceService;
        }

        [HttpPost("saveSale")]
        public async Task<IActionResult> SaveSale([FromBody] InvoiceRequest request)
        {
            try
            {
                var sale = await _saleService.SaveSaleAsync(request);

                var newRequest = new InvoiceRequest
                {
                    ClientDocument = request.ClientDocument,
                    numInvoice = sale.NumeroFactura,
                    idClient = request.idClient,
                    Items = request.Items,
                    PaymentMethod = request.PaymentMethod,
                    sendEmail = request.sendEmail
                };

                if (request.ClientDocument != "222222222222")
                {
                    await _invoiceService.GenerateIndividualInvoice(newRequest);
                }

                return Ok(new { message = "Venta guardada correctamente",
                    invoiceNumber = sale.NumeroFactura
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, detalle = ex.StackTrace });
            }
        }
    }
}
