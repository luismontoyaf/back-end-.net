using Core.Models;
using Core.Interfaces;

namespace Application.Services
{
    public class InvoiceService
    {
        private readonly IInfoRepository _infoRepository;

        private readonly string _imageUploadPath;


        public InvoiceService(IInfoRepository infoRepository, IConfiguration configuration)
        {
            _infoRepository = infoRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
        }

        
    }
}
