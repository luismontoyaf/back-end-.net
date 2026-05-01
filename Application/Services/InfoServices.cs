using Core.Models;
using Core.Interfaces;

namespace Application.Services
{
    public class InfoService
    {
        private readonly IInfoRepository _infoRepository;

        private readonly string _imageUploadPath;


        public InfoService(IInfoRepository infoRepository, IConfiguration configuration)
        {
            _infoRepository = infoRepository;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
        }

        public bool ValidateTenant(string tenantId)
        {
            return _infoRepository.ValidateTenant(tenantId);
        }

    }
}
