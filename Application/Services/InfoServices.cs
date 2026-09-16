using Core.Models;
using Core.Interfaces;

namespace Application.Services
{
    public class InfoService
    {
        private readonly IInfoRepository _infoRepository;
        private readonly TenantProvider _tenantProvider;

        private readonly string _imageUploadPath;


        public InfoService(
            IInfoRepository infoRepository,
            IConfiguration configuration,
            TenantProvider tenantProvider)
        {
            _infoRepository = infoRepository;
            _tenantProvider = tenantProvider;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
        }

        public Client? GetUserInfoByDocument(string document)
        {
            var tenantId = _tenantProvider.GetTenantId();
            return _infoRepository.GetUserInfoByDocument(document, tenantId);
        }

        public string GetParameter(string parameterName)
        {
            var tenantId = _tenantProvider.GetTenantId();
            return _infoRepository.GetParameter(parameterName, tenantId);
        }

        public bool ValidateTenant(string tenantId)
        {
            return _infoRepository.ValidateTenant(tenantId);
        }

    }
}
