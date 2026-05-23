using Core.Models;

namespace Core.Interfaces
{
    public interface IInfoRepository
    {
        //bool AddProduct(Product product);
        string GetParameterByName(string parameterName, int tenantId);

        Task<Tenant?> GetCompanyByTenant(int tenantId);

        bool ValidateTenant(string tenantId);
    }
}