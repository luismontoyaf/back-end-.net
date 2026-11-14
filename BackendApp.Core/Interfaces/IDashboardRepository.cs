using Core.Models;

namespace Core.Interfaces
{
    public interface IDashboardRepository
    {
        Task<string> GetDashboardInfo(int tenantId);
    }
}