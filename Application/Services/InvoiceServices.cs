using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using System.Globalization;
using System.IO.Compression;

namespace Application.Services
{
    public class InvoiceService
    {
        private readonly IInfoRepository _infoRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly InvoiceRepository _repository;
        private readonly ISaleRepository _saleRepository;

        private readonly string _imageUploadPath;


        public InvoiceService(IInfoRepository infoRepository, 
            IConfiguration configuration, 
            IInvoiceRepository invoiceRepository,
            IUserRepository userRepository,
            EmailService emailService,
            ISaleRepository saleRepository)
        {
            _infoRepository = infoRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
            _invoiceRepository = invoiceRepository;
            _userRepository = userRepository;
            _userRepository = userRepository;
            _saleRepository = saleRepository;
        }

        public async Task<List<Sale>> GetAllInvoicesAsync()
        {
            return await _invoiceRepository.GetAllInvoices();
        }

        public async Task<byte[]> GenerateIndividualInvoice([FromBody] InvoiceRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                throw new ArgumentException("Datos inválidos");

            var client = await _userRepository.GetClientByDocumentAsync(request.idClient);

            var invoice = await _saleRepository.GetInvoiceByInvoiceNumber(request.numInvoice);

            string nombreEmpresa = _infoRepository.GetParameterByName("NOMBRE_EMPRESA");

            var datosEmpresaJson = _infoRepository.GetParameterByName("DATOS_BASICOS_EMPRESA");

            decimal valorIva = decimal.Parse(_infoRepository.GetParameterByName("VALOR_IVA"), CultureInfo.InvariantCulture);

            // Deserializa el JSON
            var datosEmpresaObj = JsonConvert.DeserializeObject<DatosEmpresaWrapper>(datosEmpresaJson);

            var tipoDocumento = client.tipoDocumento;

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
                ClientName = client.nombre + " " + client.apellidos,
                ClientEmail = client.correo,
                ClientTypeDocument = siglasDocumento,
                ClientDocument = client.numDocumento,
                ClientPhone = client.celular,
                Items = request.Items.Select(i => new InvoiceItem
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList(),
                InvoiceNumber = invoice.NumeroFactura,
                PaymentMethod = request.PaymentMethod,
                TotalIva = total * valorIva, //IVA del 19%
                TotalAmount = total
            };

            var document = new InvoiceDocumentService(invoiceData, companyInfo);
            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            //await _emailService.SendInvoiceEmailAsync(request.ClientEmail, request.ClientName, pdfStream, invoiceData);

            return pdfStream.ToArray();
            //return Ok("Factura enviada exitosamente.");
        }

        public async Task<byte[]> GenerateZipInvoices([FromBody] List<InvoiceRequest> requests)
        {
            if (requests == null || !requests.Any())
                throw new ArgumentException("No se proporcionaron facturas");

            using var zipStream = new MemoryStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);

            foreach (var request in requests)
            {
                var client = await _userRepository.GetClientByDocumentAsync(request.idClient);

                var invoice = await _saleRepository.GetInvoiceByInvoiceNumber(request.numInvoice);

                var datosEmpresaJson = _infoRepository.GetParameterByName("DATOS_BASICOS_EMPRESA");
                var datosEmpresaObj = JsonConvert.DeserializeObject<DatosEmpresaWrapper>(datosEmpresaJson);
                decimal valorIva = decimal.Parse(_infoRepository.GetParameterByName("VALOR_IVA"), CultureInfo.InvariantCulture);

                var tipoDocumento = client.tipoDocumento;
                var tipoDocumentoMap = new Dictionary<string, string>
                {
                    { "Cédula de Ciudadanía", "CC" },
                    { "Pasaporte", "PA" },
                    { "Tarjeta de Identidad", "TI" },
                    { "Cédula de Extranjería", "CE" }
                };

                string siglasDocumento = tipoDocumentoMap.TryGetValue(tipoDocumento, out var codigo) ? codigo : "ND";

                var total = request.Items.Sum(i => i.Quantity * i.UnitPrice);
                var invoiceData = new InvoiceData
                {
                    ClientName = client.nombre + " " + client.apellidos,
                    ClientEmail = client.correo,
                    ClientTypeDocument = siglasDocumento,
                    ClientDocument = client.numDocumento,
                    ClientPhone = client.celular,
                    Items = request.Items.Select(i => new InvoiceItem
                    {
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList(),
                    InvoiceNumber = invoice.NumeroFactura,
                    PaymentMethod = request.PaymentMethod,
                    TotalIva = total * valorIva,
                    TotalAmount = total
                };

                var companyInfo = new DatosEmpresa
                {
                    Nombre = _infoRepository.GetParameterByName("NOMBRE_EMPRESA"),
                    Nit = datosEmpresaObj.DatosEmpresa.Nit,
                    Direccion = datosEmpresaObj.DatosEmpresa.Direccion,
                    Celular = datosEmpresaObj.DatosEmpresa.Celular,
                    Correo = datosEmpresaObj.DatosEmpresa.Correo
                };

                var document = new InvoiceDocumentService(invoiceData, companyInfo);
                var pdfStream = new MemoryStream();
                document.GeneratePdf(pdfStream);
                pdfStream.Position = 0;

                var entry = archive.CreateEntry($"Factura_{invoice.NumeroFactura}_{invoice.NombreCliente}.pdf");
                using var entryStream = entry.Open();
                await pdfStream.CopyToAsync(entryStream);
            }

            archive.Dispose(); // Finaliza el ZIP
            zipStream.Position = 0;

            return zipStream.ToArray();
        }

        public async Task<List<Sale>> GetListInvoices()
        {
            var invoices = await GetAllInvoicesAsync();

            var usuarios = await _userRepository.GetAllClientsAsync();

            var usuarioDict = usuarios.ToDictionary(u => u.Id);

            foreach (var invoice in invoices)
            {
                if (usuarioDict.TryGetValue(invoice.IdCliente, out var usuario))
                {
                    invoice.NombreCliente = $"{usuario.nombre} {usuario.apellidos}";
                }
            }

            return invoices;
        }

    }
}
