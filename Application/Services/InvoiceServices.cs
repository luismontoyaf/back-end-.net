using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Services;
using Newtonsoft.Json;
using QuestPDF.Fluent;
using System.Globalization;
using System.IO.Compression;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Application.Services
{
    public class InvoiceService
    {
        private readonly IInfoRepository _infoRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly InvoiceRepository _repository;
        private readonly ISaleRepository _saleRepository;
        private readonly TenantProvider _tenantProvider;

        private readonly EmailService _emailService;
        private readonly string _imageUploadPath;


        public InvoiceService(IInfoRepository infoRepository, 
            IConfiguration configuration, 
            IInvoiceRepository invoiceRepository,
            IUserRepository userRepository,
            EmailService emailService,
            ISaleRepository saleRepository,
            TenantProvider tenantProvider
            )
        {
            _infoRepository = infoRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
            _invoiceRepository = invoiceRepository;
            _userRepository = userRepository;
            _userRepository = userRepository;
            _saleRepository = saleRepository;
            _emailService = emailService;
            _tenantProvider = tenantProvider;

        }

        public async Task<List<Sale>> GetAllInvoicesAsync()
        {
            return await _invoiceRepository.GetAllInvoices();
        }

        public async Task<byte[]> GenerateIndividualInvoice(InvoiceRequest request)
        {
            if (request == null || request.Items == null || !request.Items.Any())
                throw new ArgumentException("Datos inválidos");

            var tenantId = _tenantProvider.GetTenantId();

            var client = await _userRepository.GetClientByIdAsync(request.idClient, tenantId);
            if (client == null || client.TenantId != tenantId)
                throw new UnauthorizedAccessException("Cliente no pertenece al tenant");

            var invoice = await _saleRepository.GetInvoiceByInvoiceNumber(request.numInvoice, tenantId);
            if (invoice == null || invoice.TenantId != tenantId)
                throw new UnauthorizedAccessException("Factura no pertenece al tenant");

            string nombreEmpresa = _infoRepository.GetParameterByName("NOMBRE_EMPRESA", tenantId);
            var datosEmpresaJson = _infoRepository.GetParameterByName("DATOS_BASICOS_EMPRESA", tenantId);
            decimal valorIva = decimal.Parse(
                _infoRepository.GetParameterByName("VALOR_IVA", tenantId),
                CultureInfo.InvariantCulture);

            var datosEmpresaObj = JsonConvert.DeserializeObject<DatosEmpresaWrapper>(datosEmpresaJson);

            var tipoDocumentoMap = new Dictionary<string, string>
                {
                    { "Cédula de Ciudadanía", "CC" },
                    { "Pasaporte", "PA" },
                    { "Tarjeta de Identidad", "TI" },
                    { "Cédula de Extranjería", "CE" }
                };

            string siglasDocumento = tipoDocumentoMap.TryGetValue(client.tipoDocumento, out var codigo)
                ? codigo
                : "ND";

            var companyInfo = new DatosEmpresa
            {
                Nombre = nombreEmpresa,
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
                TotalIva = total * valorIva,
                TotalAmount = total
            };

            var document = new InvoiceDocumentService(invoiceData, companyInfo);
            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            if (invoiceData.ClientDocument != "222222222222" && request.sendEmail)
            {
                await _emailService.SendInvoiceEmailAsync(
                    invoiceData.ClientEmail,
                    invoiceData.ClientName,
                    companyInfo.Nombre,
                    companyInfo.Correo,
                    pdfStream,
                    invoiceData);
            }

            return pdfStream.ToArray();
        }

        public async Task<byte[]> GenerateZipInvoices(List<InvoiceRequest> requests)
        {
            if (requests == null || !requests.Any())
                throw new ArgumentException("No se proporcionaron facturas");

            var tenantId = _tenantProvider.GetTenantId();

            using var zipStream = new MemoryStream();
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true);

            foreach (var request in requests)
            {
                var client = await _userRepository.GetClientByIdAsync(request.idClient, tenantId);
                if (client == null || client.TenantId != tenantId)
                    throw new UnauthorizedAccessException("Cliente no pertenece al tenant");

                var invoice = await _saleRepository.GetInvoiceByInvoiceNumber(request.numInvoice, tenantId);
                if (invoice == null || invoice.TenantId != tenantId)
                    throw new UnauthorizedAccessException("Factura no pertenece al tenant");

                var datosEmpresaJson = _infoRepository.GetParameterByName("DATOS_BASICOS_EMPRESA", tenantId);
                var datosEmpresaObj = JsonConvert.DeserializeObject<DatosEmpresaWrapper>(datosEmpresaJson);

                decimal valorIva = decimal.Parse(
                    _infoRepository.GetParameterByName("VALOR_IVA", tenantId),
                    CultureInfo.InvariantCulture);

                string nombreEmpresa = _infoRepository.GetParameterByName("NOMBRE_EMPRESA", tenantId);

                var tipoDocumentoMap = new Dictionary<string, string>
                {
                    { "Cédula de Ciudadanía", "CC" },
                    { "Pasaporte", "PA" },
                    { "Tarjeta de Identidad", "TI" },
                    { "Cédula de Extranjería", "CE" }
                };

                string siglasDocumento = tipoDocumentoMap.TryGetValue(client.tipoDocumento, out var codigo)
                    ? codigo
                    : "ND";

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
                    Nombre = nombreEmpresa,
                    Nit = datosEmpresaObj.DatosEmpresa.Nit,
                    Direccion = datosEmpresaObj.DatosEmpresa.Direccion,
                    Celular = datosEmpresaObj.DatosEmpresa.Celular,
                    Correo = datosEmpresaObj.DatosEmpresa.Correo
                };

                var document = new InvoiceDocumentService(invoiceData, companyInfo);
                var pdfStream = new MemoryStream();
                document.GeneratePdf(pdfStream);
                pdfStream.Position = 0;

                var entry = archive.CreateEntry($"Factura_{invoice.NumeroFactura}_{client.nombre}.pdf");

                using var entryStream = entry.Open();
                await pdfStream.CopyToAsync(entryStream);
            }

            archive.Dispose();
            zipStream.Position = 0;

            return zipStream.ToArray();
        }

        public async Task<List<InvoiceDto>> GetListInvoices()
        {
            var tenantId = _tenantProvider.GetTenantId();

            var invoices = await _invoiceRepository.GetAllInvoices();
            var usuarios = await _userRepository.GetAllClientsAsync();

            var usuarioDict = usuarios
                .Where(u => u.TenantId == tenantId)
                .ToDictionary(u => u.Id);

            return invoices.Select(inv =>
            {
                usuarioDict.TryGetValue(inv.IdCliente, out var usuario);

                return new InvoiceDto
                {
                    IdFactura = inv.IdFactura,
                    IdCliente = inv.IdCliente,
                    TenantId = inv.TenantId,
                    NombreCliente = usuario != null ? $"{usuario.nombre} {usuario.apellidos}" : string.Empty,
                    NumeroFactura = inv.NumeroFactura ?? string.Empty,
                    JsonFactura = inv.JsonFactura.RootElement.GetRawText(),
                    FormaPago = inv.FormaPago,
                    FechaCreacion = inv.FechaCreacion ?? DateTime.MinValue
                };
            }).ToList();
        }

    }
}
