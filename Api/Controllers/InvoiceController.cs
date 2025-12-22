using Microsoft.AspNetCore.Mvc;
using Core.Models;
using Infrastructure.Services;
using Application.Services;
using Infrastructure.Data;
using Core.Interfaces;

namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/invoice")]
    public class InvoiceController : Controller
    {
        private readonly InvoiceService _invoiceService;

        public InvoiceController(InvoiceService infoService, 
            EmailService emailService, 
            IUserRepository userRepository, 
            ISaleRepository saleRepository, 
            AppDbContext context)
        {
            _invoiceService = infoService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateAndSend([FromBody] InvoiceRequest request)
        {
            try
            {
                var invoice = await _invoiceService.GenerateIndividualInvoice(request);

                return File(invoice.ToArray(), "application/pdf", "factura.pdf");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("generate-multiple-invoices")]
        public async Task<IActionResult> GenerateMultipleInvoices([FromBody] List<InvoiceRequest> requests)
        {
            try
            {
                var zipBytes = await _invoiceService.GenerateZipInvoices(requests);
                return File(zipBytes, "application/zip", "Facturas.zip");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetAllInvoices")]
        public async Task<IActionResult> GetInvoices()
            {
            var result = await _invoiceService.GetListInvoices();

            return Ok(result);
        }


    }
}
