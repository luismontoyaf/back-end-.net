using Microsoft.AspNetCore.Mvc;
using Core.Models;
using BackendApp.Services;
using Application.Services;
using Infrastructure.Data;
using QuestPDF.Fluent;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/invoice")]
    public class InvoiceController : Controller
    {
        private readonly InvoiceRepository _repository;
        private readonly InvoiceService _invoiceService;
        private readonly EmailService _emailService;

        public InvoiceController(InvoiceService infoService, EmailService emailService, AppDbContext context)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _repository = new InvoiceRepository(connectionString, context);
            _invoiceService = infoService;
            _emailService = emailService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateAndSend([FromBody] InvoiceRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest("Datos inválidos");

            var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);

            var invoiceData = new InvoiceData
            {
                ClientName = request.ClientName,
                ClientEmail = request.ClientEmail,
                Items = request.Items.Select(i => new InvoiceItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                TotalAmount = total
            };

            var document = new InvoiceDocumentService(invoiceData);
            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            //await _emailService.SendInvoiceEmailAsync(request.ClientEmail, request.ClientName, pdfStream, invoiceData);

            return File(pdfStream.ToArray(), "application/pdf", "factura.pdf");
            //return Ok("Factura enviada exitosamente.");
        }


    }
}
