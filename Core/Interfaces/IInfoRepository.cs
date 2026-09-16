using Core.Models;

namespace Core.Interfaces
{
    public interface IInfoRepository
    {
        Client? GetUserInfoByDocument(string document, int tenantId);
        string GetParameter(string parameterName, int tenantId);
        //bool AddProduct(Product product);
        string GetParameterByName(string parameterName, int tenantId);

        Task<Tenant?> GetCompanyByTenant(int tenantId);

        bool ValidateTenant(string tenantId);
    }
}