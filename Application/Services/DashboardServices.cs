using Core.Models;
using Core.Interfaces;
using System.Text.Json;

namespace Application.Services
{
    public class DashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly TenantProvider _tenantProvider;

        private readonly string _imageUploadPath;


        public DashboardService(IDashboardRepository dashboardRepository, TenantProvider tenantProvider, IConfiguration configuration)
        {
            _dashboardRepository = dashboardRepository;
            _tenantProvider = tenantProvider;
            _imageUploadPath = configuration["ImageUploadPath"]; // Obtener la ruta desde appsettings.json
        }

        public async Task<DashboardResponseDto> GetDashboardInfo()
        {
            var tenantId = _tenantProvider.GetTenantId();

            var json = await _dashboardRepository.GetDashboardInfo(tenantId);

            if (string.IsNullOrEmpty(json))
            {
                return new DashboardResponseDto();
            }

            return JsonSerializer.Deserialize<DashboardResponseDto>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            )!;
        }

    }
}
