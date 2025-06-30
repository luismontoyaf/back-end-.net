using Core.Models;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Infrastructure.Data;
using Infrastructure.Services;

namespace Application.Services
{
    public class InvoiceService
    {
        private readonly IInfoRepository _infoRepository;
        private readonly IUserRepository _userRepository;
        private readonly IInvoiceRepository _invoiceRepository;

        private readonly string _imageUploadPath;


        public InvoiceService(IInfoRepository infoRepository, 
            IConfiguration configuration, 
            IInvoiceRepository invoiceRepository,
            IUserRepository userRepository)
        {
            _infoRepository = infoRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
            _invoiceRepository = invoiceRepository;
            _userRepository = userRepository;
        }

        public async Task<List<Sale>> GetAllInvoicesAsync()
        {
            return await _invoiceRepository.GetAllInvoices();
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
