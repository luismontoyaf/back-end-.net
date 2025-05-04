using Microsoft.AspNetCore.Mvc;
using Core.Models;
using BackendApp.Services;
using Application.Services;
using Infrastructure.Data;
using QuestPDF.Fluent;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;


namespace BackendApp.Controllers
{
    [ApiController]
    [Route("api/invoice")]
    public class InvoiceController : Controller
    {
        private readonly InvoiceRepository _repository;
        private readonly InfoRepository _infoRepository;
        private readonly InvoiceService _invoiceService;
        private readonly EmailService _emailService;

        public InvoiceController(InvoiceService infoService, EmailService emailService, AppDbContext context)
        {
            // Cadena de conexión (puedes moverla a configuración)
            string connectionString = "Server=LUISM;Database=AppData;Trusted_Connection=True;TrustServerCertificate=True;";

            _repository = new InvoiceRepository(connectionString, context);
            _infoRepository = new InfoRepository(connectionString, context);
            _invoiceService = infoService;
            _emailService = emailService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateAndSend([FromBody] InvoiceRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                return BadRequest("Datos inválidos");

            string nombreEmpresa = _infoRepository.GetParameterByName("NOMBRE_EMPRESA");

            var datosEmpresaJson = _infoRepository.GetParameterByName("DATOS_BASICOS_EMPRESA");

            decimal valorIva = decimal.Parse(_infoRepository.GetParameterByName("VALOR_IVA"), CultureInfo.InvariantCulture);

            // Deserializa el JSON
            var datosEmpresaObj = JsonConvert.DeserializeObject<DatosEmpresaWrapper>(datosEmpresaJson);

            var tipoDocumento = request.ClientTypeDocument;

            var tipoDocumentoMap = new Dictionary<string, string>
            {
                { "Cédula de Ciudadanía", "CC" },
                { "Pasaporte", "PA" },
                { "Tarjeta de Identidad", "TI" },
                { "Cédula de Extranjería", "CE" }
            };

            string siglasDocumento = tipoDocumentoMap.TryGetValue(tipoDocumento, out var codigo)
            ? codigo
            : "ND";

            var companyInfo = new DatosEmpresa
            {
                    Nombre = _infoRepository.GetParameterByName("NOMBRE_EMPRESA"),
                    Nit = datosEmpresaObj.DatosEmpresa.Nit,
                    Direccion = datosEmpresaObj.DatosEmpresa.Direccion,
                    Celular = datosEmpresaObj.DatosEmpresa.Celular,
                    Correo = datosEmpresaObj.DatosEmpresa.Correo
            };

            var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);

            var invoiceData = new InvoiceData
            {
                ClientName = request.ClientName,
                ClientEmail = request.ClientEmail,
                ClientTypeDocument = siglasDocumento,
                ClientDocument = request.ClientDocument,
                ClientPhone = request.ClientPhone,
                Items = request.Items.Select(i => new InvoiceItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                PaymentMethod = request.PaymentMethod,
                TotalIva = total * valorIva, //IVA del 19%
                TotalAmount = total
            };

            var document = new InvoiceDocumentService(invoiceData, companyInfo);
            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            //await _emailService.SendInvoiceEmailAsync(request.ClientEmail, request.ClientName, pdfStream, invoiceData);

            return File(pdfStream.ToArray(), "application/pdf", "factura.pdf");
            //return Ok("Factura enviada exitosamente.");
        }


    }
}
